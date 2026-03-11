<script setup lang="ts">
import { ref, watch } from 'vue'
import api from '../services/api'

const props = defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  label: {
    type: String,
    required: true
  }
})

const emit = defineEmits(['update:modelValue'])

const isOpen = ref(false)
const currentPath = ref(props.modelValue || 'C:\\') // Default to C:\
const directories = ref<string[]>([])
const isLoading = ref(false)

const fetchDirectories = async (path: string) => {
  isLoading.value = true
  try {
    const response = await api.get(`/filesystem/browse?path=${encodeURIComponent(path)}`)
    directories.value = response.data.directories
    currentPath.value = response.data.currentPath
  } catch (err) {
    console.error('Failed to fetch directories:', err)
  } finally {
    isLoading.value = false
  }
}

const openModal = () => {
  isOpen.value = true
  fetchDirectories(currentPath.value || 'C:\\')
}

const closeModal = () => {
  isOpen.value = false
}

const selectDirectory = (dir: string) => {
  // If it's the "Go Up" option
  if (dir === '..') {
    if (!currentPath.value) return;

    if (/^[a-zA-Z]:[/\\]?$/.test(currentPath.value)) {
      fetchDirectories('')
      return
    }

    const parts = currentPath.value.split(/[/\\]/).filter(p => p)
    parts.pop()
    
    if (parts.length === 1 && /^[a-zA-Z]:$/.test(parts[0])) {
      fetchDirectories(parts[0] + '\\')
    } else if (parts.length > 0) {
      fetchDirectories(parts.join('\\'))
    } else {
      fetchDirectories('')
    }
    return
  }
  
  // Normal directory click
  fetchDirectories(dir)
}

const confirmSelection = () => {
  emit('update:modelValue', currentPath.value)
  closeModal()
}

// Ensure the local currentPath reflects the prop if it changes externally
watch(() => props.modelValue, (newVal) => {
  if (newVal) currentPath.value = newVal
})
</script>

<template>
  <div class="mb-4">
    <label class="block text-sm font-medium text-gray-300 mb-2">{{ label }}</label>
    <div class="flex">
      <input 
        type="text" 
        :value="modelValue"
        @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
        class="flex-1 bg-gray-800 border border-gray-700 rounded-l-lg py-2 px-4 text-gray-200 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-colors"
        placeholder="e.g. C:\Media\Movies"
      />
      <button 
        type="button" 
        @click="openModal"
        class="bg-gray-700 hover:bg-gray-600 text-gray-200 px-4 py-2 rounded-r-lg border border-l-0 border-gray-700 transition-colors"
      >
        <i class="fas fa-folder-open"></i> Browse
      </button>
    </div>

    <!-- Modal -->
    <div v-if="isOpen" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
      <div class="bg-gray-800 border border-gray-700 rounded-xl shadow-2xl w-full max-w-2xl overflow-hidden flex flex-col h-[70vh]">
        
        <!-- Header -->
        <div class="px-6 py-4 border-b border-gray-700 flex justify-between items-center bg-gray-800/50">
          <h3 class="text-lg font-semibold text-gray-100">Select Directory</h3>
          <button @click="closeModal" class="text-gray-400 hover:text-gray-200 transition-colors">
            <i class="fas fa-times text-xl"></i>
          </button>
        </div>

        <!-- Current Path Display -->
        <div class="px-6 py-3 bg-gray-900 border-b border-gray-700 text-sm text-gray-300 font-mono flex items-center gap-2">
            <i class="fas fa-hdd text-blue-400"></i>
            {{ currentPath || 'This PC' }}
        </div>

        <!-- Directory List -->
        <div class="flex-1 overflow-y-auto p-2 space-y-1">
          <div v-if="isLoading" class="flex justify-center py-8">
            <i class="fas fa-spinner animate-spin text-3xl text-blue-500"></i>
          </div>
          <template v-else>
            <button 
                v-if="currentPath"
                @click="selectDirectory('..')"
                class="w-full text-left px-4 py-3 rounded hover:bg-gray-700/50 text-gray-300 flex items-center transition-colors"
            >
                <i class="fas fa-level-up-alt mr-3 text-gray-500"></i> ..
            </button>
            <button 
                v-for="dir in directories" :key="dir"
                @click="selectDirectory(dir)"
                class="w-full text-left px-4 py-3 rounded hover:bg-blue-600/20 hover:text-blue-400 text-gray-300 flex items-center transition-colors"
            >
                <i class="fas fa-folder mr-3 text-blue-500"></i> {{ dir === '\\' ? dir : (dir.split(/[/\\]/).pop() || dir) }}
            </button>
            <div v-if="directories.length === 0" class="text-center py-8 text-gray-500">
                No directories found.
            </div>
          </template>
        </div>

        <!-- Footer Actions -->
        <div class="px-6 py-4 border-t border-gray-700 bg-gray-800/50 flex justify-end gap-3">
          <button 
            @click="closeModal"
            class="px-5 py-2 rounded border border-gray-600 text-gray-300 hover:bg-gray-700 transition-colors"
          >
            Cancel
          </button>
          <button 
            @click="confirmSelection"
            class="px-5 py-2 rounded bg-blue-600 text-white hover:bg-blue-700 transition-colors font-medium shadow-lg hover:shadow-blue-500/20"
          >
            Select Current Folder
          </button>
        </div>

      </div>
    </div>
  </div>
</template>
