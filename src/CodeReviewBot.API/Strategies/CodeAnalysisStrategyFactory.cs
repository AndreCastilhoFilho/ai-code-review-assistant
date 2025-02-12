using CodeReviewBot.API.Interfaces;
using CodeReviewBot.API.Shared;

namespace CodeReviewBot.API.Strategies
{
    public class CodeAnalysisStrategyFactory
    {
        private readonly IEnumerable<ICodeAnalysisStrategy> _strategies;

        public CodeAnalysisStrategyFactory(IEnumerable<ICodeAnalysisStrategy> strategies)
        {
            _strategies = strategies;
        }

        public ICodeAnalysisStrategy GetStrategy(AiModelType modelType)
        {
            return _strategies.FirstOrDefault(s => s.ModelType == modelType)
                   ?? throw new ArgumentException($"No strategy found for model type {modelType}");
        }
    }
}
