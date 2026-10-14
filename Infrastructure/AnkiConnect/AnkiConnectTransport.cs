using System;
using System.Text;
using System.Text.Json;

namespace Infrastructure.AnkiConnect
{
    public class AnkiConnectTransport
    {
        private const string _ankiApi = "http://localhost:8765";
        private const int _maxAttempts = 3;

        private static readonly SemaphoreSlim _requestGate = new(1, 1);

        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _serializerOptions;

        public AnkiConnectTransport(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        internal async Task<T> SendRequestAsync<T>(string action, CancellationToken token, object? parameters = null)
        {
            await _requestGate.WaitAsync(token);

            try
            {
                for (var attempt = 1; attempt <= _maxAttempts; attempt++)
                {
                    try
                    {
                        return await SendRequestCoreAsync<T>(action, token, parameters);
                    }
                    catch (HttpRequestException) when (!token.IsCancellationRequested && attempt < _maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), token);
                    }
                    catch (HttpRequestException ex) when (!token.IsCancellationRequested)
                    {
                        throw new Exception($"AnkiConnect request failed. Action: {action}", ex);
                    }
                }

                throw new Exception($"AnkiConnect request failed. Action: {action}");
            }
            finally
            {
                _requestGate.Release();
            }
        }

        private async Task<T> SendRequestCoreAsync<T>(string action, CancellationToken token, object? parameters)
        {
            var request = new
            {
                action,
                version = 6,
                @params = parameters ?? new { }
            };

            var json = JsonSerializer.Serialize(request);

            using var response = await _httpClient.PostAsync(_ankiApi, new StringContent(json, Encoding.UTF8, "application/json"), token);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(token);
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.GetProperty("error").ValueKind != JsonValueKind.Null)
                throw new Exception(doc.RootElement.GetProperty("error").GetString());

            return JsonSerializer.Deserialize<T>(
                doc.RootElement.GetProperty("result"), _serializerOptions)!;
        }
    }
}
