namespace JenkinsAgent.Models;

/// <summary>
/// Jenkins bağlantı konfigürasyonu
/// </summary>
public class JenkinsConfig
{
    /// <summary>
    /// Jenkins sunucu URL'i (örn: http://jenkins.mustafagenc.com)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıcı adı
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// API token (şifre yerine kullanılır)
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// SSL hatalarını yoksay
    /// </summary>
    public bool IgnoreSslErrors { get; set; } = true;

    /// <summary>
    /// SSL sertifikasını doğrula (IgnoreSslErrors'ın tersi)
    /// </summary>
    public bool ValidateSSL
    {
        get => !IgnoreSslErrors;
        set => IgnoreSslErrors = !value;
    }

    /// <summary>
    /// Timeout süresi (saniye)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Otomatik yenileme süresi (saniye)
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Favori job'ları sakla
    /// </summary>
    public List<string> FavoriteJobs { get; set; } = new();

    /// <summary>
    /// Bildirimler açık mı
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Sadece başarısız build'ler için bildirim
    /// </summary>
    public bool OnlyFailureNotifications { get; set; } = false;

    /// <summary>
    /// Konfigürasyonun geçerli olup olmadığını kontrol eder
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(BaseUrl) &&
                          !string.IsNullOrWhiteSpace(Username) &&
                          !string.IsNullOrWhiteSpace(ApiToken);

    /// <summary>
    /// Basic authentication header değeri
    /// </summary>
    public string GetAuthHeaderValue()
    {
        if (!IsValid) return string.Empty;

        var credentials = $"{Username}:{ApiToken}";
        var encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        return $"Basic {encodedCredentials}";
    }
}
