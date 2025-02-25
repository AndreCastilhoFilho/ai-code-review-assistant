namespace CodeReviewBot.API.Models
{
    public class DiffInfo
    {
        public List<DiffChunk> Chunks { get; } = new();
    }

    public class DiffChunk
    {
        public int NewStart { get; set; }
        public string Content { get; set; }
    }
} 