using CodeReviewBot.API.Shared;
using CodeReviewBot.API.Strategies;

namespace CodeReviewBot.API.Interfaces
{
    public interface ICodeAnalysisStrategy
    {
        AiModelType ModelType { get; }
        Task AnalyzeAndReviewPR(string owner, string repo, int prNumber);
    }

}
