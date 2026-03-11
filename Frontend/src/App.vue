<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { ref } from 'vue'
import { useHealthCheck } from './composables/useHealthCheck'

const { isBackendOffline, isDbOffline, checkHealth } = useHealthCheck()
const isSidebarOpen = ref(true)
</script>

<template>
  <div class="app-container min-h-screen bg-[#0f172a] text-slate-200">
    <!-- Offline Banner -->
    <div 
      v-if="isBackendOffline" 
      class="fixed top-0 left-0 right-0 z-50 bg-red-600 text-white py-1 px-4 text-center text-xs font-bold animate-pulse flex items-center justify-center gap-2 shadow-lg"
    >
      <i class="fas fa-exclamation-triangle"></i>
      Backend Service Offline
      <button @click="checkHealth" class="ml-4 underline hover:no-underline text-white/80">Retry Now</button>
    </div>

    <!-- DB Offline Banner -->
    <div 
      v-else-if="isDbOffline" 
      class="fixed top-0 left-0 right-0 z-50 bg-yellow-600 text-white py-1 px-4 text-center text-xs font-bold flex items-center justify-center gap-2 shadow-lg"
    >
      <i class="fas fa-database animate-pulse"></i>
      Database Unreachable (PostgreSQL)
      <button @click="checkHealth" class="ml-4 underline hover:no-underline text-white/80">Retry Now</button>
    </div>
    
    <!-- Sidebar -->
    <aside class="w-64 bg-gray-800 border-r border-gray-700 flex flex-col fixed inset-y-0">
      <div class="p-6">
        <h1 class="text-2xl font-bold flex items-center gap-3 tracking-tight">
          <img src="./assets/logo.png" alt="Logo" class="w-8 h-8 rounded-lg shadow-sm shadow-blue-500/20 ring-1 ring-white/10" />
          <span class="bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-indigo-400">MovieManager</span>
        </h1>
      </div>
      
      <nav class="flex-1 px-4 space-y-2 mt-4">
        <RouterLink 
          to="/" 
          class="flex items-center gap-3 px-4 py-3 rounded-lg text-gray-400 transition-colors"
          active-class="bg-blue-600/10 text-white! font-medium relative before:absolute before:inset-y-0 before:left-0 before:w-1 before:bg-blue-500 before:rounded-r-full"
        >
          <i class="fas fa-list-ul w-5"></i>
          Queue
        </RouterLink>
        <RouterLink 
          to="/transcode" 
          class="flex items-center gap-3 px-4 py-3 rounded-lg text-gray-400 transition-colors"
          active-class="bg-blue-600/10 text-white! font-medium relative before:absolute before:inset-y-0 before:left-0 before:w-1 before:bg-blue-500 before:rounded-r-full"
        >
          <i class="fas fa-compress w-5"></i>
          Transcode
        </RouterLink>
        <RouterLink 
          to="/settings" 
          class="flex items-center gap-3 px-4 py-3 rounded-lg text-gray-400 transition-colors"
          active-class="bg-blue-600/10 text-white! font-medium relative before:absolute before:inset-y-0 before:left-0 before:w-1 before:bg-blue-500 before:rounded-r-full"
        >
          <i class="fas fa-cog w-5"></i>
          Settings
        </RouterLink>
        <RouterLink 
          to="/info" 
          class="flex items-center gap-3 px-4 py-3 rounded-lg text-gray-400 transition-colors"
          active-class="bg-blue-600/10 text-white! font-medium relative before:absolute before:inset-y-0 before:left-0 before:w-1 before:bg-blue-500 before:rounded-r-full"
        >
          <i class="fas fa-server w-5"></i>
          System Status
        </RouterLink>
      </nav>

      <!-- Bottom System Status & Version Indicator -->
      <div class="p-4 mt-auto">
        <RouterLink 
          to="/info" 
          class="flex items-center justify-between w-full px-4 py-3 bg-gray-900/50 hover:bg-gray-900 rounded-xl border border-gray-700/50 hover:border-gray-600/80 transition-all group"
        >
          <div class="flex items-center gap-2.5">
            <span class="relative flex h-2.5 w-2.5">
              <span v-if="!isBackendOffline && !isDbOffline" class="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75"></span>
              <span class="relative inline-flex rounded-full h-2.5 w-2.5" :class="isBackendOffline || isDbOffline ? 'bg-red-500' : 'bg-green-500'"></span>
            </span>
            <span class="font-medium text-xs text-gray-400 group-hover:text-gray-200 transition-colors tracking-wide">
              {{ isBackendOffline || isDbOffline ? 'System Error' : 'System Healthy' }}
            </span>
          </div>
          <span class="font-mono text-[10px] bg-gray-800 border border-gray-700/50 text-gray-400 group-hover:text-blue-400 px-1.5 py-0.5 rounded transition-colors shadow-inner">
            v1.1.0
          </span>
        </RouterLink>
      </div>
    </aside>

    <!-- Main Content -->
    <main class="flex-1 ml-64 p-8 relative">
        <div class="absolute inset-0 bg-gradient-to-br from-blue-900/10 via-transparent to-transparent pointer-events-none"></div>
        <div class="relative z-10">
            <RouterView v-slot="{ Component }">
                <transition name="fade" mode="out-in">
                    <component :is="Component" />
                </transition>
            </RouterView>
        </div>
    </main>
  </div>
</template>
