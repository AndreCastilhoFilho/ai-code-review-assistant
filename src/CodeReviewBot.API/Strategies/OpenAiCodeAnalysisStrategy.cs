using CodeReviewBot.API.Helpers;
using CodeReviewBot.API.Interfaces;
using CodeReviewBot.API.Shared;
using CodeReviewBot.API.Utils;
using System.Text;
using System.Text.Json;

namespace CodeReviewBot.API.Strategies
{
    public class OpenAiCodeAnalysisStrategy : ICodeAnalysisStrategy
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";

        public OpenAiCodeAnalysisStrategy(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _openAiApiKey = configuration["OpenAi:ApiKey"] ?? string.Empty;
        }

        public AiModelType ModelType => AiModelType.OpenAI;

        public async Task<string> AnalyzeCodeAsync(string codeSnippet)
        {
            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
              {
                new { role = "system", content = Prompt.System },
                new { role = "user", content = $"Please analyze the following code:\n{codeSnippet}" }
            }
                ,
                store = true
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiApiKey);

            var response = await _httpClient.PostAsync(OpenAiUrl, content);

            var responseString = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (!response.IsSuccessStatusCode)
                throw new Exception(jsonResponse.ToString());

            if (jsonResponse[0].TryGetProperty("generated_text", out var generatedText))
            {
                return StringFormatHelper.Format(generatedText.GetString().Trim(), Prompt.System);
            }

            throw new Exception("Unexpected response format from Hugging Face API.");

        }
    }
}
