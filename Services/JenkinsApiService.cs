using JenkinsAgent.Models;

namespace JenkinsAgent.Services;

/// <summary>
/// Jenkins API servisi implementasyonu - Thread-safe ve HttpClient conflict'siz
/// </summary>
public class JenkinsApiService : IJenkinsApiService
{
    private JenkinsConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _apiSemaphore = new(1, 1);

    private string? _crumbValue;
    private string? _crumbField;

    public JenkinsApiService(HttpClient _, JenkinsConfig config)
    {
        // HttpClient parametresini ignore ediyoruz, kendi factory'mizi kullanacağız
        _config = config;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public void UpdateConfiguration(JenkinsConfig config)
    {
        _config = config;
        System.Diagnostics.Debug.WriteLine($"Jenkins config updated: {config.BaseUrl}");
    }

    public async Task<bool> TestConnectionAsync()
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            System.Diagnostics.Debug.WriteLine($"Testing connection to: {_config.BaseUrl}");
            System.Diagnostics.Debug.WriteLine($"Username: {_config.Username}");
            System.Diagnostics.Debug.WriteLine($"HasApiToken: {!string.IsNullOrEmpty(_config.ApiToken)}");

            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get, "/api/json", _config);

            System.Diagnostics.Debug.WriteLine($"Request URL: {client.BaseAddress}{request.RequestUri}");

            var response = await client.SendAsync(request);

            System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"Response Headers: {response.Headers}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Response Content: {responseContent}");
            }

            var result = response.IsSuccessStatusCode;
            System.Diagnostics.Debug.WriteLine($"Connection test result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TestConnectionAsync error: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<JenkinsInfo?> GetJenkinsInfoAsync()
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/json?tree=description,jobs[name,url,color,buildable],views[name,url],version,numExecutors",
                _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JenkinsInfo>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetJenkinsInfoAsync error: {ex.Message}");
            return null;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<List<JenkinsJob>> GetJobsAsync()
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/json?tree=jobs[name,url,color,buildable,inQueue,description,lastBuild[number,result,timestamp,duration,building],lastSuccessfulBuild[number],lastFailedBuild[number]]",
                _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new List<JenkinsJob>();

            var json = await response.Content.ReadAsStringAsync();
            var jenkinsInfo = JsonSerializer.Deserialize<JenkinsInfo>(json, _jsonOptions);

            return jenkinsInfo?.Jobs ?? new List<JenkinsJob>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetJobsAsync error: {ex.Message}");
            return new List<JenkinsJob>();
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<JenkinsJob?> GetJobAsync(string jobName)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(
                HttpMethod.Get,
                $"/job/{jobName}/api/json?tree=name,url,color,buildable,inQueue,description,builds[number,result,timestamp,duration,building,url],lastBuild[number,result,timestamp,duration,building],lastSuccessfulBuild[number],lastFailedBuild[number]",
                _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JenkinsJob>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetJobAsync error: {ex.Message}");
            return null;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<bool> BuildJobAsync(string jobName)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            LogDebug($"BuildJobAsync - Job Name: '{jobName}'");

            // Önce crumb al
            var crumbObtained = await GetCrumbAsync();
            LogDebug($"BuildJobAsync - Crumb obtained: {crumbObtained}");

            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Post, $"/job/{Uri.EscapeDataString(jobName)}/buildWithParameters", _config);

            // CSRF koruması için crumb ve boş form data gönder
            var formData = new List<KeyValuePair<string, string>>();

            // Crumb varsa form data'ya ekle
            if (crumbObtained && !string.IsNullOrEmpty(_crumbField) && !string.IsNullOrEmpty(_crumbValue))
            {
                formData.Add(new KeyValuePair<string, string>(_crumbField, _crumbValue));
                LogDebug($"BuildJobAsync - Added crumb to form data: {_crumbField} = {_crumbValue}");
            }

            // FormUrlEncodedContent ile POST body oluştur
            request.Content = new FormUrlEncodedContent(formData);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            LogDebug($"BuildJobAsync - Request URL: {client.BaseAddress}/job/{Uri.EscapeDataString(jobName)}/buildWithParameters");
            LogDebug($"BuildJobAsync - Method: {request.Method}");
            LogDebug($"BuildJobAsync - Form data count: {formData.Count}");

            var response = await client.SendAsync(request);

            LogDebug($"BuildJobAsync - Response Status: {response.StatusCode} ({(int)response.StatusCode})");
            LogDebug($"BuildJobAsync - Success: {response.IsSuccessStatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                LogDebug($"BuildJobAsync - Error Response: {responseContent}");
            }
            else
            {
                LogDebug("BuildJobAsync - Job build triggered successfully!");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogDebug($"BuildJobAsync error: {ex.GetType().Name}: {ex.Message}");
            LogDebug($"BuildJobAsync stack trace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<bool> BuildJobWithParametersAsync(string jobName, Dictionary<string, string> parameters)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Post, $"/job/{jobName}/buildWithParameters", _config);
            request.Content = new FormUrlEncodedContent(parameters);

            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BuildJobWithParametersAsync error: {ex.Message}");
            return false;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<JenkinsBuild?> GetBuildAsync(string jobName, int buildNumber)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get, $"/job/{jobName}/{buildNumber}/api/json", _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JenkinsBuild>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetBuildAsync error: {ex.Message}");
            return null;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<string> GetBuildLogsAsync(string jobName, int buildNumber)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get, $"/job/{jobName}/{buildNumber}/consoleText", _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return "Log alınamadı.";

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetBuildLogsAsync error: {ex.Message}");
            return $"Log alınırken hata oluştu: {ex.Message}";
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<JenkinsBuild?> GetLastBuildAsync(string jobName)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get, $"/job/{jobName}/lastBuild/api/json", _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<JenkinsBuild>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetLastBuildAsync error: {ex.Message}");
            return null;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<List<JenkinsQueueItem>> GetQueueAsync()
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get, "/queue/api/json", _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return new List<JenkinsQueueItem>();

            var json = await response.Content.ReadAsStringAsync();
            var queueResponse = JsonSerializer.Deserialize<QueueResponse>(json, _jsonOptions);

            return queueResponse?.Items ?? new List<JenkinsQueueItem>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetQueueAsync error: {ex.Message}");
            return new List<JenkinsQueueItem>();
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<List<ComputerExecutor>> GetExecutorsAsync()
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            LogDebug("GetExecutorsAsync - Fetching executor information");

            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get,
                "/computer/api/json?tree=computer[displayName,idle,offline,executors[idle,likelyStuck,number,progress,currentExecutable[number,url,fullDisplayName]]]",
                _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                LogDebug($"GetExecutorsAsync - Error response: {response.StatusCode}");
                return new List<ComputerExecutor>();
            }

            var json = await response.Content.ReadAsStringAsync();
            LogDebug($"GetExecutorsAsync - Raw JSON length: {json.Length}");

            var computerResponse = JsonSerializer.Deserialize<ComputerResponse>(json, _jsonOptions);

            var executors = computerResponse?.Computer ?? new List<ComputerExecutor>();
            LogDebug($"GetExecutorsAsync - Found {executors.Count} computers");

            return executors;
        }
        catch (Exception ex)
        {
            LogDebug($"GetExecutorsAsync error: {ex.GetType().Name}: {ex.Message}");
            return new List<ComputerExecutor>();
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<List<JenkinsJob>> GetFoldersAsync()
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(
                HttpMethod.Get,
                "/api/json?tree=jobs[name,url,_class,jobs[name,url,_class]]",
                _config);

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"GetFoldersAsync failed: {response.StatusCode}");
                return new List<JenkinsJob>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var jenkinsInfo = JsonSerializer.Deserialize<JenkinsInfo>(jsonContent, _jsonOptions);

            if (jenkinsInfo?.Jobs == null) return new List<JenkinsJob>();

            // Sadece folder'ları döndür
            var folders = jenkinsInfo.Jobs.Where(job => job.IsAutomaticFolder).ToList();

            // Full path'leri ve IsFolder'ı ayarla
            foreach (var folder in folders)
            {
                folder.FullPath = folder.Name;
                folder.IsFolder = true;
            }

            System.Diagnostics.Debug.WriteLine($"Found {folders.Count} folders");
            return folders;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GetFoldersAsync error: {ex.Message}");
            return new List<JenkinsJob>();
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<List<JenkinsJob>> GetJobsInFolderAsync(string folderPath)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            var encodedPath = Uri.EscapeDataString(folderPath);
            var apiPath = $"/job/{encodedPath}/api/json?tree=jobs[name,url,color,buildable,_class,lastBuild[number,timestamp,result],description]";

            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get, apiPath, _config);

            LogDebug($"GetJobsInFolderAsync - Getting jobs for folder: '{folderPath}'");
            LogDebug($"GetJobsInFolderAsync - API Path: {apiPath}");
            LogDebug($"GetJobsInFolderAsync - Full URL: {client.BaseAddress}{apiPath}");

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                LogDebug($"GetJobsInFolderAsync failed: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                LogDebug($"GetJobsInFolderAsync - Error content: {errorContent}");
                return new List<JenkinsJob>();
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            LogDebug($"GetJobsInFolderAsync - Response length: {jsonContent.Length} characters");

            var folderInfo = JsonSerializer.Deserialize<JenkinsJob>(jsonContent, _jsonOptions);

            if (folderInfo?.Jobs == null)
            {
                LogDebug("GetJobsInFolderAsync - No jobs found in folder response");
                return new List<JenkinsJob>();
            }

            // Sadece gerçek job'ları döndür (folder'ları değil)
            var jobs = folderInfo.Jobs.Where(job => job.IsJob || !job.IsAutomaticFolder).ToList();
            LogDebug($"GetJobsInFolderAsync - Total items in response: {folderInfo.Jobs.Count}, actual jobs: {jobs.Count}");

            // Full path'leri ayarla
            foreach (var job in jobs)
            {
                job.FullPath = $"{folderPath}/{job.Name}";
                LogDebug($"GetJobsInFolderAsync - Job: '{job.Name}', FullPath: '{job.FullPath}', Color: '{job.Color}', Status: {job.Status}, IsJob: {job.IsJob}, Buildable: {job.Buildable}, InQueue: {job.InQueue}");
            }

            LogDebug($"GetJobsInFolderAsync - Successfully found {jobs.Count} jobs in folder '{folderPath}'");
            return jobs;
        }
        catch (Exception ex)
        {
            LogDebug($"GetJobsInFolderAsync error: {ex.GetType().Name}: {ex.Message}");
            LogDebug($"GetJobsInFolderAsync stack trace: {ex.StackTrace}");
            return new List<JenkinsJob>();
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    private static void LogDebug(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        try
        {
            var logFile = Path.Combine(Path.GetTempPath(), "jenkins-agent-debug.log");
            File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}\n");
        }
        catch
        {
            // Ignore logging errors
        }
    }

    public async Task<bool> BuildJobInFolderAsync(string folderName, string jobName)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            LogDebug($"BuildJobInFolderAsync - Folder: '{folderName}', Job: '{jobName}'");

            // Önce crumb al
            var crumbObtained = await GetCrumbAsync();
            LogDebug($"BuildJobInFolderAsync - Crumb obtained: {crumbObtained}");

            // Jenkins'te folder/job path için URL encoding gerekebilir
            var encodedFolderName = Uri.EscapeDataString(folderName);
            var encodedJobName = Uri.EscapeDataString(jobName);

            // Folder içindeki job için URL: /job/FolderName/job/JobName/buildWithParameters
            var jobPath = $"/job/{encodedFolderName}/job/{encodedJobName}/buildWithParameters";

            LogDebug($"BuildJobInFolderAsync - Job Path: {jobPath}");

            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Post, jobPath, _config);

            // CSRF koruması için crumb ve boş form data gönder
            var formData = new List<KeyValuePair<string, string>>();

            // Crumb varsa form data'ya ekle
            if (crumbObtained && !string.IsNullOrEmpty(_crumbField) && !string.IsNullOrEmpty(_crumbValue))
            {
                formData.Add(new KeyValuePair<string, string>(_crumbField, _crumbValue));
                LogDebug($"BuildJobInFolderAsync - Added crumb to form data: {_crumbField} = {_crumbValue}");
            }

            // FormUrlEncodedContent ile POST body oluştur
            request.Content = new FormUrlEncodedContent(formData);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            LogDebug($"BuildJobInFolderAsync - Full URL: {client.BaseAddress}{jobPath}");
            LogDebug($"BuildJobInFolderAsync - Method: {request.Method}");
            LogDebug($"BuildJobInFolderAsync - Form data count: {formData.Count}");

            var response = await client.SendAsync(request);

            LogDebug($"BuildJobInFolderAsync - Response Status: {response.StatusCode} ({(int)response.StatusCode})");
            LogDebug($"BuildJobInFolderAsync - Response Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"))}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                LogDebug($"BuildJobInFolderAsync - Error Response Content: {responseContent}");

                // 404 ise URL'in yanlış olduğunu gösterir
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    LogDebug($"BuildJobInFolderAsync - 404 Error: Job or folder not found. Check path: {jobPath}");
                }
            }
            else
            {
                LogDebug($"BuildJobInFolderAsync - Success! Job build triggered.");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogDebug($"BuildJobInFolderAsync error: {ex.GetType().Name}: {ex.Message}");
            LogDebug($"BuildJobInFolderAsync stack trace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<bool> BuildJobInFolderAlternativeAsync(string folderName, string jobName)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            LogDebug($"BuildJobInFolderAlternativeAsync - Folder: '{folderName}', Job: '{jobName}'");

            // Önce crumb al
            var crumbObtained = await GetCrumbAsync();
            LogDebug($"BuildJobInFolderAlternativeAsync - Crumb obtained: {crumbObtained}");

            // Alternative path format: /job/FolderName/job/JobName/build
            // Sometimes folder path needs to be URL encoded or use different separator
            var jobPath = $"/job/{Uri.EscapeDataString(folderName)}/job/{Uri.EscapeDataString(jobName)}/build";

            LogDebug($"BuildJobInFolderAlternativeAsync - Trying path: {jobPath}");

            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Post, jobPath, _config);

            // Crumb varsa header'a ekle
            if (crumbObtained && !string.IsNullOrEmpty(_crumbField) && !string.IsNullOrEmpty(_crumbValue))
            {
                request.Headers.Add(_crumbField, _crumbValue);
                LogDebug($"BuildJobInFolderAlternativeAsync - Added crumb header: {_crumbField} = {_crumbValue}");
            }

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                // Try alternative format: /view/FolderName/job/JobName/build
                var alternativePath = $"/view/{Uri.EscapeDataString(folderName)}/job/{Uri.EscapeDataString(jobName)}/build";
                LogDebug($"BuildJobInFolderAlternativeAsync - Trying alternative path: {alternativePath}");

                using var alternativeRequest = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Post, alternativePath, _config);

                // Crumb header'ını alternatif request'e de ekle
                if (crumbObtained && !string.IsNullOrEmpty(_crumbField) && !string.IsNullOrEmpty(_crumbValue))
                {
                    alternativeRequest.Headers.Add(_crumbField, _crumbValue);
                }

                var alternativeResponse = await client.SendAsync(alternativeRequest);

                if (!alternativeResponse.IsSuccessStatusCode)
                {
                    // Try third format: direct job name with folder prefix
                    var directPath = $"/job/{Uri.EscapeDataString($"{folderName}/{jobName}")}/build";
                    LogDebug($"BuildJobInFolderAlternativeAsync - Trying direct path: {directPath}");

                    using var directRequest = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Post, directPath, _config);

                    // Crumb header'ını direct request'e de ekle
                    if (crumbObtained && !string.IsNullOrEmpty(_crumbField) && !string.IsNullOrEmpty(_crumbValue))
                    {
                        directRequest.Headers.Add(_crumbField, _crumbValue);
                    }

                    var directResponse = await client.SendAsync(directRequest);

                    LogDebug($"BuildJobInFolderAlternativeAsync - Direct response: {directResponse.StatusCode}");
                    return directResponse.IsSuccessStatusCode;
                }

                LogDebug($"BuildJobInFolderAlternativeAsync - Alternative response: {alternativeResponse.StatusCode}");
                return alternativeResponse.IsSuccessStatusCode;
            }

            LogDebug($"BuildJobInFolderAlternativeAsync - First attempt response: {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BuildJobInFolderAlternativeAsync error: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    private async Task<bool> GetCrumbAsync()
    {
        try
        {
            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get, "/crumbIssuer/api/json", _config);

            LogDebug("GetCrumbAsync - Requesting CSRF crumb");
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                LogDebug($"GetCrumbAsync - Failed to get crumb: {response.StatusCode}");
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();
            LogDebug($"GetCrumbAsync - Crumb response: {json}");

            var crumbData = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _jsonOptions);

            if (crumbData != null && crumbData.ContainsKey("crumb") && crumbData.ContainsKey("crumbRequestField"))
            {
                _crumbValue = crumbData["crumb"]?.ToString();
                _crumbField = crumbData["crumbRequestField"]?.ToString();
                LogDebug($"GetCrumbAsync - Got crumb: {_crumbField} = {_crumbValue}");
                return true;
            }

            LogDebug("GetCrumbAsync - Invalid crumb response format");
            return false;
        }
        catch (Exception ex)
        {
            LogDebug($"GetCrumbAsync - Error: {ex.Message}");
            return false;
        }
    }

    private class QueueResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("items")]
        public List<JenkinsQueueItem> Items { get; set; } = new();
    }

    public async Task<bool> StopJobAsync(string jobName, int buildNumber)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            LogDebug($"StopJobAsync - Job Name: '{jobName}', Build Number: {buildNumber}");

            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Post, $"/job/{Uri.EscapeDataString(jobName)}/{buildNumber}/stop", _config);

            // CSRF token gerekebilir
            bool crumbObtained = await GetCrumbAsync();
            LogDebug($"StopJobAsync - Crumb obtained: {crumbObtained}");

            if (crumbObtained && !string.IsNullOrEmpty(_crumbField) && !string.IsNullOrEmpty(_crumbValue))
            {
                request.Headers.Add(_crumbField, _crumbValue);
                LogDebug($"StopJobAsync - Added crumb header: {_crumbField} = {_crumbValue}");
            }

            LogDebug($"StopJobAsync - Request URL: {client.BaseAddress}job/{Uri.EscapeDataString(jobName)}/{buildNumber}/stop");

            using var response = await client.SendAsync(request);
            LogDebug($"StopJobAsync - Response Status: {response.StatusCode} ({(int)response.StatusCode})");
            LogDebug($"StopJobAsync - Success: {response.IsSuccessStatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                LogDebug($"StopJobAsync - Error Response: {responseContent}");
            }
            else
            {
                LogDebug("StopJobAsync - Job stopped successfully!");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogDebug($"StopJobAsync error: {ex.GetType().Name}: {ex.Message}");
            LogDebug($"StopJobAsync stack trace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<bool> StopJobInFolderAsync(string folderName, string jobName, int buildNumber)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            LogDebug($"StopJobInFolderAsync - Folder: '{folderName}', Job Name: '{jobName}', Build Number: {buildNumber}");

            using var client = SafeHttpClientFactory.CreateClient(_config);

            // Folder içindeki job için URL formatı: /job/FolderName/job/JobName/BuildNumber/stop
            var encodedFolderName = Uri.EscapeDataString(folderName);
            var encodedJobName = Uri.EscapeDataString(jobName);
            var stopUrl = $"/job/{encodedFolderName}/job/{encodedJobName}/{buildNumber}/stop";

            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Post, stopUrl, _config);

            // CSRF token gerekebilir
            bool crumbObtained = await GetCrumbAsync();
            LogDebug($"StopJobInFolderAsync - Crumb obtained: {crumbObtained}");

            if (crumbObtained && !string.IsNullOrEmpty(_crumbField) && !string.IsNullOrEmpty(_crumbValue))
            {
                request.Headers.Add(_crumbField, _crumbValue);
                LogDebug($"StopJobInFolderAsync - Added crumb header: {_crumbField} = {_crumbValue}");
            }

            LogDebug($"StopJobInFolderAsync - Request URL: {client.BaseAddress}{stopUrl}");

            using var response = await client.SendAsync(request);
            LogDebug($"StopJobInFolderAsync - Response Status: {response.StatusCode} ({(int)response.StatusCode})");
            LogDebug($"StopJobInFolderAsync - Success: {response.IsSuccessStatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                LogDebug($"StopJobInFolderAsync - Error Response: {responseContent}");
            }
            else
            {
                LogDebug("StopJobInFolderAsync - Job stopped successfully!");
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            LogDebug($"StopJobInFolderAsync error: {ex.GetType().Name}: {ex.Message}");
            LogDebug($"StopJobInFolderAsync stack trace: {ex.StackTrace}");
            return false;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }

    public async Task<string> GetMetricsAsync(string metricsUrl)
    {
        await _apiSemaphore.WaitAsync();
        try
        {
            LogDebug($"GetMetricsAsync - URL: {metricsUrl}");

            using var client = SafeHttpClientFactory.CreateClient(_config);
            using var request = SafeHttpClientFactory.CreateAuthenticatedRequest(HttpMethod.Get, metricsUrl, _config);

            var response = await client.SendAsync(request);

            LogDebug($"GetMetricsAsync - Response Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                LogDebug($"GetMetricsAsync - Failed to get metrics: {response.StatusCode}");
                return string.Empty;
            }

            var content = await response.Content.ReadAsStringAsync();
            LogDebug($"GetMetricsAsync - Response length: {content.Length} characters");

            return content;
        }
        catch (Exception ex)
        {
            LogDebug($"GetMetricsAsync error: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            _apiSemaphore.Release();
        }
    }
}
