using System.Text.Json;
using System.Text;
using CodeReviewBot.API.Interfaces;
using CodeReviewBot.API.Utils;
using CodeReviewBot.API.Shared;
using CodeReviewBot.API.Helpers;

namespace CodeReviewBot.API.Strategies
{
    public class HuggingFaceCodeAnalysisStrategy : ICodeAnalysisStrategy

    {
        private readonly HttpClient _httpClient;
        private readonly string _huggingFaceApiKey;
        private const string HuggingFaceUrl = "https://api-inference.huggingface.co/models/mistralai/Mistral-7B-Instruct-v0.3";

        public AiModelType ModelType => AiModelType.HuggingFace;

        public HuggingFaceCodeAnalysisStrategy(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _huggingFaceApiKey = configuration["HuggingFaceApi:ApiKey"];
        }

        public async Task<string> AnalyzeCodeAsync(string codeSnippet)
        {
            var prompt = $"{Prompt.System}  Code snippet:\n{codeSnippet}";

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
            responseString = responseString.Trim().Trim('"');

            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

            if (jsonResponse[0].TryGetProperty("generated_text", out var generatedText))
            {
                return StringFormatHelper.Format(generatedText.GetString().Trim(), prompt);
            }

            throw new Exception("Unexpected response format from Hugging Face API.");

        }

      

    }
}

