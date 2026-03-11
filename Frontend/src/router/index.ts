import { createRouter, createWebHistory } from 'vue-router'
import QueueView from '../views/QueueView.vue'
import SettingsView from '../views/SettingsView.vue'
import TranscodeView from '../views/TranscodeView.vue'
import InfoView from '../views/InfoView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'queue',
      component: QueueView
    },
    {
      path: '/transcode',
      name: 'transcode',
      component: TranscodeView
    },
    {
      path: '/settings',
      name: 'settings',
      component: SettingsView
    },
    {
      path: '/info',
      name: 'info',
      component: InfoView
    },
    {
      path: '/player',
      name: 'player',
      component: () => import('../views/PlayerView.vue')
    }
  ]
})

export default router
