using CodeReviewBot.API.Helpers;
using CodeReviewBot.API.Services;
using CodeReviewBot.API.Shared;
using System.Text;
using System.Text.Json;

namespace CodeReviewBot.API.Strategies
{
    public class HuggingFaceCodeAnalysisStrategy : BaseCodeAnalysisStrategy
    {
        private readonly HttpClient _httpClient;
        private readonly string _huggingFaceApiKey;
        private const string HuggingFaceUrl = "https://api-inference.huggingface.co/models/mistralai/Mistral-7B-Instruct-v0.3";

        public HuggingFaceCodeAnalysisStrategy(
            HttpClient httpClient, 
            GitHubService githubService, 
            IConfiguration configuration) : base(githubService)
        {
            _httpClient = httpClient;
            _huggingFaceApiKey = configuration["HuggingFaceApi:ApiKey"];
        }

        public override AiModelType ModelType => AiModelType.HuggingFace;

        protected override async Task<List<CodeReviewComment>> AnalyzeCodeWithPrompt(string codeSnippet, string currentFileName)
        {
            var prompt = string.Format(Prompt.CodeReviewSystem, currentFileName, codeSnippet);
            var requestBody = new
            {
                inputs = prompt,
                parameters = new
                {
                    max_new_tokens = 1024,
                    temperature = 0.3,
                    top_p = 0.9
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _huggingFaceApiKey);

            var response = await _httpClient.PostAsync(HuggingFaceUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse[0].TryGetProperty("generated_text", out var generatedText))
            {
                return ParseAIResponse(generatedText.GetString());
            }

            throw new Exception("Unexpected response format from Hugging Face API.");
        }
    }
}

