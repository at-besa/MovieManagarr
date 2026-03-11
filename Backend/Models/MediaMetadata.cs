namespace Backend.Models
{
    public class MediaMetadata
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
        public string MediaType { get; set; } = string.Empty; // "Movie" or "Series"
        public string PosterUrl { get; set; } = string.Empty;
        public string Overview { get; set; } = string.Empty;
    }
}
