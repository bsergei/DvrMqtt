using Microsoft.Extensions.Options;

namespace Dvr;

public class DvrHttpWebService : IDvrHttpWebService, IDisposable
{
    private readonly DvrOptions options_;
    private readonly HttpClient httpClient_;

    public DvrHttpWebService(
        IOptions<DvrOptions> options,
        IHttpClientFactory clientFactory)
    {
        options_ = options.Value;
        httpClient_ = clientFactory.CreateClient(nameof(DvrHttpWebService));
    }

    public void Dispose()
    {
        httpClient_.Dispose();
    }

    public async Task<byte[]> GetCameraSnapshot()
    {
        var bytes = await httpClient_.GetByteArrayAsync(
            $"http://{options_.HostIp}/webcapture.jpg?command=snap&channel=1&user={options_.User}&password={options_.Password}");

        return bytes;
    }
}