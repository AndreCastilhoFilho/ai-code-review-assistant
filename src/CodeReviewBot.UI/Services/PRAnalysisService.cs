namespace CodeReviewBot.UI
{
    using CodeReviewBot.API.Shared;
    using System.Net.Http;
    using System.Threading.Tasks;

    namespace AIReview.UI.Services
    {
        public class PRAnalysisService
        {
            private readonly HttpClient _httpClient;

            public PRAnalysisService(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public async Task<string> AnalyzePR(string prUrl, AiModelType type)
            {
                var fullUrl = $"analyze/{Uri.EscapeDataString(prUrl)}?modelType={type}";

                var response = await _httpClient.GetAsync(fullUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API Error: {response.StatusCode} - {errorMessage}");
                }

                return await response.Content.ReadAsStringAsync();
            }

        }
    }
}
