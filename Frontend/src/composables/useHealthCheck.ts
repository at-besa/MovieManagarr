import { ref, onMounted, onUnmounted } from 'vue';
import api from '../services/api';

export function useHealthCheck() {
  const isBackendOffline = ref(false);
  const isDbOffline = ref(false);
  let intervalId: number | null = null;

  const checkHealth = async () => {
    try {
      // Use dedicated health endpoint
      await api.get('/health', { timeout: 3000 });
      isBackendOffline.value = false;
      isDbOffline.value = false;
    } catch (error: any) {
      if (error.response?.data?.title === 'DB_OFFLINE') {
        isBackendOffline.value = false;
        isDbOffline.value = true;
      } else {
        isBackendOffline.value = true;
        isDbOffline.value = false;
      }
    }
  };

  onMounted(() => {
    checkHealth();
    intervalId = window.setInterval(checkHealth, 10000); // Check every 10s
  });

  onUnmounted(() => {
    if (intervalId) {
      clearInterval(intervalId);
    }
  });

  return {
    isBackendOffline,
    isDbOffline,
    checkHealth
  };
}
