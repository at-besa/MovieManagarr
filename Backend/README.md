# MovieManager Backend

The robust, high-performance C# .NET API that powers the MovieManager file ingestion and transcoding pipeline. Designed for scalability and asynchronous execution, it leverages a strictly typed Entity Framework Core database layer tightly integrated into a PostgreSQL Docker container.

## 🚀 Core Microservices

- **FileSystemMonitorService**: Recursively scans arbitrary target directories for new, unknown `.mkv`, `.mp4` video files to stage for transcoding.
- **MediaAnalysisService**: Ingests video files to extract absolute framerate, container size, and video/audio stream codecs using FFmpeg native bindings.
- **TranscodingService (Xabe.FFmpeg)**: A resilient H.265 transcoding orchestrator. Supports `nvenc`, `qsv`, and `amf` encoders dynamically via user configuration. Includes an automated software fallback and streams raw render percentages back to the frontend via Server-Sent Events (SSE).
- **TmdbService**: Strips messy scene release names (e.g. `M0vie.Title.1080p.YIFI`) and fuzzy-matches against TMDB to extract standard Year and Title metadata.
- **OpenSubtitlesService / Whisper**: Communicates with the local `ahmetoner/whisper-asr-webservice` AI docker container to listen to media files locally and generate perfect English subtitle tracks.

## 🛠️ Tech Stack

- **Framework**: .NET 10.0 ASP.NET Core Web API
- **ORM**: Entity Framework Core 10.0 (Code-First Migrations)
- **Database**: PostgreSQL 15
- **Media Parsing**: `Xabe.FFmpeg`
- **Dependency Injection**: Scoped/Transient Service Locator pattern
- **Concurrency**: Asynchronous tasks and `ConcurrentDictionary` multi-threading

## ⚙️ Development Setup

The backend API defaults to listening on `http://localhost:5294`. It exposes its Swagger/OpenAPI spec when running in a `Development` environment.

### Prerequisites
- .NET 10 SDK
- FFmpeg installed locally and accessible via CLI
- Docker Desktop (for Postgres DB)

### Installation

1. From the project root, start the PostgreSQL container:
   ```bash
   docker-compose up -d db
   ```

2. Restore tools and install Entity Framework dependencies:
   ```bash
   cd Backend
   dotnet tool install --global dotnet-ef
   dotnet restore
   ```

3. Update the database schema to the latest EF migration:
   ```bash
   dotnet ef database update
   ```

4. Run the API:
   ```bash
   dotnet run
   ```

## 📂 Project Structure

- `Controllers/`: Exposes REST endpoints (`SettingsController`, `TranscodingController`, `SystemController`, `QueueController`).
- `Data/`: Contains the `AppDbContext` and Entity Framework Code-First Migrations.
- `Models/`: The strongly-typed entities representing API routes and DB tables.
- `Services/`: The core business logic orchestrators.
