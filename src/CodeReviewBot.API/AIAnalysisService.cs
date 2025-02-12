using System.Text.Json;
using System.Text;

namespace CodeReviewBot.API
{
    public class AIAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly string _openAiApiKey;
        private const string OpenAiUrl = "https://api.openai.com/v1/chat/completions";

        public AIAnalysisService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _openAiApiKey = configuration["OpenAi:ApiKey"] ?? string.Empty;
        }

        public async Task<string> AnalyzeCode(string codeSnippet)
        {
            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
              {
                new { role = "system", content = "You are a helpful assistant that provides code analysis." },
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

            return jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString().Trim();
        }
    }
}
