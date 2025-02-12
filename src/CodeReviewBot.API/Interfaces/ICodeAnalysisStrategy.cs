using CodeReviewBot.API.Shared;

namespace CodeReviewBot.API.Interfaces
{
    public interface ICodeAnalysisStrategy
    {
        AiModelType ModelType { get; }
        Task<string> AnalyzeCodeAsync(string codeSnippet);
    }

}
