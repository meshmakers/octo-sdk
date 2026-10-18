using System.Net;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.BotServices;

namespace Sdk.ServiceClient.Tests.BotServices;

/// <summary>
///     The tus upload of <see cref="BotServicesClient" /> against a tus test server that enforces a request body
///     limit like the ingress of the managed environments does (AB#6786, AB#6970): octo-cli and MCP failed on a
///     3.48 GB backup because the whole file went out in one PATCH that the ingress refused with 413.
/// </summary>
public class BotServicesClientTusUploadTests : IClassFixture<TusTestServer>
{
    private const long MiB = 1024 * 1024;

    private readonly TusTestServer _server;

    public BotServicesClientTusUploadTests(TusTestServer server)
    {
        _server = server;
        _server.Configure();
    }

    // ── Options ────────────────────────────────────────────────────────────

    [Fact]
    public void TusPartSizeBytes_DefaultsTo50MiB()
    {
        Assert.Equal(50 * MiB, new BotServiceClientOptions().TusPartSizeBytes);
        Assert.Equal(50 * MiB, BotServiceClientOptions.DefaultTusPartSizeBytes);
    }

    [Theory]
    [InlineData(MiB)]
    [InlineData(50 * MiB)]
    [InlineData(90 * MiB)]
    public void TusPartSizeBytes_WithinTheIngressSafeRange_IsAccepted(long value)
    {
        var options = new BotServiceClientOptions { TusPartSizeBytes = value };

        Assert.Equal(value, options.TusPartSizeBytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(MiB - 1)]
    [InlineData(90 * MiB + 1)]
    [InlineData(100 * MiB)]
    [InlineData(3_476_972_984)]
    public void TusPartSizeBytes_OutsideTheIngressSafeRange_IsRejectedWithAClearMessage(long value)
    {
        var options = new BotServiceClientOptions();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.TusPartSizeBytes = value);

        Assert.Contains("100 MiB", exception.Message);
        Assert.Contains("413", exception.Message);
        Assert.Equal(50 * MiB, options.TusPartSizeBytes);
    }

    // ── Parts, offsets, identical file ─────────────────────────────────────

    [Fact]
    public async Task RestoreRepositoryWithTusAsync_UploadsInPartsNeverAboveThePartSize()
    {
        _server.Configure(bodyLimitBytes: MiB);
        using var file = TempFile.WithRandomContent(".gz", 10 * MiB + 12345);
        var client = CreateClient(partSizeBytes: MiB);

        await client.RestoreRepositoryWithTusAsync("acme", "db-1", file.Path,
            cancellationToken: TestContext.Current.CancellationToken);

        var patches = _server.Requests.Where(r => r.Method == "PATCH").ToArray();
        Assert.Equal(11, patches.Length);
        Assert.All(patches, p => Assert.True(p.BodyLength <= MiB));
        AssertContinuousOffsets(patches);
        Assert.Equal(file.Length, _server.Offset);
        Assert.Equal(file.Sha256, _server.ReceivedSha256);
    }

    [Fact]
    public async Task StartImportArchiveDataWithTusAsync_UploadsInPartsNeverAboveThePartSize()
    {
        _server.Configure(bodyLimitBytes: MiB);
        using var file = TempFile.WithRandomContent(".zip", 3 * MiB);
        var client = CreateClient(partSizeBytes: MiB);

        await client.StartImportArchiveDataWithTusAsync("acme", "archive-1", file.Path, ArchiveImportMode.Upsert,
            cancellationToken: TestContext.Current.CancellationToken);

        var patches = _server.Requests.Where(r => r.Method == "PATCH").ToArray();
        Assert.Equal(3, patches.Length);
        Assert.All(patches, p => Assert.True(p.BodyLength <= MiB));
        AssertContinuousOffsets(patches);
        Assert.Equal(file.Sha256, _server.ReceivedSha256);
    }

    /// <summary>
    ///     The scenario of the bug: a backup of a gigabyte or more against a server that refuses any request body above
    ///     100 MiB, with the default settings. The file is 1 GiB with a distinct block every MiB, so a lost, repeated or
    ///     reordered part changes the hash.
    /// </summary>
    [Fact]
    public async Task RestoreRepositoryWithTusAsync_OneGiBWithDefaultSettings_PassesA100MiBBodyLimit()
    {
        _server.Configure(bodyLimitBytes: 100 * MiB);
        using var file = TempFile.WithBlockContent(".octobak.zip", 1024);
        var client = CreateClient();

        await client.RestoreRepositoryWithTusAsync("acme", "db-1", file.Path,
            cancellationToken: TestContext.Current.CancellationToken);

        var patches = _server.Requests.Where(r => r.Method == "PATCH").ToArray();
        Assert.Equal(21, patches.Length);
        Assert.All(patches, p => Assert.True(p.BodyLength <= 50 * MiB));
        AssertContinuousOffsets(patches);
        Assert.Equal(file.Length, _server.Offset);
        Assert.Equal(file.Sha256, _server.ReceivedSha256);
    }

    [Fact]
    public async Task RestoreRepositoryWithTusAsync_ReportsProgressUpToOne()
    {
        using var file = TempFile.WithRandomContent(".gz", 5 * MiB);
        var client = CreateClient(partSizeBytes: MiB);
        var progress = new List<double>();

        await client.RestoreRepositoryWithTusAsync("acme", "db-1", file.Path, progressCallback: progress.Add,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEmpty(progress);
        Assert.Equal(progress.OrderBy(p => p), progress);
        Assert.Equal(1d, progress[^1]);
    }

    [Fact]
    public async Task RestoreRepositoryWithTusAsync_EmptyFile_SendsNoPart()
    {
        using var file = TempFile.WithRandomContent(".gz", 0);
        var client = CreateClient();

        await client.RestoreRepositoryWithTusAsync("acme", "db-1", file.Path,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.DoesNotContain(_server.Requests, r => r.Method == "PATCH");
    }

    // ── A refused part is reported ─────────────────────────────────────────

    /// <summary>
    ///     The original defect: a part above the limit (here a part size set above what the server accepts) answered
    ///     413 and the client said nothing. Now the exception names status, part and offset.
    /// </summary>
    [Fact]
    public async Task RestoreRepositoryWithTusAsync_PartAboveTheServerLimit_ThrowsWith413PartAndOffset()
    {
        _server.Configure(bodyLimitBytes: MiB);
        using var file = TempFile.WithRandomContent(".gz", 5 * MiB);
        var client = CreateClient(partSizeBytes: 4 * MiB);

        var exception = await Assert.ThrowsAsync<TusUploadException>(() =>
            client.RestoreRepositoryWithTusAsync("acme", "db-1", file.Path,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.StatusCode);
        Assert.Equal(1, exception.PartNumber);
        Assert.Equal(2, exception.PartCount);
        Assert.Equal(0, exception.Offset);
        Assert.Equal(4 * MiB, exception.PartSizeBytes);
        Assert.Contains("413", exception.Message);
        Assert.Contains("part 1 of 2", exception.Message);
        Assert.Contains("TusPartSizeBytes", exception.Message);
        // A refusal is not retried: the same body would be refused again.
        Assert.Single(_server.Requests, r => r.Method == "PATCH");
        Assert.Equal(0, _server.Offset);
    }

    [Fact]
    public async Task RestoreRepositoryWithTusAsync_LaterPartRefused_ReportsItsNumberAndOffset()
    {
        _server.Configure(refuseFromPatchNumber: 3);
        using var file = TempFile.WithRandomContent(".gz", 5 * MiB);
        var client = CreateClient(partSizeBytes: MiB);

        var exception = await Assert.ThrowsAsync<TusUploadException>(() =>
            client.RestoreRepositoryWithTusAsync("acme", "db-1", file.Path,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, exception.StatusCode);
        Assert.Equal(3, exception.PartNumber);
        Assert.Equal(5, exception.PartCount);
        Assert.Equal(2 * MiB, exception.Offset);
    }

    [Fact]
    public async Task StartImportArchiveDataWithTusAsync_RefusedPart_ThrowsInsteadOfStartingTheJob()
    {
        _server.Configure(bodyLimitBytes: MiB);
        using var file = TempFile.WithRandomContent(".zip", 3 * MiB);
        var client = CreateClient(partSizeBytes: 2 * MiB);

        await Assert.ThrowsAsync<TusUploadException>(() =>
            client.StartImportArchiveDataWithTusAsync("acme", "archive-1", file.Path, ArchiveImportMode.Upsert,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Single(_server.Requests, r => r.Method == "PATCH");
    }

    [Fact]
    public async Task TheExceptionCarriesNoFileContent()
    {
        _server.Configure(refuseFromPatchNumber: 1);
        using var file = TempFile.WithRandomContent(".gz", 2 * MiB);
        var client = CreateClient(partSizeBytes: MiB);

        var exception = await Assert.ThrowsAsync<TusUploadException>(() =>
            client.RestoreRepositoryWithTusAsync("acme", "db-1", file.Path,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.True(exception.Message.Length < 400);
        Assert.Null(exception.InnerException);
    }

    // ── Resume ─────────────────────────────────────────────────────────────

    /// <summary>
    ///     The connection drops in the middle of a part after the server has applied some of its bytes. The client
    ///     reads the server's offset and continues from there; the file arrives identical, nothing is sent twice.
    /// </summary>
    [Fact]
    public async Task Upload_ConnectionDroppedMidPart_ResumesFromTheServerOffset()
    {
        _server.Configure();
        using var file = TempFile.WithRandomContent(".gz", 3 * MiB + 77);
        using var httpClient = CreateHttpClient(new LoseAnswerAfterPartialBodyHandler(600_000));

        await TusUploader.UploadAsync(httpClient, TusLocation(), file.Path, MiB,
            retryDelay: TimeSpan.FromMilliseconds(10), cancellationToken: TestContext.Current.CancellationToken);

        var requests = _server.Requests;
        var dropped = Array.FindIndex(requests.ToArray(), r => r.Method == "PATCH" && r.Offset == 0);
        Assert.Equal("HEAD", requests[dropped + 1].Method);
        var resumed = requests[dropped + 2];
        Assert.Equal("PATCH", resumed.Method);
        Assert.True(resumed.Offset > 0 && resumed.Offset < MiB, $"resumed at {resumed.Offset}");
        // The resumed request ends at the part boundary, so no body is above the part size.
        Assert.Equal(MiB - resumed.Offset, resumed.BodyLength);
        Assert.Equal(file.Length, _server.Offset);
        Assert.Equal(file.Sha256, _server.ReceivedSha256);
    }

    [Fact]
    public async Task Upload_ServerErrorOnAPart_IsRetriedFromTheServerOffset()
    {
        _server.Configure(serverErrorOnPatchNumber: 2);
        using var file = TempFile.WithRandomContent(".gz", 3 * MiB);

        await TusUploader.UploadAsync(CreateHttpClient(), TusLocation(), file.Path, MiB,
            retryDelay: TimeSpan.FromMilliseconds(10), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(file.Sha256, _server.ReceivedSha256);
        Assert.Equal(4, _server.Requests.Count(r => r.Method == "PATCH"));
    }

    [Fact]
    public async Task Upload_ServerErrorWithoutRetriesLeft_ThrowsWithStatusPartAndOffset()
    {
        _server.Configure(serverErrorOnPatchNumber: 2);
        using var file = TempFile.WithRandomContent(".gz", 3 * MiB);

        var exception = await Assert.ThrowsAsync<TusUploadException>(() =>
            TusUploader.UploadAsync(CreateHttpClient(), TusLocation(), file.Path, MiB, maxRetries: 0,
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal(2, exception.PartNumber);
        Assert.Equal(MiB, exception.Offset);
    }

    /// <summary>
    ///     A PATCH fails and the following offset <c>HEAD</c> is answered 503 (pod restart, ingress hiccup): the part
    ///     is still retried, the remaining attempts are not given up (CodeRabbit finding on PR #79).
    /// </summary>
    [Fact]
    public async Task Upload_ServerErrorOnTheOffsetRead_IsRetriedLikeALostConnection()
    {
        using var file = TempFile.WithRandomContent(".gz", 3 * MiB);
        using var httpClient = CreateHttpClient(new FailPatchThenHeadHandler());

        await TusUploader.UploadAsync(httpClient, TusLocation(), file.Path, MiB,
            retryDelay: TimeSpan.FromMilliseconds(10), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(file.Length, _server.Offset);
        Assert.Equal(file.Sha256, _server.ReceivedSha256);
    }

    /// <summary>
    ///     The bearer token is read again for every request, so a token renewed during a long upload is used from
    ///     then on (CodeRabbit finding on PR #79).
    /// </summary>
    [Fact]
    public async Task Upload_TokenRenewedDuringTheUpload_IsSentOnTheLaterRequests()
    {
        using var file = TempFile.WithRandomContent(".gz", 3 * MiB);
        var recorder = new RecordAuthorizationHandler();
        using var httpClient = CreateHttpClient(recorder);
        var calls = 0;

        await TusUploader.UploadAsync(httpClient, TusLocation(), file.Path, MiB,
            accessTokenProvider: () => $"token-{Interlocked.Increment(ref calls)}",
            cancellationToken: TestContext.Current.CancellationToken);

        var sent = recorder.Authorizations;
        Assert.True(sent.Count >= 4, $"{sent.Count} requests");
        Assert.DoesNotContain("test", sent);
        Assert.Equal(sent.Count, sent.Distinct().Count());
        Assert.Equal(file.Sha256, _server.ReceivedSha256);
    }

    [Fact]
    public async Task Upload_CancelledByTheCaller_Cancels()
    {
        using var file = TempFile.WithRandomContent(".gz", 2 * MiB);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TusUploader.UploadAsync(CreateHttpClient(), TusLocation(), file.Path, MiB, cancellationToken: cts.Token));
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static void AssertContinuousOffsets(IEnumerable<(string Method, long? Offset, long BodyLength)> patches)
    {
        long expected = 0;
        foreach (var patch in patches)
        {
            Assert.Equal(expected, patch.Offset);
            expected += patch.BodyLength;
        }
    }

    private Uri TusLocation()
    {
        return new Uri($"{_server.BaseUrl}acme/v1/tus-upload/{TusTestServer.TusFileId}");
    }

    private static HttpClient CreateHttpClient(DelegatingHandler? handler = null)
    {
        var client = handler is null
            ? new HttpClient { Timeout = Timeout.InfiniteTimeSpan }
            : new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", "test");
        return client;
    }

    private BotServicesClient CreateClient(long? partSizeBytes = null)
    {
        var options = new BotServiceClientOptions { EndpointUri = _server.BaseUrl };
        if (partSizeBytes is { } size)
        {
            options.TusPartSizeBytes = size;
        }

        return new BotServicesClient(options, A.Fake<IBotServiceClientAccessToken>());
    }

    /// <summary>
    ///     Simulates a connection that dies in the middle of the first PATCH: the server receives and applies the first
    ///     <c>bytesToDeliver</c> bytes of the part, the answer never reaches the client.
    /// </summary>
    private sealed class LoseAnswerAfterPartialBodyHandler : DelegatingHandler
    {
        private readonly int _bytesToDeliver;
        private int _dropped;

        public LoseAnswerAfterPartialBodyHandler(int bytesToDeliver) : base(new HttpClientHandler())
        {
            _bytesToDeliver = bytesToDeliver;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Patch || Interlocked.Exchange(ref _dropped, 1) == 1)
            {
                return await base.SendAsync(request, cancellationToken);
            }

            var body = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            var partial = new ByteArrayContent(body, 0, _bytesToDeliver);
            partial.Headers.ContentType = request.Content.Headers.ContentType;
            request.Content = partial;
            request.Headers.ExpectContinue = false;
            using var response = await base.SendAsync(request, cancellationToken);
            throw new HttpRequestException("The connection was reset while the part was being sent.");
        }
    }

    /// <summary>The first PATCH fails with a lost connection and the next HEAD is answered 503, then all passes.</summary>
    private sealed class FailPatchThenHeadHandler : DelegatingHandler
    {
        private int _patchFailed;
        private int _headFailed;

        public FailPatchThenHeadHandler() : base(new HttpClientHandler())
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Patch && Interlocked.Exchange(ref _patchFailed, 1) == 0)
            {
                throw new HttpRequestException("The connection was reset.");
            }

            if (request.Method == HttpMethod.Head && _patchFailed == 1 &&
                Interlocked.Exchange(ref _headFailed, 1) == 0)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>Records the authorization header value of every request.</summary>
    private sealed class RecordAuthorizationHandler : DelegatingHandler
    {
        private readonly List<string?> _authorizations = [];

        public RecordAuthorizationHandler() : base(new HttpClientHandler())
        {
        }

        public IReadOnlyList<string?> Authorizations
        {
            get
            {
                lock (_authorizations)
                {
                    return _authorizations.ToArray();
                }
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            lock (_authorizations)
            {
                _authorizations.Add(request.Headers.Authorization?.Parameter);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    /// <summary>A temporary file with its length and SHA-256, deleted on dispose.</summary>
    private sealed class TempFile : IDisposable
    {
        private TempFile(string path, long length, string sha256)
        {
            Path = path;
            Length = length;
            Sha256 = sha256;
        }

        public string Path { get; }
        public long Length { get; }
        public string Sha256 { get; }

        public static TempFile WithRandomContent(string extension, long length)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                System.IO.Path.GetRandomFileName() + extension);
            var bytes = new byte[length];
            Random.Shared.NextBytes(bytes);
            File.WriteAllBytes(path, bytes);
            return new TempFile(path, length, Convert.ToHexString(SHA256.HashData(bytes)));
        }

        /// <summary>
        ///     Writes <paramref name="mebibytes" /> blocks of 1 MiB; every block starts with its own number, so no two
        ///     blocks are equal and a lost, repeated or reordered part changes the hash.
        /// </summary>
        public static TempFile WithBlockContent(string extension, int mebibytes)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                System.IO.Path.GetRandomFileName() + extension);
            var block = new byte[MiB];
            Random.Shared.NextBytes(block);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                for (var i = 0; i < mebibytes; i++)
                {
                    BitConverter.TryWriteBytes(block, i);
                    stream.Write(block);
                    hash.AppendData(block);
                }
            }

            return new TempFile(path, mebibytes * MiB, Convert.ToHexString(hash.GetHashAndReset()));
        }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
