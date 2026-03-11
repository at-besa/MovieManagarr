using Backend.Models;
using Backend.Services.Interfaces;
using Xabe.FFmpeg;

namespace Backend.Services
{
    public class MediaAnalysisService : IMediaAnalysisService
    {
        public async Task<MediaAnalysisResult> AnalyzeFileAsync(string filePath, string ffmpegPath = null)
        {
            var result = new MediaAnalysisResult();

            try
            {
                if (!string.IsNullOrWhiteSpace(ffmpegPath))
                {
                    FFmpeg.SetExecutablesPath(ffmpegPath);
                }

                var mediaInfo = await FFmpeg.GetMediaInfo(filePath);

                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                if (videoStream != null)
                {
                    result.VideoCodec = videoStream.Codec;
                    result.Resolution = DetermineResolution(videoStream.Width, videoStream.Height);
                    result.VideoBitrate = videoStream.Bitrate;
                    result.Width = videoStream.Width;
                    result.Height = videoStream.Height;
                }

                result.DurationSeconds = mediaInfo.Duration.TotalSeconds;

                var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
                if (audioStream != null)
                {
                    result.AudioCodec = audioStream.Codec;
                    result.AudioChannels = audioStream.Channels;
                    result.AudioBitrate = audioStream.Bitrate;
                }

                var fileInfo = new FileInfo(filePath);
                result.FileSizeBytes = fileInfo.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FFmpeg Analysis failed: {ex.Message}. Falling back to filename parsing.");
                result.IsSuccess = false;
                
                // Fallback parsing
                var lowerPath = filePath.ToLowerInvariant();
                if (lowerPath.Contains("2160p") || lowerPath.Contains("4k")) result.Resolution = "4k";
                else if (lowerPath.Contains("1080p")) result.Resolution = "1080p";
                else if (lowerPath.Contains("720p")) result.Resolution = "720p";
                else result.Resolution = "SD";

                if (lowerPath.Contains("h265") || lowerPath.Contains("x265") || lowerPath.Contains("hevc")) result.VideoCodec = "x265";
                else if (lowerPath.Contains("h264") || lowerPath.Contains("x264") || lowerPath.Contains("avc")) result.VideoCodec = "x264";
                else result.VideoCodec = "Unknown";
                
                if (lowerPath.Contains("aac")) result.AudioCodec = "aac";
                else if (lowerPath.Contains("ac3")) result.AudioCodec = "ac3";
                else if (lowerPath.Contains("dts")) result.AudioCodec = "dts";
                else result.AudioCodec = "Unknown";
            }

            // Ensure we don't return nulls
            result.Resolution = string.IsNullOrWhiteSpace(result.Resolution) ? "Unknown" : result.Resolution;
            result.VideoCodec = string.IsNullOrWhiteSpace(result.VideoCodec) ? "Unknown" : result.VideoCodec;
            result.AudioCodec = string.IsNullOrWhiteSpace(result.AudioCodec) ? "Unknown" : result.AudioCodec;

            return result;
        }

        private string DetermineResolution(int width, int height)
        {
            if (width >= 3840 || height >= 2160) return "4k";
            if (width >= 1920 || height >= 1080) return "1080p";
            if (width >= 1280 || height >= 720) return "720p";
            return "SD";
        }
    }
}
