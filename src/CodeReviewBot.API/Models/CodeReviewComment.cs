namespace CodeReviewBot.API.Models
{
    public class CodeReviewComment
    {
        public string FileName { get; set; }
        public int LineNumber { get; set; }
        public string Category { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
    }
} 