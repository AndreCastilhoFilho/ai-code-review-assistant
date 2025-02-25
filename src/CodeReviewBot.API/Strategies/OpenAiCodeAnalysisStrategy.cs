using CodeReviewBot.API.Helpers;
using CodeReviewBot.API.Interfaces;
using CodeReviewBot.API.Services;
using CodeReviewBot.API.Shared;
using System.Text;
using System.Text.Json;

namespace CodeReviewBot.API.Strategies
{
    public class OpenAiCodeAnalysisStrategy : BaseCodeAnalysisStrategy
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAiCodeAnalysisStrategy(
            HttpClient httpClient, 
            GitHubService githubService, 
            IConfiguration configuration) : base(githubService)
        {
            _httpClient = httpClient;
            _openAiApiKey = configuration["OpenAi:ApiKey"] ?? string.Empty;
        }

        public override AiModelType ModelType => AiModelType.OpenAI;

        protected override async Task<List<CodeReviewComment>> AnalyzeCodeWithPrompt(string codeSnippet, string currentFileName)
        {
            var prompt = string.Format(Prompt.CodeReviewSystem, currentFileName, codeSnippet);
            
            var requestBody = new
            {
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = prompt }
                },
                temperature = 0.3,
                max_tokens = 1024
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiApiKey);

            var response = await _httpClient.PostAsync(OpenAiUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse.TryGetProperty("choices", out var choices) && 
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var messageContent))
            {
                return ParseAIResponse(messageContent.GetString());
            }

            throw new Exception("Unexpected response format from OpenAI API.");
        }
    }
}
