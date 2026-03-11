<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import api from '../services/api'

interface FileItem {
    path: string;
    relativePath: string;
    directory: string;
    fileName: string;
    sizeBytes: number;
}

interface AnalysisResult {
    resolution: string;
    videoCodec: string;
    audioCodec: string;
    audioChannels: number;
    fileSizeBytes: number;
    durationSeconds: number;
    videoBitrate: number;
    audioBitrate: number;
    width: number;
    height: number;
    isSuccess: boolean;
}

const files = ref<FileItem[]>([])
const selectedFile = ref<FileItem | null>(null)
const analysis = ref<AnalysisResult | null>(null)
const isLoadingFiles = ref(false)
const isAnalyzing = ref(false)
const isEstimating = ref(false)
const isTranscoding = ref(false)
const isGeneratingPreview = ref(false)
const previewError = ref('')

// Transcode settings
const qualityPreset = ref('balanced')
const customCrf = ref<number | ''>('')
const hwAcceleration = ref('qsv')
const copyAudio = ref(true)
const targetResolution = ref('')

// Estimates
const estimatedBytes = ref(0)
const originalBytes = ref(0)
const savingsPercent = ref(0)

// Job tracking
const currentJobId = ref('')
const jobProgress = ref(0)
const jobStatus = ref('')
const jobError = ref('')
const actualOutputBytes = ref(0)

// Preview
const previewUrl = ref('')

// File filter
const searchQuery = ref('')
const directoryFilter = ref('all')

const filteredFiles = computed(() => {
    let result = files.value
    if (directoryFilter.value !== 'all') {
        result = result.filter(f => f.directory === directoryFilter.value)
    }
    if (searchQuery.value) {
        const q = searchQuery.value.toLowerCase()
        result = result.filter(f => f.fileName.toLowerCase().includes(q) || f.relativePath.toLowerCase().includes(q))
    }
    return result
})

const loadFiles = async () => {
    isLoadingFiles.value = true
    try {
        const res = await api.get('/transcode/files')
        files.value = res.data
    } catch (err) {
        console.error('Failed to load files:', err)
    } finally {
        isLoadingFiles.value = false
    }
}

const selectFile = async (file: FileItem) => {
    selectedFile.value = file
    analysis.value = null
    previewUrl.value = ''
    isAnalyzing.value = true
    
    try {
        const res = await api.get(`/transcode/analyze?path=${encodeURIComponent(file.path)}`)
        analysis.value = res.data
        originalBytes.value = res.data.fileSizeBytes
        await updateEstimate()
    } catch (err) {
        console.error('Analysis failed:', err)
    } finally {
        isAnalyzing.value = false
    }
}

const updateEstimate = async () => {
    if (!selectedFile.value) return
    isEstimating.value = true
    try {
        const res = await api.post('/transcode/estimate', {
            filePath: selectedFile.value.path,
            settings: {
                qualityPreset: qualityPreset.value,
                customCrf: customCrf.value || null,
                hwAcceleration: hwAcceleration.value,
                copyAudio: copyAudio.value,
                targetResolution: targetResolution.value || null
            }
        })
        estimatedBytes.value = res.data.estimatedBytes
        savingsPercent.value = res.data.savingsPercent
    } catch (err) {
        console.error('Estimate failed:', err)
    } finally {
        isEstimating.value = false
    }
}

const generatePreview = async () => {
    if (!selectedFile.value) return
    isGeneratingPreview.value = true
    previewUrl.value = ''
    previewError.value = ''
    try {
        const res = await api.post('/transcode/preview', {
            filePath: selectedFile.value.path
        })
        const origin = window.location.origin === 'http://localhost:9999' ? 'http://localhost:5294' : window.location.origin
        previewUrl.value = `${origin}${res.data.previewUrl}`
    } catch (err: any) {
        console.error('Preview generation failed:', err)
        previewError.value = err.response?.data?.message || 'Failed to generate preview. Check if FFmpeg is working.'
    } finally {
        isGeneratingPreview.value = false
    }
}

const startTranscode = async () => {
    if (!selectedFile.value) return
    isTranscoding.value = true
    jobProgress.value = 0
    jobStatus.value = 'Starting...'
    jobError.value = ''
    actualOutputBytes.value = 0

    try {
        const res = await api.post('/transcode/start', {
            filePath: selectedFile.value.path,
            settings: {
                qualityPreset: qualityPreset.value,
                customCrf: customCrf.value || null,
                hwAcceleration: hwAcceleration.value,
                copyAudio: copyAudio.value,
                targetResolution: targetResolution.value || null
            }
        })

        currentJobId.value = res.data.jobId

        // Start SSE progress tracking
        const origin = window.location.origin === 'http://localhost:9999' ? 'http://localhost:5294' : window.location.origin
        const eventSource = new EventSource(`${origin}/api/transcode/progress/${res.data.jobId}`)
        
        eventSource.onmessage = (event) => {
            const data = JSON.parse(event.data)
            jobProgress.value = Math.round(data.progressPercent * 10) / 10
            jobStatus.value = data.status
            actualOutputBytes.value = data.actualOutputBytes

            if (data.errorMessage) {
                jobError.value = data.errorMessage
            }

            if (data.status === 'Done' || data.status === 'Failed') {
                eventSource.close()
                isTranscoding.value = false
                if (data.status === 'Failed' && !data.errorMessage) {
                    jobError.value = 'Transcode failed.'
                }
            }
        }

        eventSource.onerror = () => {
            eventSource.close()
            // Fallback to polling
            pollJobStatus()
        }
    } catch (err: any) {
        console.error('Transcode start failed:', err)
        isTranscoding.value = false
        jobError.value = err.response?.data?.message || 'Failed to start transcode job.'
    }
}

const pollJobStatus = async () => {
    if (!currentJobId.value) return
    const interval = setInterval(async () => {
        try {
            const res = await api.get(`/transcode/status/${currentJobId.value}`)
            jobProgress.value = Math.round(res.data.progressPercent * 10) / 10
            jobStatus.value = res.data.status
            actualOutputBytes.value = res.data.actualOutputBytes
            
            if (res.data.errorMessage) {
                jobError.value = res.data.errorMessage
            }

            if (res.data.status === 'Done' || res.data.status === 'Failed' || res.data.status === 'Canceled') {
                clearInterval(interval)
                isTranscoding.value = false
                if (res.data.status === 'Failed' && !res.data.errorMessage) {
                    jobError.value = 'Transcode failed.'
                } else if (res.data.status === 'Canceled') {
                    jobError.value = 'Transcode was canceled by the user.'
                }
            }
        } catch { clearInterval(interval); isTranscoding.value = false }
    }, 2000)
}

const cancelTranscode = async () => {
    if (!currentJobId.value || !isTranscoding.value) return
    try {
        await api.post(`/transcode/cancel/${currentJobId.value}`)
        jobStatus.value = 'Canceled'
        isTranscoding.value = false
        jobError.value = 'Transcode was canceled by the user.'
    } catch (err: any) {
        console.error('Failed to cancel job:', err)
        jobError.value = err.response?.data?.message || 'Failed to cancel job.'
    }
}

const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

const formatDuration = (seconds: number) => {
    const h = Math.floor(seconds / 3600)
    const m = Math.floor((seconds % 3600) / 60)
    const s = Math.floor(seconds % 60)
    return h > 0 ? `${h}h ${m}m ${s}s` : `${m}m ${s}s`
}

// Re-estimate when settings change
watch([qualityPreset, customCrf, hwAcceleration, copyAudio, targetResolution], () => {
    if (selectedFile.value && analysis.value) {
        updateEstimate()
    }
})

onMounted(() => {
    loadFiles()
})
</script>

<template>
  <div class="space-y-6 max-w-7xl mx-auto">
    <div class="flex items-center justify-between mb-8">
      <div>
        <h1 class="text-3xl font-bold text-white tracking-tight">Transcode</h1>
        <p class="text-gray-400 mt-1">Convert video files to H.265 (HEVC) with quality and size control.</p>
      </div>
      <button 
        @click="loadFiles" 
        :disabled="isLoadingFiles"
        class="bg-blue-600/20 hover:bg-blue-600 text-blue-400 hover:text-white px-4 py-2 rounded-lg flex items-center gap-2 transition-all border border-blue-500/30 font-medium"
      >
        <i class="fas" :class="isLoadingFiles ? 'fa-circle-notch animate-spin' : 'fa-sync-alt'"></i> Refresh Files
      </button>
    </div>

    <div class="grid grid-cols-12 gap-6">

      <!-- File Browser (Left) -->
      <div class="col-span-12 lg:col-span-4 bg-gray-800/50 border border-gray-700/50 rounded-xl overflow-hidden shadow-xl backdrop-blur-sm flex flex-col" style="max-height: 80vh;">
        <div class="p-4 border-b border-gray-700/50">
          <div class="flex items-center gap-2 mb-3">
              <i class="fas fa-folder-open text-blue-400"></i>
              <h2 class="text-sm font-semibold text-gray-200">Media Library</h2>
              <span class="text-xs text-gray-500 ml-auto">{{ filteredFiles.length }} files</span>
          </div>
          <input 
            v-model="searchQuery"
            type="text"
            placeholder="Filter files..."
            class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2 px-3 text-sm text-gray-200 focus:outline-none focus:ring-1 focus:ring-blue-500 mb-2"
          />
          <div class="flex gap-1">
            <button 
              v-for="opt in ['all', 'Source', 'Movies', 'Series']" :key="opt"
              @click="directoryFilter = opt"
              class="px-3 py-1 text-xs rounded-lg transition-colors"
              :class="directoryFilter === opt ? 'bg-blue-600 text-white' : 'bg-gray-700/50 text-gray-400 hover:text-white'"
            >{{ opt === 'all' ? 'All' : opt }}</button>
          </div>
        </div>

        <div class="flex-1 overflow-y-auto">
          <div v-if="isLoadingFiles" class="p-8 text-center text-gray-500">
            <i class="fas fa-circle-notch animate-spin text-2xl mb-2"></i>
            <p class="text-sm">Scanning library...</p>
          </div>
          <div v-else-if="filteredFiles.length === 0" class="p-8 text-center text-gray-600 text-sm italic">
            No files found.
          </div>
          <ul v-else class="divide-y divide-gray-700/30">
            <li 
              v-for="file in filteredFiles" :key="file.path"
              @click="selectFile(file)"
              class="px-4 py-3 cursor-pointer transition-colors hover:bg-gray-700/30"
              :class="selectedFile?.path === file.path ? 'bg-blue-600/10 border-l-2 border-blue-500' : 'border-l-2 border-transparent'"
            >
              <p class="text-sm text-gray-200 truncate font-medium">{{ file.fileName }}</p>
              <div class="flex items-center gap-2 mt-1">
                <span class="text-[10px] px-1.5 py-0.5 rounded" 
                  :class="file.directory === 'Movies' ? 'bg-purple-500/20 text-purple-400 border border-purple-500/30' : 'bg-blue-500/20 text-blue-400 border border-blue-500/30'">
                  {{ file.directory }}
                </span>
                <span class="text-xs text-gray-500">{{ formatBytes(file.sizeBytes) }}</span>
              </div>
            </li>
          </ul>
        </div>
      </div>

      <!-- Right Panel -->
      <div class="col-span-12 lg:col-span-8 space-y-6">

        <!-- No file selected -->
        <div v-if="!selectedFile" class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-12 text-center backdrop-blur-sm">
          <div class="w-16 h-16 bg-gray-700 rounded-full flex items-center justify-center mx-auto mb-4 text-2xl text-gray-400">
            <i class="fas fa-compress"></i>
          </div>
          <h3 class="text-xl font-medium text-gray-200 mb-2">Select a file to transcode</h3>
          <p class="text-gray-400 max-w-sm mx-auto">Choose a video file from your library to analyze, preview, and convert to H.265.</p>
        </div>

        <!-- File Analysis -->
        <div v-else>
          <!-- Source Info Card -->
          <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-5 shadow-xl backdrop-blur-sm">
            <div class="flex items-center gap-3 mb-4">
              <i class="fas fa-film text-purple-400"></i>
              <h3 class="text-sm font-semibold text-gray-200">Source File</h3>
              <span v-if="isAnalyzing" class="text-xs text-gray-500 ml-auto"><i class="fas fa-circle-notch animate-spin mr-1"></i>Analyzing...</span>
            </div>
            <p class="text-sm text-gray-300 truncate mb-3 font-mono bg-gray-900/50 px-3 py-2 rounded-lg">{{ selectedFile.relativePath }}</p>
            
            <div v-if="analysis" class="grid grid-cols-2 md:grid-cols-4 gap-3">
              <div class="bg-gray-900/50 rounded-lg p-3 text-center">
                <p class="text-[10px] text-gray-500 uppercase tracking-wider mb-1">Resolution</p>
                <p class="text-sm font-semibold text-white">{{ analysis.width }}×{{ analysis.height }}</p>
              </div>
              <div class="bg-gray-900/50 rounded-lg p-3 text-center">
                <p class="text-[10px] text-gray-500 uppercase tracking-wider mb-1">Codec</p>
                <p class="text-sm font-semibold" :class="analysis.videoCodec?.includes('hevc') || analysis.videoCodec?.includes('265') ? 'text-green-400' : 'text-orange-400'">{{ analysis.videoCodec }}</p>
              </div>
              <div class="bg-gray-900/50 rounded-lg p-3 text-center">
                <p class="text-[10px] text-gray-500 uppercase tracking-wider mb-1">Duration</p>
                <p class="text-sm font-semibold text-white">{{ formatDuration(analysis.durationSeconds) }}</p>
              </div>
              <div class="bg-gray-900/50 rounded-lg p-3 text-center">
                <p class="text-[10px] text-gray-500 uppercase tracking-wider mb-1">Size</p>
                <p class="text-sm font-semibold text-white">{{ formatBytes(analysis.fileSizeBytes) }}</p>
              </div>
            </div>
          </div>

          <!-- Transcode Settings -->
          <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-5 shadow-xl backdrop-blur-sm">
            <div class="flex items-center gap-3 mb-5">
              <i class="fas fa-sliders-h text-blue-400"></i>
              <h3 class="text-sm font-semibold text-gray-200">Transcode Settings</h3>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
              <!-- Quality Preset -->
              <div>
                <label class="block text-xs text-gray-400 mb-2 uppercase tracking-wider">Quality Preset</label>
                <div class="flex gap-2">
                  <button 
                    v-for="p in ['fast', 'balanced', 'quality']" :key="p"
                    @click="qualityPreset = p; customCrf = ''"
                    class="flex-1 py-2.5 rounded-lg text-sm font-medium transition-all border"
                    :class="qualityPreset === p && !customCrf
                      ? (p === 'fast' ? 'bg-yellow-600/20 text-yellow-400 border-yellow-500/50' 
                        : p === 'quality' ? 'bg-green-600/20 text-green-400 border-green-500/50'
                        : 'bg-blue-600/20 text-blue-400 border-blue-500/50')
                      : 'bg-gray-900/50 text-gray-500 border-gray-700/50 hover:text-gray-300'"
                  >
                    <i class="fas mr-1" :class="p === 'fast' ? 'fa-bolt' : p === 'quality' ? 'fa-gem' : 'fa-balance-scale'"></i>
                    {{ p.charAt(0).toUpperCase() + p.slice(1) }}
                  </button>
                </div>
                <p class="text-[10px] text-gray-500 mt-2">
                  {{ qualityPreset === 'fast' ? 'CRF 28 — Smallest file, faster encode' : qualityPreset === 'quality' ? 'CRF 18 — Near-lossless, larger file' : 'CRF 23 — Best trade-off of quality and size' }}
                </p>
              </div>

              <!-- HW Acceleration -->
              <div>
                <label class="block text-xs text-gray-400 mb-2 uppercase tracking-wider">Hardware Acceleration</label>
                <select 
                  v-model="hwAcceleration"
                  class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 px-4 text-sm text-gray-200 focus:outline-none focus:ring-1 focus:ring-blue-500"
                >
                  <option value="qsv">Intel QuickSync (QSV)</option>
                  <option value="nvenc">NVIDIA NVENC</option>
                  <option value="amf">AMD AMF</option>
                  <option value="software">Software (libx265)</option>
                </select>
              </div>

              <!-- Custom CRF -->
              <div>
                <label class="block text-xs text-gray-400 mb-2 uppercase tracking-wider">Custom CRF Override (0-51)</label>
                <input 
                  v-model="customCrf"
                  type="number" min="0" max="51"
                  class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 px-4 text-sm text-gray-200 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="Leave empty to use preset"
                />
              </div>

              <!-- Resolution Override -->
              <div>
                <label class="block text-xs text-gray-400 mb-2 uppercase tracking-wider">Target Resolution</label>
                <select 
                  v-model="targetResolution"
                  class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 px-4 text-sm text-gray-200 focus:outline-none focus:ring-1 focus:ring-blue-500"
                >
                  <option value="">Keep Original</option>
                  <option value="4k">4K (2160p)</option>
                  <option value="1080p">1080p</option>
                  <option value="720p">720p</option>
                </select>
              </div>
            </div>

            <!-- Audio Toggle -->
            <div class="mt-4 flex items-center gap-3">
              <label class="relative inline-flex items-center cursor-pointer">
                <input type="checkbox" v-model="copyAudio" class="sr-only peer">
                <div class="w-9 h-5 bg-gray-700 rounded-full peer peer-checked:bg-blue-600 peer-checked:after:translate-x-full after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all"></div>
              </label>
              <span class="text-sm text-gray-300">Copy audio stream (no re-encode)</span>
            </div>
          </div>

          <!-- Size Estimate Banner -->
          <div v-if="analysis && estimatedBytes > 0" class="bg-gradient-to-r from-blue-900/30 to-purple-900/30 border border-blue-500/20 rounded-xl p-5 shadow-xl backdrop-blur-sm">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-xs text-gray-400 uppercase tracking-wider mb-1">Estimated Output Size</p>
                <p class="text-2xl font-bold text-white">
                  ~{{ formatBytes(estimatedBytes) }}
                  <span class="text-sm font-normal text-gray-400 ml-2">(was {{ formatBytes(originalBytes) }})</span>
                </p>
              </div>
              <div class="text-right">
                <p class="text-3xl font-bold" :class="savingsPercent > 0 ? 'text-green-400' : 'text-yellow-400'">
                  {{ savingsPercent > 0 ? '-' : '+' }}{{ Math.abs(savingsPercent) }}%
                </p>
                <p class="text-xs text-gray-400">{{ savingsPercent > 0 ? 'smaller' : 'larger' }}</p>
              </div>
            </div>
          </div>

          <!-- Preview Section -->
          <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-5 shadow-xl backdrop-blur-sm">
            <div class="flex items-center gap-3 mb-4">
              <i class="fas fa-play-circle text-green-400"></i>
              <h3 class="text-sm font-semibold text-gray-200">Preview & Actions</h3>
            </div>
            
            <div v-if="previewUrl" class="mb-4 rounded-lg overflow-hidden bg-black relative aspect-video flex items-center justify-center">
              <video :src="previewUrl" controls class="w-full max-h-[300px]"></video>
            </div>
            
            <div v-if="isGeneratingPreview" class="mb-4 rounded-lg overflow-hidden bg-gray-900 aspect-video flex flex-col items-center justify-center border border-gray-700/50">
              <i class="fas fa-cog fa-spin text-4xl text-blue-500 mb-3"></i>
              <p class="text-sm text-gray-400 font-medium">Generating preview clip...</p>
              <p class="text-[10px] text-gray-500 mt-1">This usually takes 5-10 seconds</p>
            </div>

            <div v-if="previewError" class="mb-4 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-xs text-red-400 flex items-center gap-2">
              <i class="fas fa-exclamation-circle text-sm"></i>
              {{ previewError }}
            </div>

            <div class="flex gap-3">
              <button 
                @click="generatePreview"
                :disabled="isGeneratingPreview || isTranscoding"
                class="bg-gray-700/50 hover:bg-gray-600 text-gray-300 hover:text-white px-4 py-2.5 rounded-lg flex items-center gap-2 transition-all border border-gray-600/50 text-sm disabled:opacity-50"
              >
                <i class="fas" :class="isGeneratingPreview ? 'fa-circle-notch animate-spin' : 'fa-eye'"></i>
                {{ isGeneratingPreview ? 'Generating...' : 'Generate Preview (15s)' }}
              </button>

              <button 
                @click="startTranscode"
                :disabled="isTranscoding || !analysis"
                class="flex-1 bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white px-6 py-2.5 rounded-lg font-semibold shadow-lg shadow-blue-500/20 transition-all disabled:opacity-50 flex items-center justify-center gap-2 text-sm"
              >
                <i class="fas" :class="isTranscoding ? 'fa-circle-notch animate-spin' : 'fa-compress'"></i>
                {{ isTranscoding ? 'Transcoding...' : 'Start Transcode' }}
              </button>
            </div>
          </div>

          <!-- Progress Bar (during transcode) -->
          <div v-if="isTranscoding || jobStatus === 'Done' || jobStatus === 'Failed' || jobStatus === 'Canceled'" class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-5 shadow-xl backdrop-blur-sm">
            <div class="flex items-center gap-3 mb-3">
              <i class="fas" :class="jobStatus === 'Done' ? 'fa-check-circle text-green-400' : (jobStatus === 'Failed' || jobStatus === 'Canceled') ? 'fa-times-circle text-red-400' : 'fa-cog fa-spin text-blue-400'"></i>
              <h3 class="text-sm font-semibold text-gray-200">
                {{ jobStatus === 'Done' ? 'Transcode Complete!' : jobStatus === 'Failed' ? 'Transcode Failed' : jobStatus === 'Canceled' ? 'Transcode Canceled' : 'Transcoding in Progress...' }}
              </h3>
              
              <div class="ml-auto flex items-center gap-4">
                <span class="text-sm font-mono text-blue-400">{{ jobProgress.toFixed(1) }}%</span>
                <button 
                  v-if="isTranscoding || jobStatus === 'Running'"
                  @click="cancelTranscode"
                  class="bg-red-500/20 hover:bg-red-500/40 border border-red-500/50 text-red-400 hover:text-red-300 px-3 py-1 rounded text-xs font-bold transition-colors flex items-center gap-1"
                >
                  <i class="fas fa-stop"></i> Cancel
                </button>
              </div>
            </div>
            
            <div class="w-full bg-gray-700/50 rounded-full h-3 overflow-hidden">
              <div 
                class="h-full rounded-full transition-all duration-500"
                :class="jobStatus === 'Done' ? 'bg-green-500' : (jobStatus === 'Failed' || jobStatus === 'Canceled') ? 'bg-red-500' : 'bg-gradient-to-r from-blue-500 to-purple-500'"
                :style="{ width: `${jobProgress}%` }"
              ></div>
            </div>

            <div v-if="jobError" class="mt-3 text-sm text-red-400 bg-red-500/10 border border-red-500/20 rounded-lg px-3 py-2">
              <i class="fas fa-exclamation-triangle mr-1"></i> {{ jobError }}
            </div>

            <div v-if="jobStatus === 'Done' && actualOutputBytes > 0" class="mt-3 text-sm text-green-400">
              <i class="fas fa-check mr-1"></i> Final output: {{ formatBytes(actualOutputBytes) }} (estimated was ~{{ formatBytes(estimatedBytes) }})
            </div>
          </div>

        </div>

      </div>
    </div>
  </div>
</template>
