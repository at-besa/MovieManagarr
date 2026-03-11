<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../services/api'

const router = useRouter()

interface MediaMetadata {
    id: number;
    title: string;
    originalTitle: string;
    overview: string;
    posterUrl: string;
    releaseDate: string;
    year: number;
    mediaType: string;
}

interface QueueItem {
    file: string;
    autoMatch: {
        title: string;
        year: number;
        season: number | null;
        episode: number | null;
        isSeries: boolean;
        posterUrl: string;
        id: number;
    }
}

const queue = ref<QueueItem[]>([])
const expandedFile = ref<string | null>(null)
const isLoadingMatches = ref(false)
const isProcessing = ref(false)
const isProcessingAll = ref(false)

// Parsing State
const parseTitle = ref('')
const parseYear = ref<number | ''>('')
const parseIsSeries = ref(false)
const searchResults = ref<MediaMetadata[]>([])
const selectedMatch = ref<MediaMetadata | null>(null)

const loadQueue = async () => {
  try {
    const res = await api.get('/mediaprocessing/queue')
    queue.value = res.data
  } catch (err) {
    console.error('Failed to load queue:', err)
  }
}

const toggleFile = async (item: QueueItem) => {
    if (expandedFile.value === item.file) {
        expandedFile.value = null
        return
    }
    
    expandedFile.value = item.file
    searchResults.value = []
    selectedMatch.value = null
    
    // Use pre-parsed auto match data
    parseTitle.value = item.autoMatch.title || ''
    parseYear.value = item.autoMatch.year || ''
    parseIsSeries.value = item.autoMatch.isSeries
    
    if (parseTitle.value) {
        searchTmdb()
    }
}

const searchTmdb = async () => {
    if (!parseTitle.value) return;
    isLoadingMatches.value = true
    searchResults.value = []
    selectedMatch.value = null
    
    try {
        let url = `/mediaprocessing/search?title=${encodeURIComponent(parseTitle.value)}&isSeries=${parseIsSeries.value}`
        if (parseYear.value) url += `&year=${parseYear.value}`
        
        const res = await api.get(url)
        searchResults.value = res.data || []
        if (searchResults.value.length > 0) {
            selectedMatch.value = searchResults.value[0] // Auto-select first
        }
    } catch (err) {
        console.error("Search failed:", err)
    } finally {
        isLoadingMatches.value = false
    }
}

const processFile = async (file: string) => {
    if (!selectedMatch.value) {
        alert("Please select a valid TMDB match first.");
        return;
    }
    
    isProcessing.value = true;
    try {
        await api.post('/mediaprocessing/process', {
            filePath: file,
            metadata: selectedMatch.value
        });
        // Success: remove from local state
        queue.value = queue.value.filter(item => item.file !== file);
        expandedFile.value = null;
    } catch (err) {
        console.error("Process failed:", err);
        alert("Failed to process file. Check backend logs.");
    } finally {
        isProcessing.value = false;
    }
}

const autoMatchedCount = computed(() => queue.value.filter(item => item.autoMatch && item.autoMatch.id > 0).length)

const processAll = async () => {
    isProcessingAll.value = true;
    const matchesToProcess = queue.value.filter(item => item.autoMatch && item.autoMatch.id > 0);
    
    for (const item of matchesToProcess) {
        try {
            await api.post('/mediaprocessing/process', {
                filePath: item.file,
                metadata: {
                    id: item.autoMatch.id,
                    title: item.autoMatch.title,
                    year: item.autoMatch.year,
                    mediaType: item.autoMatch.isSeries ? 'Series' : 'Movie',
                    posterUrl: item.autoMatch.posterUrl || '',
                    overview: '',
                    originalTitle: '',
                    releaseDate: ''
                }
            });
            // Success: remove from local state
            queue.value = queue.value.filter(q => q.file !== item.file);
            if (expandedFile.value === item.file) {
                expandedFile.value = null;
            }
        } catch (err) {
            console.error(`Failed to process ${item.file}:`, err);
        }
    }
    isProcessingAll.value = false;
}

onMounted(() => {
  loadQueue()
})
</script>

<template>
  <div class="space-y-6 max-w-7xl mx-auto">
    <div class="flex items-center justify-between mb-8">
      <div>
        <h1 class="text-3xl font-bold text-white tracking-tight">Queue</h1>
        <p class="text-gray-400 mt-1">Files waiting to be matched and processed.</p>
      </div>
      <div class="flex items-center gap-3">
          <button 
            v-if="autoMatchedCount > 0"
            @click="processAll" 
            :disabled="isProcessingAll || isProcessing"
            class="bg-emerald-600/20 hover:bg-emerald-600 text-emerald-400 hover:text-white px-4 py-2 rounded-lg flex items-center gap-2 transition-all border border-emerald-500/30 font-medium disabled:opacity-50"
          >
            <i class="fas" :class="isProcessingAll ? 'fa-circle-notch animate-spin' : 'fa-magic'"></i> 
            {{ isProcessingAll ? 'Processing...' : `Process All (${autoMatchedCount})` }}
          </button>
          <button 
            @click="loadQueue" 
            class="bg-blue-600/20 hover:bg-blue-600 text-blue-400 hover:text-white px-4 py-2 rounded-lg flex items-center gap-2 transition-all border border-blue-500/30 font-medium"
          >
            <i class="fas fa-sync-alt"></i> Refresh List
          </button>
      </div>
    </div>

    <!-- Filled State -->
    <div v-if="queue.length > 0" class="bg-gray-800/50 border border-gray-700/50 rounded-xl overflow-hidden shadow-xl backdrop-blur-sm">
        <ul class="divide-y divide-gray-700/50">
            <li v-for="item in queue" :key="item.file" class="transition-colors group" :class="{'bg-gray-800/80': expandedFile === item.file}">
                <!-- File Header Row -->
                <div 
                    @click="toggleFile(item)"
                    class="px-6 py-4 flex items-center gap-4 cursor-pointer hover:bg-gray-700/40"
                >
                    <div v-if="item.autoMatch.posterUrl" class="w-10 h-14 rounded bg-gray-800 shrink-0 overflow-hidden shadow-sm border border-gray-700/50">
                        <img :src="item.autoMatch.posterUrl" class="w-full h-full object-cover">
                    </div>
                    <div v-else-if="item.autoMatch.id" class="w-10 h-14 rounded bg-gray-800 shrink-0 overflow-hidden shadow-sm border border-gray-700/50 flex flex-col items-center justify-center text-gray-600">
                        <i class="fas fa-image text-xs mb-0.5"></i>
                        <span class="text-[8px] font-medium leading-none">No Img</span>
                    </div>
                    <div v-else class="w-10 h-10 rounded bg-blue-900/30 flex items-center justify-center text-blue-400 shrink-0">
                        <i class="fas fa-file-video text-lg"></i>
                    </div>
                        <div class="flex-1 min-w-0 flex items-center gap-4">
                            <div class="flex-1">
                                <p class="text-sm font-medium text-gray-200 truncate">{{ item.file }}</p>
                                <p class="text-xs text-blue-400 mt-1" v-if="expandedFile === item.file">Configure matching details below...</p>
                                <p class="text-xs text-gray-500 mt-1" v-else>Click to match and process...</p>
                            </div>

                            <!-- Play Button -->
                            <button 
                                @click.stop="router.push({ name: 'player', query: { path: item.file, title: item.autoMatch.title || item.file } })"
                                class="px-3 py-1.5 rounded-lg bg-gray-700/50 hover:bg-blue-600/50 text-gray-200 hover:text-white flex items-center gap-2 transition-all border border-gray-600/50 hover:border-blue-500/50 text-xs font-medium shadow-sm"
                                title="Play file"
                            >
                                <i class="fas fa-play"></i> Play
                            </button>
                        <div class="hidden md:flex items-center gap-2 bg-gray-900/50 px-3 py-1.5 rounded-lg border border-gray-700/50" v-if="item.autoMatch.title">
                            <i class="fas fa-magic text-purple-400 text-xs text-opacity-80"></i>
                            <span class="text-xs font-semibold text-gray-300">
                                {{ item.autoMatch.title }} <span class="text-gray-500 font-normal">({{ item.autoMatch.year || 'N/A' }})</span>
                            </span>
                            <span v-if="item.autoMatch.isSeries" class="ml-1 text-[10px] bg-blue-500/20 text-blue-400 px-1.5 rounded border border-blue-500/30">TV</span>
                        </div>
                    </div>
                    <i class="fas" :class="expandedFile === item.file ? 'fa-chevron-up text-blue-400' : 'fa-chevron-down text-gray-500'"></i>
                </div>

                <!-- Expanded Workspace -->
                 <div v-if="expandedFile === item.file" class="px-6 pb-6 pt-2 border-t border-gray-700/30 bg-gray-900/30">
                     <div class="grid grid-cols-12 gap-6">
                         
                         <!-- Left Column: Search Tools -->
                         <div class="col-span-12 md:col-span-4 bg-gray-800/50 p-4 rounded-xl border border-gray-700/50 h-fit">
                             <h4 class="text-sm font-semibold text-gray-300 mb-4 flex items-center gap-2">
                                <i class="fas fa-search text-gray-500"></i> Manual Search
                             </h4>
                             
                             <div class="space-y-3">
                                 <div>
                                     <label class="block text-xs text-gray-400 mb-1">Title or TMDb ID</label>
                                     <input type="text" v-model="parseTitle" class="w-full bg-gray-900 border border-gray-700 rounded p-2 text-sm text-gray-200 focus:outline-none focus:ring-1 focus:ring-blue-500" placeholder="e.g. Inception or 27205" @keyup.enter="searchTmdb">
                                 </div>
                                 <div class="flex gap-3">
                                     <div class="flex-1">
                                        <label class="block text-xs text-gray-400 mb-1">Year</label>
                                        <input type="number" v-model="parseYear" class="w-full bg-gray-900 border border-gray-700 rounded p-2 text-sm text-gray-200 focus:outline-none focus:ring-1 focus:ring-blue-500" placeholder="Optional" @keyup.enter="searchTmdb">
                                     </div>
                                     <div class="flex-1">
                                        <label class="block text-xs text-gray-400 mb-1">Media Type</label>
                                        <select v-model="parseIsSeries" class="w-full bg-gray-900 border border-gray-700 rounded p-2 text-sm text-gray-200 focus:outline-none focus:ring-1 focus:ring-blue-500">
                                            <option :value="false">Movie</option>
                                            <option :value="true">Series</option>
                                        </select>
                                     </div>
                                 </div>
                                 <button @click="searchTmdb" class="w-full mt-2 bg-gray-700 hover:bg-gray-600 text-gray-200 py-2 rounded border border-gray-600 text-sm transition-colors flex justify-center items-center gap-2">
                                     <i class="fas fa-spinner animate-spin" v-if="isLoadingMatches"></i>
                                     <span v-else>Search TMDb</span>
                                 </button>
                             </div>
                         </div>
                         
                         <!-- Right Column: Results & Action -->
                         <div class="col-span-12 md:col-span-8 flex flex-col">
                             <h4 class="text-sm font-semibold text-gray-300 mb-2">Search Results</h4>
                             
                             <div class="flex-1 bg-gray-900/50 rounded-xl border border-gray-800 p-2 min-h-[220px] max-h-[300px] overflow-y-auto w-full">
                                 <div v-if="isLoadingMatches" class="flex items-center justify-center h-full text-gray-500 gap-3">
                                     <i class="fas fa-circle-notch animate-spin text-xl"></i> Searching...
                                 </div>
                                 <div v-else-if="searchResults.length === 0" class="flex items-center justify-center h-full text-gray-600 text-sm italic">
                                     No results found. Adjust search terms.
                                 </div>
                                 <div v-else class="space-y-1">
                                     <div 
                                        v-for="res in searchResults" :key="res.id"
                                        @click="selectedMatch = res"
                                        class="flex gap-4 p-2 rounded cursor-pointer transition-colors border"
                                        :class="selectedMatch?.id === res.id ? 'bg-blue-600/20 border-blue-500 shadow-[inset_0_0_0_1px_rgba(59,130,246,0.5)]' : 'border-transparent hover:bg-gray-800'"
                                     >
                                         <div class="w-12 h-18 bg-gray-800 shrink-0 rounded overflow-hidden border border-gray-700/50">
                                             <img v-if="res.posterUrl" :src="res.posterUrl" class="w-full h-full object-cover">
                                             <div v-else class="w-full h-full flex flex-col justify-center items-center text-gray-500 bg-gray-800/80 p-1 text-center">
                                                <i class="fas fa-image text-sm mb-1"></i>
                                                <span class="text-[9px] font-medium leading-tight">No<br>Image</span>
                                             </div>
                                         </div>
                                         <div class="flex-1 min-w-0 flex flex-col justify-center">
                                             <h5 class="font-medium text-gray-200 text-sm truncate">{{ res.title }} <span class="text-gray-500 font-normal ml-1">({{ res.year || 'N/A' }}) &bull; ID: {{ res.id }}</span></h5>
                                             <p class="text-xs text-gray-400 mt-1 line-clamp-2 leading-relaxed">{{ res.overview || 'No overview available.' }}</p>
                                         </div>
                                         <div class="flex items-center pr-2">
                                            <i class="fas fa-check-circle" :class="selectedMatch?.id === res.id ? 'text-blue-500' : 'text-gray-700'"></i>
                                         </div>
                                     </div>
                                 </div>
                             </div>
                             
                             <div class="mt-4 flex justify-end">
                                 <button 
                                    @click="processFile(item.file)"
                                    :disabled="!selectedMatch || isProcessing"
                                    class="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-lg font-medium shadow-lg hover:shadow-blue-500/20 transition-all disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
                                 >
                                     <i class="fas fa-cogs" :class="{'animate-spin': isProcessing}"></i>
                                     {{ isProcessing ? 'Processing...' : 'Confirm & Process' }}
                                 </button>
                             </div>
                         </div>
                         
                     </div>
                 </div>
            </li>
        </ul>
    </div>

    <!-- Empty State -->
    <div v-else class="bg-gray-800/50 border border-gray-700/50 rounded-xl p-12 text-center backdrop-blur-sm">
      <div class="w-16 h-16 bg-gray-700 rounded-full flex items-center justify-center mx-auto mb-4 text-2xl text-gray-400">
        <i class="fas fa-inbox"></i>
      </div>
      <h3 class="text-xl font-medium text-gray-200 mb-2">Queue is empty</h3>
      <p class="text-gray-400 max-w-sm mx-auto">Click "Refresh List" to check your source directory for new video files.</p>
    </div>

  </div>
</template>
