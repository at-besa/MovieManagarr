<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import api from '../services/api'

interface SystemInfo {
  version: string
  osVersion: string
  dotNetVersion: string
  startTime: string
  databaseStatus: string
  ffmpegStatus: string
  ffmpegVersion: string
}

const sysInfo = ref<SystemInfo | null>(null)
const isLoading = ref(true)
const errorMsg = ref('')

const serverUptime = ref('')

let uptimeInterval: number | null = null

const fetchInfo = async () => {
    isLoading.value = true
    errorMsg.value = ''
    try {
        const res = await api.get('/system/info')
        sysInfo.value = res.data
        updateUptime()
    } catch (err: any) {
        errorMsg.value = err.response?.data?.message || err.message || 'Failed to fetch system info'
    } finally {
        isLoading.value = false
    }
}

const updateUptime = () => {
    if (!sysInfo.value || !sysInfo.value.startTime) return
    const start = new Date(sysInfo.value.startTime).getTime()
    const now = new Date().getTime()
    const diff = now - start

    if (diff < 0) {
        serverUptime.value = 'Just started'
        return
    }

    const days = Math.floor(diff / (1000 * 60 * 60 * 24))
    const hours = Math.floor((diff / (1000 * 60 * 60)) % 24)
    const mins = Math.floor((diff / 1000 / 60) % 60)
    const secs = Math.floor((diff / 1000) % 60)

    let str = ''
    if (days > 0) str += `${days}d `
    if (hours > 0) str += `${hours}h `
    if (mins > 0) str += `${mins}m `
    str += `${secs}s`
    
    serverUptime.value = str
}

onMounted(() => {
    fetchInfo()
    uptimeInterval = window.setInterval(updateUptime, 1000)
})

onUnmounted(() => {
    if (uptimeInterval) clearInterval(uptimeInterval)
})
</script>

<template>
  <div class="space-y-6 max-w-7xl mx-auto">
    <div class="flex items-center justify-between mb-8">
      <div>
        <h1 class="text-3xl font-bold text-white tracking-tight">System Status</h1>
        <p class="text-gray-400 mt-1">Detailed health and environment information for MovieManager services.</p>
      </div>
      <button 
        @click="fetchInfo" 
        :disabled="isLoading"
        class="bg-gray-800 hover:bg-gray-700 text-white px-4 py-2 rounded-lg text-sm font-medium transition-colors border border-gray-700 flex items-center gap-2 disabled:opacity-50 relative overflow-hidden group"
      >
        <span class="absolute inset-0 w-full h-full bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-full group-hover:animate-[shimmer_1.5s_infinite]"></span>
        <i class="fas fa-sync-alt" :class="{ 'animate-spin text-blue-400': isLoading }"></i>
        Refresh
      </button>
    </div>

    <!-- Error State -->
    <div v-if="errorMsg" class="bg-red-500/10 border border-red-500/20 rounded-xl p-6 text-center">
      <i class="fas fa-exclamation-triangle text-3xl text-red-500 mb-2 mt-2"></i>
      <h3 class="text-lg font-medium text-red-400">Failed to Connect</h3>
      <p class="text-gray-400 mt-1 max-w-md mx-auto">{{ errorMsg }}</p>
      <button @click="fetchInfo" class="mt-4 bg-red-500/20 hover:bg-red-500/30 text-red-400 px-4 py-2 rounded-lg text-sm transition-colors border border-red-500/30">
        Try Again
      </button>
    </div>

    <!-- Skeleton Loader -->
    <div v-if="isLoading && !sysInfo" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div v-for="i in 3" :key="i" class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm animate-pulse">
        <div class="h-5 bg-gray-700 rounded w-1/3 mb-6"></div>
        <div class="space-y-4">
          <div v-for="j in 3" :key="j" class="flex justify-between">
            <div class="h-4 bg-gray-700 rounded w-1/4"></div>
            <div class="h-4 bg-gray-700 rounded w-1/2"></div>
          </div>
        </div>
      </div>
    </div>

    <!-- Content -->
    <div v-if="sysInfo && !errorMsg" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      
      <!-- App Card -->
      <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm">
        <div class="flex items-center gap-3 mb-6">
          <div class="w-10 h-10 rounded-lg bg-blue-500/20 flex items-center justify-center text-blue-400">
            <i class="fas fa-server text-lg"></i>
          </div>
          <h2 class="text-lg font-semibold text-gray-200">Application</h2>
        </div>
        
        <div class="space-y-4 text-sm">
          <div class="flex justify-between items-center border-b border-gray-700/50 pb-3">
            <span class="text-gray-400">Version</span>
            <span class="text-gray-200 font-mono bg-gray-900 px-2 py-0.5 rounded border border-gray-700/50 flex items-center gap-2">
                <i class="fas fa-tag text-xs text-blue-400"></i>
                {{ sysInfo.version }}
            </span>
          </div>
          <div class="flex justify-between items-center border-b border-gray-700/50 pb-3">
            <span class="text-gray-400">Uptime</span>
            <span class="text-blue-400 font-mono flex items-center gap-2">
                <i class="fas fa-clock text-xs"></i>
                {{ serverUptime }}
            </span>
          </div>
        </div>
      </div>

      <!-- Environment Card -->
      <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm lg:col-span-2">
        <div class="flex items-center gap-3 mb-6">
          <div class="w-10 h-10 rounded-lg bg-purple-500/20 flex items-center justify-center text-purple-400">
            <i class="fas fa-microchip text-lg"></i>
          </div>
          <h2 class="text-lg font-semibold text-gray-200">Environment</h2>
        </div>
        
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-x-8 gap-y-4 text-sm">
          <div class="flex justify-between items-start border-b border-gray-700/50 pb-3">
            <span class="text-gray-400 w-1/3">OS</span>
            <span class="text-gray-200 text-right font-mono text-xs">{{ sysInfo.osVersion }}</span>
          </div>
          <div class="flex justify-between items-start border-b border-gray-700/50 pb-3">
            <span class="text-gray-400 w-1/3">Runtime</span>
            <span class="text-gray-200 text-right font-mono text-xs">{{ sysInfo.dotNetVersion }}</span>
          </div>
        </div>
      </div>

      <!-- Database Card -->
      <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm">
        <div class="flex items-center gap-3 mb-6">
          <div class="w-10 h-10 rounded-lg flex items-center justify-center" :class="sysInfo.databaseStatus === 'Online' ? 'bg-green-500/20 text-green-400' : 'bg-red-500/20 text-red-400'">
            <i class="fas fa-database text-lg"></i>
          </div>
          <h2 class="text-lg font-semibold text-gray-200">PostgreSQL</h2>
        </div>
        
        <div class="space-y-4 text-sm">
          <div class="flex justify-between items-center border-b border-gray-700/50 pb-3">
            <span class="text-gray-400">Status</span>
            <span class="font-medium flex items-center gap-2" :class="sysInfo.databaseStatus === 'Online' ? 'text-green-400' : 'text-red-400'">
              <span class="relative flex h-2.5 w-2.5">
                <span v-if="sysInfo.databaseStatus === 'Online'" class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                <span class="relative inline-flex rounded-full h-2.5 w-2.5" :class="sysInfo.databaseStatus === 'Online' ? 'bg-green-500' : 'bg-red-500'"></span>
              </span>
              {{ sysInfo.databaseStatus }}
            </span>
          </div>
        </div>
      </div>

      <!-- FFmpeg Card -->
      <div class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-6 shadow-xl backdrop-blur-sm lg:col-span-2">
        <div class="flex items-center gap-3 mb-6">
          <div class="w-10 h-10 rounded-lg flex items-center justify-center" :class="sysInfo.ffmpegStatus === 'Online' ? 'bg-green-500/20 text-green-400' : 'bg-red-500/20 text-red-400'">
            <i class="fas fa-photo-video text-lg"></i>
          </div>
          <h2 class="text-lg font-semibold text-gray-200">FFmpeg Engine</h2>
        </div>
        
        <div class="space-y-4 text-sm">
          <div class="flex justify-between items-center border-b border-gray-700/50 pb-3">
            <span class="text-gray-400">Status</span>
            <span class="font-medium flex items-center gap-2" :class="sysInfo.ffmpegStatus === 'Online' ? 'text-green-400' : 'text-red-400'">
              <span class="relative flex h-2.5 w-2.5">
                <span v-if="sysInfo.ffmpegStatus === 'Online'" class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
                <span class="relative inline-flex rounded-full h-2.5 w-2.5" :class="sysInfo.ffmpegStatus === 'Online' ? 'bg-green-500' : 'bg-red-500'"></span>
              </span>
              {{ sysInfo.ffmpegStatus === 'Online' ? 'Installed & Ready' : 'Executable Not Found' }}
            </span>
          </div>
          <div class="flex flex-col border-b border-gray-700/50 pb-3">
            <span class="text-gray-400 mb-2">Build Output</span>
            <div class="bg-gray-900 border border-gray-700/50 rounded-lg p-3 overflow-x-auto">
                <pre class="text-xs text-gray-300 font-mono">{{ sysInfo.ffmpegVersion || '---' }}</pre>
            </div>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>
