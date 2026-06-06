using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HttpPing.Services
{
    public class ProbeResult
    {
        public int StatusCode { get; set; }
        public string StatusDescription { get; set; } = "";
        public long ResponseTimeMs { get; set; }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class HttpProbeService : IDisposable
    {
        private readonly HttpClient _client;

        public HttpProbeService(int timeoutSeconds = 10, string userAgent = "HttpPing/1.0")
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
            };
            _client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        public async Task<ProbeResult> CheckAsync(string url, CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "http://" + url;
                }

                var response = await _client.GetAsync(url, ct).ConfigureAwait(false);
                sw.Stop();

                return new ProbeResult
                {
                    StatusCode = (int)response.StatusCode,
                    StatusDescription = response.ReasonPhrase ?? "",
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    IsSuccess = true
                };
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                return new ProbeResult
                {
                    StatusCode = 0,
                    StatusDescription = "超时",
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    IsSuccess = false,
                    ErrorMessage = "请求超时"
                };
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                var errMsg = ex.InnerException?.Message ?? ex.Message;
                return new ProbeResult
                {
                    StatusCode = 0,
                    StatusDescription = "连接失败",
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    IsSuccess = false,
                    ErrorMessage = errMsg
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new ProbeResult
                {
                    StatusCode = 0,
                    StatusDescription = "错误",
                    ResponseTimeMs = sw.ElapsedMilliseconds,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
