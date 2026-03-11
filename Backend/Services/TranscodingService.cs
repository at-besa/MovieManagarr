using Backend.Models;
using Backend.Services.Interfaces;
using System.Collections.Concurrent;
using Xabe.FFmpeg;

namespace Backend.Services
{
    public class TranscodingService : ITranscodingService
    {
        private readonly ConcurrentDictionary<string, TranscodeJob> _jobs = new();

        public TranscodeJob? GetJobStatus(string jobId)
        {
            _jobs.TryGetValue(jobId, out var job);
            return job;
        }

        public bool CancelJob(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var job))
            {
                if (job.Status == "Running")
                {
                    job.Status = "Canceled";
                    job.ErrorMessage = "Job was canceled by the user.";
                    job.CompletedAt = DateTime.UtcNow;
                    job.CancellationTokenSource?.Cancel();
                    return true;
                }
            }
            return false;
        }

        public long EstimateOutputSize(MediaAnalysisResult analysis, TranscodeSettings settings)
        {
            if (analysis.DurationSeconds <= 0) return 0;

            long sourceBitrate = analysis.VideoBitrate;
            if (sourceBitrate <= 0)
            {
                // Fallback: estimate from file size
                sourceBitrate = (long)(analysis.FileSizeBytes * 8 / analysis.DurationSeconds * 0.9);
            }

            int crf = settings.GetCrf();

            // Empirical CRF compression ratio lookup
            double compressionRatio = crf switch
            {
                <= 18 => 0.85,
                <= 20 => 0.70,
                <= 23 => 0.55,
                <= 26 => 0.45,
                <= 28 => 0.35,
                <= 32 => 0.25,
                _ => 0.20
            };

            // HEVC is ~40% more efficient than H.264 at same visual quality
            string sourceCodec = analysis.VideoCodec?.ToLowerInvariant() ?? "";
            if (sourceCodec.Contains("h264") || sourceCodec.Contains("avc") || sourceCodec.Contains("x264"))
            {
                compressionRatio *= 0.60;
            }

            long audioBitrate = analysis.AudioBitrate > 0 ? analysis.AudioBitrate : 128000; // default 128kbps
            double estimatedVideoBitrate = sourceBitrate * compressionRatio;
            long estimatedBytes = (long)((estimatedVideoBitrate + audioBitrate) * analysis.DurationSeconds / 8);

            return Math.Max(estimatedBytes, 1024); // never return 0
        }

        public async Task<string> GeneratePreviewClipAsync(string filePath, string ffmpegPath = "")
        {
            if (!string.IsNullOrWhiteSpace(ffmpegPath))
            {
                FFmpeg.SetExecutablesPath(ffmpegPath);
            }

            var previewDir = Path.Combine(Path.GetTempPath(), "MovieManager_Previews");
            Directory.CreateDirectory(previewDir);

            // Sanitize filename: remove everything except alphanumeric for max compatibility
            var safeFileName = Guid.NewGuid().ToString("N");
            var previewFileName = $"preview_{safeFileName}.mp4";
            var outputPath = Path.Combine(previewDir, previewFileName);

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            // Extract first 15 seconds at 480p for preview
            var conversion = FFmpeg.Conversions.New()
                .AddParameter("-ss 0") // Seek to start (input parameter)
                .AddParameter($"-i \"{filePath}\"")
                .AddParameter("-t 15")
                .AddParameter("-vf scale=-2:480")
                .AddParameter("-c:v libx264 -preset ultrafast -crf 28")
                .AddParameter("-c:a aac -b:a 96k")
                .AddParameter("-movflags +faststart")
                .SetOutput(outputPath);

            await conversion.Start();

            if (!File.Exists(outputPath))
            {
                throw new Exception("FFmpeg failed to create preview file.");
            }

            return previewFileName;
        }

        public async Task<TranscodeJob> StartTranscodeAsync(string filePath, TranscodeSettings settings, string ffmpegPath = "")
        {
            if (!string.IsNullOrWhiteSpace(ffmpegPath))
            {
                FFmpeg.SetExecutablesPath(ffmpegPath);
            }

            var job = new TranscodeJob
            {
                SourcePath = filePath,
                Settings = settings,
                Status = "Running",
                StartedAt = DateTime.UtcNow
            };

            // Build output path next to source with _hevc suffix
            var dir = Path.GetDirectoryName(filePath)!;
            var name = Path.GetFileNameWithoutExtension(filePath);
            var ext = Path.GetExtension(filePath);
            job.OutputPath = Path.Combine(dir, $"{name}_hevc{ext}");

            _jobs[job.JobId] = job;

            // Run transcode in background
            _ = Task.Run(async () =>
            {
                var cts = new CancellationTokenSource();
                job.CancellationTokenSource = cts;

                try
                {
                    var mediaInfo = await FFmpeg.GetMediaInfo(filePath);
                    var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                    var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

                    if (videoStream == null)
                    {
                        job.Status = "Failed";
                        job.ErrorMessage = "No video stream found in source file.";
                        return;
                    }

                    int crf = settings.GetCrf();
                    string preset = settings.GetFfmpegPreset();
                    bool useSoftwareFallback = false;

                    while (true)
                    {
                        string currentHw = useSoftwareFallback ? "software" : settings.HwAcceleration;
                        string encoderName = currentHw switch {
                            "nvenc" => "hevc_nvenc",
                            "amf" => "hevc_amf",
                            "qsv" => "hevc_qsv",
                            _ => "libx265"
                        };

                        var args = new List<string>();

                        // Video encoding
                        args.Add($"-c:v {encoderName}");

                        if (currentHw == "software")
                        {
                            args.Add($"-crf {crf} -preset {preset}");
                        }
                        else if (currentHw == "nvenc")
                        {
                            args.Add($"-rc constqp -qp {crf} -preset {preset}");
                        }
                        else if (currentHw == "qsv")
                        {
                            args.Add($"-global_quality {crf} -preset {preset}");
                        }
                        else if (currentHw == "amf")
                        {
                            string amfQuality = preset switch { "veryfast" => "speed", "slow" => "quality", _ => "balanced" };
                            args.Add($"-rc cqp -qp_i {crf} -qp_p {crf} -quality {amfQuality}");
                        }

                        // Resolution override
                        if (!string.IsNullOrEmpty(settings.TargetResolution))
                        {
                            int targetHeight = settings.TargetResolution switch
                            {
                                "720p" => 720,
                                "1080p" => 1080,
                                "4k" => 2160,
                                _ => 0
                            };
                            if (targetHeight > 0)
                            {
                                args.Add($"-vf scale=-2:{targetHeight}");
                            }
                        }

                        // Audio handling
                        if (settings.CopyAudio)
                        {
                            args.Add("-c:a copy");
                        }
                        else
                        {
                            args.Add("-c:a aac -b:a 192k");
                        }

                        // Copy subtitles and metadata
                        args.Add("-c:s copy");
                        args.Add("-map_metadata 0");

                        if (File.Exists(job.OutputPath))
                        {
                            File.Delete(job.OutputPath);
                        }

                        var conversion = FFmpeg.Conversions.New()
                            .AddParameter($"-i \"{filePath}\"")
                            .AddParameter(string.Join(" ", args))
                            .SetOutput(job.OutputPath);

                        conversion.OnProgress += (sender, progressArgs) =>
                        {
                            if (cts.IsCancellationRequested) return;

                            if (mediaInfo.Duration.TotalSeconds > 0)
                            {
                                job.ProgressPercent = Math.Min(100, 
                                    progressArgs.Duration.TotalSeconds / mediaInfo.Duration.TotalSeconds * 100);
                            }
                            else if (progressArgs.Percent > 0)
                            {
                                // Fallback native calculation if duration missing
                                job.ProgressPercent = Math.Min(100, progressArgs.Percent);
                            }
                            
                            // Prevent NaN
                            if (double.IsNaN(job.ProgressPercent))
                            {
                                job.ProgressPercent = 0;
                            }
                        };

                        try
                        {
                            await conversion.Start(cts.Token);
                            break; // Success
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when canceled
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (!useSoftwareFallback && settings.HwAcceleration != "software")
                            {
                                useSoftwareFallback = true;
                                job.ErrorMessage = $"HW Accel ({settings.HwAcceleration}) failed. Falling back to software. Error: {ex.Message}";
                                continue;
                            }
                            throw;
                        }
                    }

                    if (!cts.IsCancellationRequested && job.Status != "Canceled")
                    {
                        job.ProgressPercent = 100;
                        job.Status = "Done";
                        job.CompletedAt = DateTime.UtcNow;
                    }

                    if (File.Exists(job.OutputPath))
                    {
                        job.ActualOutputBytes = new FileInfo(job.OutputPath).Length;
                    }
                }
                catch (Exception ex)
                {
                    job.Status = "Failed";
                    job.ErrorMessage = ex.Message;
                    job.CompletedAt = DateTime.UtcNow;
                }
            });

            return job;
        }
    }
}
