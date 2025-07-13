using JenkinsAgent.Models;

namespace JenkinsAgent.Services;

/// <summary>
/// Thread-safe HttpClient factory that creates isolated instances
/// </summary>
public class SafeHttpClientFactory
{
    public static HttpClient CreateClient(JenkinsConfig? config = null)
    {
        var handler = new HttpClientHandler();

        // SSL doğrulamasını ayarlıyorum, config'e göre
        if (config?.IgnoreSslErrors == true)
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }

        var client = new HttpClient(handler);

        if (config != null)
        {
            client.BaseAddress = new Uri(config.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
        }

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Jenkins-Agent-WPF/1.0");

        return client;
    }

    public static HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string requestUri, JenkinsConfig config)
    {
        var request = new HttpRequestMessage(method, requestUri);

        if (config.IsValid)
        {
            var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{config.Username}:{config.ApiToken}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);
        }

        return request;
    }
}
