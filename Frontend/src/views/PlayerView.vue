<template>
  <div class="player-container">
    <!-- Close Button -->
    <button @click="goBack" class="back-button" title="Close Player">
      <svg xmlns="http://www.w3.org/2000/svg" class="h-10 w-10 text-white/80 hover:text-white transition-colors" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
        <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
      </svg>
    </button>

    <!-- Header Overlay -->
    <div class="player-header">
      <h1 v-if="title">{{ title }}</h1>
      <p v-if="subTitle">{{ subTitle }}</p>
    </div>

    <!-- Video Element -->
    <div v-content class="video-wrapper">
      <video ref="videoPlayer" controls autoplay crossorigin="anonymous" class="main-video">
        <source :src="streamUrl" :type="streamType">
        Your browser does not support the video tag.
      </video>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="player-overlay">
      <div class="spinner"></div>
      <p>Loading stream...</p>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import axios from 'axios';

const route = useRoute();
const router = useRouter();
const videoPlayer = ref(null);
const loading = ref(true);

const filePath = computed(() => route.query.path);
const title = ref(route.query.title || 'Movie Player');
const subTitle = ref('');

const streamUrl = computed(() => {
  if (!filePath.value) return '';
  const baseUrl = window.location.origin.replace('9999', '5294');
  return `${baseUrl}/api/transcode/stream?path=${encodeURIComponent(filePath.value)}`;
});

const streamType = computed(() => {
  if (!filePath.value) return 'video/mp4';
  const ext = filePath.value.split('.').pop().toLowerCase();
  if (ext === 'mkv') return 'video/x-matroska';
  if (ext === 'webm') return 'video/webm';
  if (ext === 'avi') return 'video/x-msvideo';
  return 'video/mp4';
});

onMounted(async () => {
  if (!filePath.value) {
    alert('No file path provided.');
    router.push('/');
    return;
  }

  // Try to get relative path for a better subtitle/header
  try {
    const res = await axios.get(`http://localhost:5294/api/transcode/relative-path?path=${encodeURIComponent(filePath.value)}`);
    subTitle.value = res.data.relativePath;
  } catch (e) {
    subTitle.value = filePath.value;
  }

  if (videoPlayer.value) {
    videoPlayer.value.addEventListener('loadeddata', () => {
      loading.value = false;
    });
    
    videoPlayer.value.addEventListener('error', (e) => {
      console.error('Video Error:', e);
      loading.value = false;
      // Note: Some H.265 files might not play in all browsers
    });
  }
});

const goBack = () => {
  router.back();
};
</script>

<style scoped>
.player-container {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: #000;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  color: #fff;
}

.back-button {
  position: absolute;
  top: 1.5rem;
  right: 2rem;
  background: transparent;
  border: none;
  width: 48px;
  height: 48px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
  transition: transform 0.2s ease;
  filter: drop-shadow(0 2px 4px rgba(0,0,0,0.8));
}

.back-button:hover {
  transform: scale(1.1);
}

.player-header {
  position: absolute;
  top: 2rem;
  left: 3rem;
  z-index: 100;
  pointer-events: none;
  text-shadow: 0 4px 10px rgba(0,0,0,0.8);
}

.player-header h1 {
  font-size: 2rem;
  margin: 0;
  background: linear-gradient(to right, #fff, #aaa);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  font-weight: 800;
}

.player-header p {
  color: rgba(255,255,255,0.6);
  margin: 0.5rem 0 0;
  font-size: 0.9rem;
  max-width: 600px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.video-wrapper {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background: radial-gradient(circle, #1a1a1a 0%, #000 100%);
}

.main-video {
  width: 100%;
  max-height: 100vh;
  outline: none;
}

.player-overlay {
  position: absolute;
  inset: 0;
  background: rgba(0,0,0,0.7);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 50;
  backdrop-filter: blur(5px);
}

.spinner {
  width: 50px;
  height: 50px;
  border: 4px solid rgba(255,255,255,0.1);
  border-top-color: #3b82f6;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>
