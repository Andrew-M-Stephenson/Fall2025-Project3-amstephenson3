// Services/AiReviewsService.cs
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Fall2025_Project3_amstephenson3.Services
{
    public class AiReviewsService
    {
        private readonly HttpClient _http;
        private readonly string _endpoint;
        private readonly string _deployment;
        private readonly string _apiKey;
        private readonly string _apiVersion;

        public AiReviewsService(IConfiguration config, IHttpClientFactory httpFactory)
        {
            _endpoint = config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("Missing AzureOpenAI:Endpoint");
            _deployment = config["AzureOpenAI:Deployment"] ?? throw new InvalidOperationException("Missing AzureOpenAI:Deployment");
            _apiKey = config["AzureOpenAI:Key"] ?? throw new InvalidOperationException("Missing AzureOpenAI:Key");
            _apiVersion = config["AzureOpenAI:ApiVersion"] ?? "2024-12-01-preview";

            _http = httpFactory.CreateClient(nameof(AiReviewsService));
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _http.DefaultRequestHeaders.Add("api-key", _apiKey);
        }

        public async Task<List<string>> GenerateReviewsAsync(string movieTitle, IEnumerable<string>? cast)
        {
            var url = $"{_endpoint.TrimEnd('/')}/openai/deployments/{_deployment}/chat/completions?api-version={_apiVersion}";

            var castLine = cast is not null && cast.Any()
                ? $"Cast: {string.Join(", ", cast.Take(8))}."
                : "Cast: (not provided).";

            var payload = new
            {
                messages = new object[]
                {
                    new { role = "system", content = "You are a helpful assistant. Return ONLY valid JSON; no extra text." },
                    new
                    {
                        role = "user",
                        content = $@"
Generate ten short, varied, natural-language mini-reviews for the movie '{movieTitle}'.
{castLine}
Keep each review to 1–2 sentences, a mix of positive/neutral/lightly critical. No hashtags or @mentions.
Return ONLY JSON with this exact shape:
{{ ""reviews"": [""..."",""..."" ] }}
Ensure there are exactly 10 strings in ""reviews"" and no extra properties."
                    }
                },
                temperature = 0.7,
                max_tokens = 800,
                response_format = new { type = "json_object" },
                model = _deployment
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Azure OpenAI error {resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var innerText = doc.RootElement
                               .GetProperty("choices")[0]
                               .GetProperty("message")
                               .GetProperty("content")
                               .GetString() ?? "";

            try
            {
                using var inner = JsonDocument.Parse(innerText);
                var reviews = inner.RootElement.GetProperty("reviews")
                    .EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Take(10)
                    .ToList();

                return reviews;
            }
            catch
            {
                // Fallback if model ignored JSON mode: split lines
                return FallbackExtractLines(innerText, 10);
            }
        }

        private static List<string> FallbackExtractLines(string text, int max)
        {
            var lines = new List<string>();
            foreach (var line in text.Split('\n'))
            {
                var s = line.Trim().TrimStart('-', '*').Trim();
                if (!string.IsNullOrWhiteSpace(s)) lines.Add(s);
                if (lines.Count >= max) break;
            }
            return lines;
        }
    }
}
