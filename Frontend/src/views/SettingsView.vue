<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../services/api'
import FolderSelector from '../components/FolderSelector.vue'

const tmdbApiKey = ref('')
const sourceDir = ref('')
const targetMovieDir = ref('')
const targetSeriesDir = ref('')
const moviePattern = ref('')
const seriesPattern = ref('')
const seasonFolderPattern = ref('')
const ignoreFolders = ref('')
const defaultHwAccel = ref('qsv')
const ffmpegPath = ref('')

const isSaving = ref(false)
const showSuccess = ref(false)

const loadSettings = async () => {
    try {
        const res = await api.get('/settings');
        tmdbApiKey.value = res.data.tmdbApiKey;
        sourceDir.value = res.data.sourceDir;
        targetMovieDir.value = res.data.targetMovieDir;
        targetSeriesDir.value = res.data.targetSeriesDir;
        moviePattern.value = res.data.moviePattern;
        seriesPattern.value = res.data.seriesPattern;
        seasonFolderPattern.value = res.data.seasonFolderPattern;
        ignoreFolders.value = res.data.ignoreFolders;
        defaultHwAccel.value = res.data.defaultHwAccel || 'qsv';
        ffmpegPath.value = res.data.ffmpegPath;
    } catch (err) {
        console.error("Failed to load settings:", err);
    }
}

const saveSettings = async () => {
    isSaving.value = true
    try {
        await api.post('/settings', {
            tmdbApiKey: tmdbApiKey.value,
            sourceDir: sourceDir.value,
            targetMovieDir: targetMovieDir.value,
            targetSeriesDir: targetSeriesDir.value,
            moviePattern: moviePattern.value,
            seriesPattern: seriesPattern.value,
            seasonFolderPattern: seasonFolderPattern.value,
            ignoreFolders: ignoreFolders.value,
            defaultHwAccel: defaultHwAccel.value,
            ffmpegPath: ffmpegPath.value
        })
        showSuccess.value = true
        setTimeout(() => showSuccess.value = false, 3000)
    } catch (err) {
        alert('Failed to save settings.')
    } finally {
        isSaving.value = false
    }
}

onMounted(() => {
    loadSettings();
})
</script>

<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <div class="flex items-center justify-between mb-8">
        <div>
            <h1 class="text-3xl font-bold text-gray-100 tracking-tight">Configuration</h1>
            <p class="text-gray-400 mt-1">Manage API keys, local directory paths, and file naming patterns.</p>
        </div>
        <button 
            @click="saveSettings" 
            :disabled="isSaving"
            class="px-6 py-2.5 rounded-lg flex items-center gap-2 transition-all shadow-lg text-white font-medium"
            :class="[
                showSuccess 
                    ? 'bg-emerald-600 hover:bg-emerald-700 shadow-emerald-500/20' 
                    : 'bg-blue-600 hover:bg-blue-700 shadow-blue-500/20',
                isSaving ? 'opacity-50 cursor-not-allowed' : ''
            ]"
        >
            <i class="fas" :class="[
                isSaving ? 'fa-circle-notch animate-spin' : 
                showSuccess ? 'fa-check' : 'fa-save'
            ]"></i> 
            {{ isSaving ? 'Saving...' : showSuccess ? 'Saved!' : 'Save Settings' }}
        </button>
    </div>

    <!-- API Keys Section -->
    <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm">
      <h2 class="text-xl font-semibold text-gray-200 mb-4 flex items-center gap-2">
        <i class="fas fa-key text-blue-500"></i> API Integrations
      </h2>
      
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-300 mb-2">TMDb API Key</label>
          <div class="relative">
            <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                <i class="fas fa-fingerprint text-gray-500"></i>
            </div>
            <input 
              v-model="tmdbApiKey"
              type="password" 
              class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 pl-10 pr-4 text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors"
              placeholder="Enter your v3 auth key"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Naming Patterns Section -->
    <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm">
      <div class="flex justify-between items-start mb-4">
          <h2 class="text-xl font-semibold text-gray-200 flex items-center gap-2">
            <i class="fas fa-file-signature text-blue-500"></i> Naming Patterns
          </h2>
          <div class="text-xs text-gray-400 max-w-sm text-right">
              Available variables: <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">{Title}</code> <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">{Year}</code> <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">{Resolution}</code> <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">{VideoCodec}</code> <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">{AudioCodec}</code> <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">{S}</code> <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">{E}</code> <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">{EpisodeTitle}</code>
          </div>
      </div>
      
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-300 mb-2">Movie Pattern</label>
          <input 
            v-model="moviePattern"
            type="text" 
            class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 px-4 text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors font-mono text-sm"
            placeholder="{Title} ({Year}) - [{Resolution} {VideoCodec}]"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-300 mb-2">Series Pattern</label>
          <input 
            v-model="seriesPattern"
            type="text" 
            class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 px-4 text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors font-mono text-sm"
            placeholder="{Title} ({Year}) S{S}E{E} - {EpisodeTitle} - [{Resolution} {VideoCodec}]"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-300 mb-2">Season Directory Pattern</label>
          <input 
            v-model="seasonFolderPattern"
            type="text" 
            class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 px-4 text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors font-mono text-sm"
            placeholder="Season {S} or Staffel {S}"
          />
        </div>
      </div>
    </div>

    <!-- Directories Section -->
    <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm">
      <h2 class="text-xl font-semibold text-gray-200 mb-4 flex items-center gap-2">
        <i class="fas fa-folder-tree text-blue-500"></i> Local Directories
      </h2>
      
      <div class="space-y-2">
        <FolderSelector v-model="sourceDir" label="Source Directory (Downloads)" />
        <FolderSelector v-model="targetMovieDir" label="Target Movie Directory" />
        <FolderSelector v-model="targetSeriesDir" label="Target Series Directory" />
      </div>
    </div>

    <!-- Advanced Configuration Section -->
    <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm">
      <h2 class="text-xl font-semibold text-gray-200 mb-4 flex items-center gap-2">
        <i class="fas fa-microchip text-blue-500"></i> Advanced Settings
      </h2>
      
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-300 mb-2">Ignore Folders List (Comma-separated)</label>
          <input 
            v-model="ignoreFolders"
            type="text" 
            class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 px-4 text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors text-sm"
            placeholder="e.g. Games,Apps,Software"
          />
          <p class="text-xs text-gray-400 mt-2 ml-1">Any video files discovered deeply inside paths containing these strings will be silently skipped from the processing queue, protecting game cutscenes.</p>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-300 mb-2">Default Hardware Acceleration</label>
          <select 
            v-model="defaultHwAccel"
            class="w-full bg-gray-900 border border-gray-700 rounded-lg py-2.5 px-4 text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors text-sm"
          >
            <option value="qsv">Intel QuickSync (QSV)</option>
            <option value="nvenc">NVIDIA NVENC</option>
            <option value="amf">AMD AMF</option>
            <option value="software">Software (libx265)</option>
          </select>
          <p class="text-xs text-gray-400 mt-2 ml-1">Choose the GPU acceleration method used by default when starting a new transcode job.</p>
        </div>
        <div class="pt-2">
          <FolderSelector v-model="ffmpegPath" label="Custom FFmpeg Executable Directory" />
          <p class="text-xs text-gray-400 mt-2 ml-1">Optional. If FFmpeg is not installed globally on your machine, point this to the folder containing <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">ffmpeg.exe</code> and <code class="bg-gray-900 px-1 py-0.5 rounded text-blue-300">ffprobe.exe</code> to enable detailed media codec and resolution analysis.</p>
        </div>
      </div>
    </div>

  </div>
</template>
