# MovieManager Frontend

The modern, responsive web dashboard for the MovieManager ecosystem, built with Vue 3.5, Vite 7.3, and Tailwind CSS 3.4. It provides a real-time command center for monitoring media files, automated metadata matching, and hardware-accelerated transcoding.

## 🚀 Features

- **Real-time Queue Dashboard**: Monitors file ingestion statuses via automated polling.
- **Transcode Studio**: Provides full control over hardware acceleration (NVENC, AMF, QSV), video quality (CRF), and resolution downscaling.
- **Server-Sent Events (SSE)**: Parses live FFmpeg progress logs to display accurate progress bars and ETA.
- **Live System Status**: Dedicated `/info` view providing a snapshot of the backend, PostgreSQL database, and FFmpeg engine health.
- **Premium UI/UX**: Designed with dark mode glassmorphism, dynamic animations, and responsive Tailwind CSS layouts.

## 🛠️ Tech Stack

- **Framework**: [Vue 3.5](https://vuejs.org/) (Composition API / `<script setup>`)
- **Build Tool**: [Vite 7.3](https://vitejs.dev/)
- **Routing**: [Vue Router 5](https://router.vuejs.org/)
- **Styling**: [Tailwind CSS 3.4](https://tailwindcss.com/)
- **Icons**: [FontAwesome 7.2](https://fontawesome.com/)

## ⚙️ Development Setup

The frontend runs on port `9999` and proxies API requests (`/api`) to the .NET backend running on port `5294` to prevent CORS issues during local development.

### Prerequisites
- Node.js (v18+)
- npm or pnpm

### Installation

1. Install dependencies:
   ```bash
   cd Frontend
   npm install
   ```

2. Start the Vite development server:
   ```bash
   npm run dev
   ```

3. Open your browser and navigate to `http://localhost:9999`

## 📂 Project Structure

- `src/components/`: Reusable UI elements (e.g., custom folder selection dialogues).
- `src/views/`: Main routing pages (`QueueView`, `TranscodeView`, `SettingsView`, `InfoView`).
- `src/services/api.ts`: Centralized Axios configuration for backend communication.
- `src/composables/`: Reusable Vue composition functions (e.g., `useHealthCheck.ts`).
- `src/assets/`: Static assets such as logos and global stylesheets.
