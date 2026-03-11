# MovieManager

**A professional, automated .NET/Vue command center for scanning, cataloging, transcoding, and managing a robust media library.**

Built from the ground up to handle massive local media archives, MovieManager recursively ingests video files, intelligently fetches metadata from The Movie Database (TMDB), standardizes filenames, generates subtitles via OpenAI Whisper, and provides hardware-accelerated H.265 transcoding controls—all monitored through a beautiful Vue 3 frontend dashboard.

---

## 🚀 Key Features

*   **Intelligent Ingestion**: Recursively monitors source directories, matching erratic filenames (`m0v1e.YIFI.1080p.mkv`) against TMDB's API to extract pristine years and titles.
*   **Automated Renaming & Organization**: Standardizes output files and organizes them into clean folder hierarchies before moving them to a target drive.
*   **Hardware-Accelerated Transcoding**: A robust FFmpeg pipeline (powered by `Xabe.FFmpeg`) that supports NVIDIA NVENC, AMD AMF, and Intel QSV. Features a seamless software fallback if GPU drivers fail.
*   **Live SSE Progress Tracking**: A real-time WebSocket/SSE connection streams exact FFmpeg rendering percentages and ETA straight to the frontend.
*   **AI Subtitle Generation**: Automatically extracts audio, sends it to a local OpenAI Whisper docker container, and embeds newly generated `.srt` files directly into the media container.
*   **Premium Web Dashboard**: A bespoke Vue 3 SPA customized with Tailwind CSS, offering a responsive glassmorphic UI, real-time backend health monitoring, and advanced transcode configuration dials.

## 🏗️ Architecture Stack

### Backend
*   **Framework**: .NET 10 ASP.NET Core Web API
*   **Database**: PostgreSQL 15 (via Entity Framework Core 10.0 & Docker)
*   **Transcoding Engine**: `Xabe.FFmpeg`
*   **Subtitle Engine**: `ahmetoner/whisper-asr-webservice` (Local Docker)

### Frontend
*   **Framework**: Vue 3 (Composition API / Setup)
*   **Build Tool**: Vite
*   **Styling**: Tailwind CSS
*   **Routing**: Vue Router

## 🛠️ Quickstart Installation

1.  **Start Services**:
    Launch the PostgreSQL database and (optionally) the Whisper subtitle container using Docker Compose.
    ```bash
    docker-compose up -d
    ```

2.  **Start Backend**:
    ```bash
    cd Backend
    dotnet ef database update # Applies EF Core migrations
    dotnet run
    ```
    The API will listen on `http://localhost:5294`.

3.  **Start Frontend**:
    ```bash
    cd Frontend
    npm install
    npm run dev
    ```
    The dashboard will launch on `http://localhost:9999` and proxy API requests back to the frontend.

## ⚙️ Configuration & Hardware Support

You can configure global inputs, outputs, and TMDB API keys directly from the UI > Settings pane.
MovieManager supports NVENC, QSV, and AMF hardware encoders out of the box. Ensure your system has the correct proprietary drivers installed and the `ffmpeg` executable available in your `%PATH%` (Windows) or `/usr/bin/ffmpeg` (Linux).
