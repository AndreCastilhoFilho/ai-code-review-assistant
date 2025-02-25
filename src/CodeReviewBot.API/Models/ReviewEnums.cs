namespace CodeReviewBot.API.Models
{
    public enum ReviewCategory
    {
        SECURITY,
        PERFORMANCE,
        BEST_PRACTICES,
        BUG,
        TEST
    }

    public enum ReviewSeverity
    {
        HIGH,
        MEDIUM,
        LOW
    }
} 