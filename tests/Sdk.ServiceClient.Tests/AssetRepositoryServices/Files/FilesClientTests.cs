using System.Net;
using System.Text;
using System.Text.Json;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Files;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

namespace Sdk.ServiceClient.Tests.AssetRepositoryServices.Files;

/// <summary>
///     AB#6229: the typed files client — routes, segment encoding, headers, conflict values, zip body,
///     Content-Disposition parsing, problem-details and GraphQL error mapping, GraphQL documents.
/// </summary>
public class FilesClientTests
{
    private const string Endpoint = "https://assets.example.com/";
    private const string Tenant = "meshtest";
    private const string Files = Endpoint + Tenant + "/v1/files/";
    private const string GraphQl = Endpoint + "tenants/" + Tenant + "/GraphQL";
    private const string RootId = "65a000000000000000000001";
    private const string FolderId = "65a000000000000000000002";
    private const string FileId = "65a000000000000000000003";
    private const string OtherFolderId = "65a000000000000000000004";

    private const string FileEntryJson =
        "{\"rtId\":\"" + FileId + "\",\"ckTypeId\":\"System.Files/FileSystemItem\",\"kind\":\"file\",\"name\":\"ä b.pdf\"," +
        "\"root\":\"Files\",\"path\":\"Docs/ä b.pdf\",\"parentRtId\":\"" + FolderId + "\",\"size\":3," +
        "\"contentType\":\"application/pdf\",\"binaryId\":\"65a0000000000000000000ff\",\"replaced\":false}";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private readonly RecordingHandler _handler = new();
    private readonly ServiceClientAccessToken _token = new() { AccessToken = "tok-1" };

    private FilesClient CreateClient() =>
        new(new AssetServiceClientOptions { EndpointUri = Endpoint, TenantId = Tenant }, _token, _handler);

    // ---- REST routes + encoding -----------------------------------------------------------------

    [Fact]
    public async Task GetCapabilities_HitsTheTenantRoute_WithBearer()
    {
        _handler.Respond(HttpStatusCode.OK,
            "{\"apiVersion\":1,\"maxUploadBytes\":104857600,\"zipDownload\":true,\"zipMaxFiles\":1000," +
            "\"zipMaxBytes\":524288000,\"previewMaxBytes\":20971520,\"recursiveDelete\":true,\"folderStats\":true," +
            "\"reservedRootNames\":[\"capabilities\",\"items\",\"stats\",\"zip\"]}");

        var caps = await CreateClient().GetCapabilitiesAsync(cancellationToken: Ct);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(Files + "capabilities", request.Wire);
        Assert.Equal("Bearer tok-1", request.Authorization);
        Assert.Equal(104857600, caps.MaxUploadBytes);
        Assert.True(caps.ZipDownload);
        Assert.Equal(1000, caps.ZipMaxFiles);
        Assert.Equal(4, caps.ReservedRootNames.Count);
    }

    [Fact]
    public async Task Requests_ReadTheTokenPerCall()
    {
        _handler.Respond(HttpStatusCode.OK, "{}").Respond(HttpStatusCode.OK, "{}");
        var client = CreateClient();

        await client.GetCapabilitiesAsync(cancellationToken: Ct);
        _token.AccessToken = "tok-2";
        await client.GetCapabilitiesAsync(cancellationToken: Ct);

        Assert.Equal("Bearer tok-2", _handler.Requests[1].Authorization);
    }

    [Fact]
    public async Task Upload_EncodesEverySegment_SendsRawBodyAndContentType()
    {
        _handler.Respond(HttpStatusCode.Created, FileEntryJson);
        var bytes = "abc"u8.ToArray();

        var entry = await CreateClient().UploadAsync("Files", "/Docs//Q1 2026/ä b#?.pdf", new MemoryStream(bytes),
            "application/pdf", cancellationToken: Ct);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal(Files + "Files/Docs/Q1%202026/%C3%A4%20b%23%3F.pdf?conflict=fail", request.Wire);
        Assert.Equal("application/pdf", request.ContentType);
        Assert.Equal(3, request.ContentLength);
        Assert.Equal(bytes, request.Body);
        Assert.Equal(FileId, entry.RtId);
        Assert.Equal("ä b.pdf", entry.Name);
        Assert.False(entry.Replaced);
    }

    [Theory]
    [InlineData(FileConflictMode.Fail, false, "?conflict=fail")]
    [InlineData(FileConflictMode.Replace, false, "?conflict=replace")]
    [InlineData(FileConflictMode.KeepBoth, true, "?conflict=keepBoth&createFolders=true")]
    public async Task Upload_SendsConflictAndCreateFolders(FileConflictMode mode, bool createFolders, string query)
    {
        _handler.Respond(HttpStatusCode.OK, FileEntryJson);

        await CreateClient().UploadAsync("Files", "a.txt", new MemoryStream([1]), null, mode, createFolders, cancellationToken: Ct);

        Assert.Equal(Files + "Files/a.txt" + query, _handler.Requests[0].Wire);
    }

    [Fact]
    public async Task Upload_WithoutContentType_SendsOctetStream_AndLeavesTheCallerStreamOpen()
    {
        _handler.Respond(HttpStatusCode.Created, FileEntryJson);
        var stream = new MemoryStream([1, 2]);

        await CreateClient().UploadAsync("Files", "a.bin", stream, cancellationToken: Ct);

        Assert.Equal("application/octet-stream", _handler.Requests[0].ContentType);
        Assert.True(stream.CanRead); // not disposed with the request
    }

    [Fact]
    public async Task Upload_NonSeekableStream_IsSentWithoutLength()
    {
        _handler.Respond(HttpStatusCode.Created, FileEntryJson);

        await CreateClient().UploadAsync("Files", "a.bin", new NonSeekableStream([1, 2, 3]), cancellationToken: Ct);

        Assert.Null(_handler.Requests[0].ContentLength);
        Assert.Equal(new byte[] { 1, 2, 3 }, _handler.Requests[0].Body);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("Docs/../secret.txt")]
    [InlineData("./a.txt")]
    [InlineData("")]
    [InlineData("/")]
    public async Task Upload_RefusesDotSegmentsAndEmptyPaths_WithoutSending(string path)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateClient().UploadAsync("Files", path, new MemoryStream(), cancellationToken: Ct));
        Assert.Empty(_handler.Requests);
    }

    [Theory]
    [InlineData("items")]
    [InlineData("ZIP")]
    [InlineData("a/b")]
    [InlineData("..")]
    [InlineData(" ")]
    public async Task PathRoutes_RefuseReservedOrInvalidRoots(string root)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateClient().DownloadAsync(root, "a.txt", cancellationToken: Ct));
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task UploadToFolder_PostsToItemsContent_WithEncodedName()
    {
        _handler.Respond(HttpStatusCode.Created, FileEntryJson);

        await CreateClient().UploadToFolderAsync(FolderId, "a&b ä.txt", new MemoryStream([1]), "text/plain",
            FileConflictMode.KeepBoth, cancellationToken: Ct);

        var request = _handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(Files + $"items/{FolderId}/content?name=a%26b%20%C3%A4.txt&conflict=keepBoth", request.Wire);
        Assert.Equal("text/plain", request.ContentType);
    }

    [Fact]
    public async Task ReplaceContent_PutsToItemsContent()
    {
        _handler.Respond(HttpStatusCode.OK, FileEntryJson.Replace("\"replaced\":false", "\"replaced\":true"));

        var entry = await CreateClient().ReplaceContentAsync(FileId, new MemoryStream([9]), "application/pdf", cancellationToken: Ct);

        Assert.Equal(HttpMethod.Put, _handler.Requests[0].Method);
        Assert.Equal(Files + $"items/{FileId}/content", _handler.Requests[0].Wire);
        Assert.True(entry.Replaced);
    }

    [Theory]
    [InlineData("not-an-id")]
    [InlineData("65a00000000000000000000")]
    [InlineData("")]
    public async Task RtIdRoutes_RefuseInvalidIds(string rtId)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateClient().DownloadByIdAsync(rtId, cancellationToken: Ct));
        Assert.Empty(_handler.Requests);
    }

    // ---- downloads ------------------------------------------------------------------------------

    [Fact]
    public async Task Download_ByPath_ReturnsStreamNameTypeLengthAndETag()
    {
        _handler.RespondBytes("%PDF"u8.ToArray(), r =>
        {
            r.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            r.Content.Headers.TryAddWithoutValidation("Content-Disposition",
                "attachment; filename=\"a b.pdf\"; filename*=UTF-8''%C3%A4%20b.pdf");
            r.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"65a0000000000000000000ff\"");
        });

        await using var download = await CreateClient().DownloadAsync("Files", "Docs/ä b.pdf", inline: true, cancellationToken: Ct);

        Assert.Equal(Files + "Files/Docs/%C3%A4%20b.pdf?inline=true", _handler.Requests[0].Wire);
        Assert.Equal("ä b.pdf", download.FileName);
        Assert.Equal("application/pdf", download.ContentType);
        Assert.Equal(4, download.ContentLength);
        Assert.Equal("\"65a0000000000000000000ff\"", download.ETag);
        using var reader = new StreamReader(download.Content);
        Assert.Equal("%PDF", await reader.ReadToEndAsync(Ct));
    }

    [Fact]
    public async Task Download_ById_FallsBackToThePlainFileName()
    {
        _handler.RespondBytes([1], r =>
            r.Content.Headers.TryAddWithoutValidation("Content-Disposition", "inline; filename=\"report.csv\""));

        await using var download = await CreateClient().DownloadByIdAsync(FileId, cancellationToken: Ct);

        Assert.Equal(Files + $"items/{FileId}/content", _handler.Requests[0].Wire);
        Assert.Equal("report.csv", download.FileName);
    }

    [Fact]
    public async Task DownloadZip_PostsTheSelection_AndReadsTheZipName()
    {
        _handler.RespondBytes([0x50, 0x4b], r =>
        {
            r.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
            r.Content.Headers.TryAddWithoutValidation("Content-Disposition",
                "attachment; filename=\"export.zip\"; filename*=UTF-8''export.zip");
        });

        await using var zip = await CreateClient().DownloadZipAsync(new ZipRequestDto
        {
            Items = [new FileRefDto { Root = "Files", Path = "Docs/Q1" }],
            RtIds = [FileId],
            FileName = "export"
        }, cancellationToken: Ct);

        var request = _handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(Files + "zip", request.Wire);
        Assert.StartsWith("application/json", request.ContentType);
        using var body = JsonDocument.Parse(request.BodyText);
        Assert.Equal("Files", body.RootElement.GetProperty("items")[0].GetProperty("root").GetString());
        Assert.Equal("Docs/Q1", body.RootElement.GetProperty("items")[0].GetProperty("path").GetString());
        Assert.Equal(FileId, body.RootElement.GetProperty("rtIds")[0].GetString());
        Assert.Equal("export", body.RootElement.GetProperty("fileName").GetString());
        Assert.Equal("export.zip", zip.FileName);
        Assert.Equal("application/zip", zip.ContentType);
    }

    [Fact]
    public async Task DownloadZip_ByRtIds_SendsOnlyRtIds()
    {
        _handler.RespondBytes([0x50, 0x4b], _ => { });

        await using var _ = await CreateClient().DownloadZipAsync([FileId, FolderId], cancellationToken: Ct);

        using var body = JsonDocument.Parse(_handler.Requests[0].BodyText);
        Assert.Equal(2, body.RootElement.GetProperty("rtIds").GetArrayLength());
        Assert.True(!body.RootElement.TryGetProperty("items", out var items) || items.ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task DownloadZip_EmptySelection_IsRefusedClientSide()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateClient().DownloadZipAsync(new ZipRequestDto(), cancellationToken: Ct));
        Assert.Empty(_handler.Requests);
    }

    // ---- stats ----------------------------------------------------------------------------------

    [Fact]
    public async Task Stats_ByPath_SendsRootAndPathAsQuery()
    {
        _handler.Respond(HttpStatusCode.OK,
            "{\"folders\":2,\"files\":5,\"bytes\":1024,\"linkedFiles\":1,\"hiddenEntries\":3,\"complete\":true," +
            "\"linkedSamples\":[{\"rtId\":\"" + FileId + "\",\"name\":\"a.pdf\",\"path\":\"Q1/a.pdf\"," +
            "\"entities\":[{\"rtId\":\"" + OtherFolderId + "\",\"ckTypeId\":\"Basic/Invoice\",\"displayName\":\"INV-1\"}]}]}");

        var stats = await CreateClient().GetFolderStatsAsync("Files", "Docs/Q 1", cancellationToken: Ct);

        Assert.Equal(Files + "stats?root=Files&path=Docs%2FQ%201", _handler.Requests[0].Wire);
        Assert.Equal(5, stats.Files);
        Assert.Equal(3, stats.HiddenEntries);
        Assert.Equal("INV-1", stats.LinkedSamples[0].Entities[0].DisplayName);
    }

    [Fact]
    public async Task Stats_OfARoot_OmitsThePath_AndById_UsesItemsRoute()
    {
        _handler.Respond(HttpStatusCode.OK, "{}").Respond(HttpStatusCode.OK, "{}");
        var client = CreateClient();

        await client.GetFolderStatsAsync("Files", cancellationToken: Ct);
        await client.GetFolderStatsByIdAsync(FolderId, cancellationToken: Ct);

        Assert.Equal(Files + "stats?root=Files", _handler.Requests[0].Wire);
        Assert.Equal(Files + $"items/{FolderId}/stats", _handler.Requests[1].Wire);
    }

    // ---- REST error mapping ---------------------------------------------------------------------

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "ROOT_NOT_FOUND")]
    [InlineData(HttpStatusCode.NotFound, "PATH_NOT_FOUND")]
    [InlineData(HttpStatusCode.Conflict, "AMBIGUOUS_PATH")]
    [InlineData(HttpStatusCode.Conflict, "NAME_CONFLICT")]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, "LIMIT_EXCEEDED")]
    [InlineData(HttpStatusCode.Forbidden, "FORBIDDEN")]
    [InlineData(HttpStatusCode.BadRequest, "INVALID_NAME")]
    public async Task ProblemDetails_MapToFilesApiException_WithCodeAndStatus(HttpStatusCode status, string code)
    {
        _handler.Respond(status,
            $"{{\"status\":{(int)status},\"title\":\"{code}\",\"detail\":\"Something about it.\",\"code\":\"{code}\"}}",
            "application/problem+json");

        var e = await Assert.ThrowsAsync<FilesApiException>(() =>
            CreateClient().UploadAsync("Files", "a.txt", new MemoryStream([1]), cancellationToken: Ct));

        Assert.Equal(code, e.Code);
        Assert.Equal(status, e.HttpStatusCode);
        Assert.Equal("Something about it.", e.Detail);
        Assert.False(e.IsGraphQlError);
        Assert.IsAssignableFrom<ServiceClientResultException>(e);
    }

    [Fact]
    public async Task ProblemDetails_OnDownload_AreMappedBeforeAnyStreamIsReturned()
    {
        _handler.Respond(HttpStatusCode.NotFound,
            "{\"status\":404,\"title\":\"PATH_NOT_FOUND\",\"detail\":\"'Files/x' not found.\",\"code\":\"PATH_NOT_FOUND\"}",
            "application/problem+json");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().DownloadAsync("Files", "x", cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.PathNotFound, e.Code);
    }

    [Fact]
    public async Task ProblemDetails_WithoutCodeExtension_UseACodeShapedTitle()
    {
        _handler.Respond(HttpStatusCode.Conflict, "{\"status\":409,\"title\":\"NOT_A_FILE\",\"detail\":\"d\"}",
            "application/problem+json");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().DownloadByIdAsync(FolderId, cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.NotAFile, e.Code);
    }

    [Fact]
    public async Task IngressHtml413_MapsToLimitExceeded()
    {
        _handler.Respond(HttpStatusCode.RequestEntityTooLarge,
            "<html><body>413 Request Entity Too Large</body></html>", "text/html");

        var e = await Assert.ThrowsAsync<FilesApiException>(() =>
            CreateClient().UploadAsync("Files", "big.bin", new MemoryStream([1]), cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.LimitExceeded, e.Code);
        Assert.Contains("413", e.Detail);
    }

    [Fact]
    public async Task ServerError_WithoutProblemDetails_IsUnknown()
    {
        _handler.Respond(HttpStatusCode.InternalServerError, "boom", "text/plain");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().GetCapabilitiesAsync(cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.Unknown, e.Code);
        Assert.Equal(HttpStatusCode.InternalServerError, e.HttpStatusCode);
    }

    [Fact]
    public async Task Unauthorized_ThrowsUnauthorizedServiceAccessException()
    {
        _handler.Respond(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<UnauthorizedServiceAccessException>(() => CreateClient().GetCapabilitiesAsync(cancellationToken: Ct));
    }

    [Fact]
    public async Task MissingConfiguration_Throws()
    {
        var client = new FilesClient(new AssetServiceClientOptions { EndpointUri = Endpoint }, _token, _handler);

        await Assert.ThrowsAsync<ServiceConfigurationMissingException>(() => client.GetCapabilitiesAsync(cancellationToken: Ct));
    }

    // ---- GraphQL metadata -----------------------------------------------------------------------

    [Fact]
    public async Task ListRoots_PostsToTheTenantGraphQlEndpoint_AndMapsThePage()
    {
        _handler.RespondData(
            "{\"runtime\":{\"systemFilesFolderRoot\":{\"totalCount\":2,\"pageInfo\":{\"endCursor\":\"c2\",\"hasNextPage\":true}," +
            "\"items\":[{\"__typename\":\"SystemFilesFolderRoot\",\"rtId\":\"" + RootId + "\",\"name\":\"Files\"," +
            "\"rtWellKnownName\":\"Files\",\"rtCreationDateTime\":\"2026-10-09T08:00:00Z\"}]}}}");

        var page = await CreateClient().ListRootsAsync(1, cancellationToken: Ct);

        var request = _handler.Requests[0];
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(GraphQl, request.Wire);
        Assert.Equal("Bearer tok-1", request.Authorization);
        using var body = JsonDocument.Parse(request.BodyText);
        Assert.Contains("systemFilesFolderRoot(", body.RootElement.GetProperty("query").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("variables").GetProperty("first").GetInt32());

        Assert.Equal(2, page.TotalCount);
        Assert.True(page.HasNextPage);
        Assert.Equal("c2", page.EndCursor);
        var root = Assert.Single(page.Items);
        Assert.Equal(FileEntryKinds.Root, root.Kind);
        Assert.Equal(FileSystemCkTypeIds.FolderRoot, root.CkTypeId);
        Assert.Equal("Files", root.Root);
        Assert.Equal(new DateTime(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc), root.CreatedAt);
    }

    [Fact]
    public async Task GetEntry_ResolvesSegmentByCaseInsensitiveRegex_PrefersTheExactName()
    {
        _handler
            .RespondData(RootsData(("Files", RootId)))
            .RespondData(ChildrenData("roots", Folder(FolderId, "docs"), Folder(OtherFolderId, "Docs")))
            .RespondData(ChildrenData("folders", File(FileId, "a+b.pdf", 7)));

        var entry = await CreateClient().GetEntryAsync("Files", "Docs/a+b.pdf", cancellationToken: Ct);

        Assert.Equal(FileId, entry.RtId);
        Assert.Equal(FileEntryKinds.File, entry.Kind);
        Assert.Equal("Files", entry.Root);
        Assert.Equal("Docs/a+b.pdf", entry.Path);
        Assert.Equal(OtherFolderId, entry.ParentRtId);
        Assert.Equal(7, entry.Size);

        using var rootQuery = JsonDocument.Parse(_handler.Requests[0].BodyText);
        var rootFilter = rootQuery.RootElement.GetProperty("variables").GetProperty("filter")[0];
        Assert.Equal("rtWellKnownName", rootFilter.GetProperty("attributePath").GetString());
        Assert.Equal("MATCH_REG_EX", rootFilter.GetProperty("operator").GetString());
        Assert.Equal("(?i)^Files$", rootFilter.GetProperty("comparisonValue").GetString());

        using var second = JsonDocument.Parse(_handler.Requests[1].BodyText);
        Assert.Equal(RootId, second.RootElement.GetProperty("variables").GetProperty("rtId").GetString());
        // Engine bug: fieldFilter + sortOrder + first on an association answers INCOMPLETE_SLICE — lookups send no sort.
        var lookupQuery = second.RootElement.GetProperty("query").GetString()!;
        Assert.Contains("fieldFilter: $filter", lookupQuery);
        Assert.DoesNotContain("sortOrder", lookupQuery);
        using var third = JsonDocument.Parse(_handler.Requests[2].BodyText);
        Assert.Equal(OtherFolderId, third.RootElement.GetProperty("variables").GetProperty("rtId").GetString());
        Assert.Equal(@"(?i)^a\+b\.pdf$",
            third.RootElement.GetProperty("variables").GetProperty("filter")[0].GetProperty("comparisonValue").GetString());
    }

    [Fact]
    public void ChildLookupQuery_HasNoSortOrder_PlainListingIsSorted()
    {
        // Engine bug (smoke test 8cdd04c): fieldFilter + sortOrder + first on an association connection answers
        // INCOMPLETE_SLICE when the folder has more than one child.
        Assert.DoesNotContain("sortOrder", FilesClient.ChildLookupQuery);
        Assert.Contains("fieldFilter: $filter", FilesClient.ChildLookupQuery);
        Assert.Contains("sortOrder", FilesClient.ChildrenQuery);
    }

    [Fact]
    public async Task GetEntry_UnknownRoot_IsRootNotFound()
    {
        _handler.RespondData(RootsData());

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().GetEntryAsync("Nope", "a", cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.RootNotFound, e.Code);
        Assert.Equal(HttpStatusCode.NotFound, e.HttpStatusCode);
    }

    [Fact]
    public async Task GetEntry_MissingSegment_IsPathNotFound_AndDuplicates_AreAmbiguous()
    {
        _handler.RespondData(RootsData(("Files", RootId))).RespondData(ChildrenData("roots"));
        var missing = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().GetEntryAsync("Files", "x", cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.PathNotFound, missing.Code);

        _handler.RespondData(RootsData(("Files", RootId)))
            .RespondData(ChildrenData("roots", Folder(FolderId, "dup"), Folder(OtherFolderId, "DUP")));
        var ambiguous = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().GetEntryAsync("Files", "Dup", cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.AmbiguousPath, ambiguous.Code);
        Assert.Equal(HttpStatusCode.Conflict, ambiguous.HttpStatusCode);
    }

    [Fact]
    public async Task ListChildren_ByPath_SetsRootAndPathOnTheItems()
    {
        _handler
            .RespondData(RootsData(("Files", RootId)))
            .RespondData(ChildrenData("roots", Folder(FolderId, "Docs")))
            .RespondData(ChildrenData("folders", Folder(OtherFolderId, "Sub"), File(FileId, "a.txt", 1)));

        var page = await CreateClient().ListChildrenAsync("Files", "Docs", 50, cancellationToken: Ct);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal("Docs/Sub", page.Items[0].Path);
        Assert.Equal(FileEntryKinds.Folder, page.Items[0].Kind);
        Assert.Equal("Docs/a.txt", page.Items[1].Path);
        Assert.Equal(FolderId, page.Items[1].ParentRtId);
        using var last = JsonDocument.Parse(_handler.Requests[2].BodyText);
        Assert.Equal(50, last.RootElement.GetProperty("variables").GetProperty("first").GetInt32());
        Assert.Equal(JsonValueKind.Null, last.RootElement.GetProperty("variables").GetProperty("filter").ValueKind);
        Assert.Contains("sortOrder: [{attributePath: \"name\", sortOrder: ASCENDING}]",
            last.RootElement.GetProperty("query").GetString()); // plain listings stay sorted by name
    }

    [Fact]
    public async Task ListChildrenById_OfAFile_IsNotAFolder()
    {
        _handler.RespondData(
            "{\"runtime\":{\"roots\":{\"items\":[]},\"folders\":{\"items\":[]},\"files\":{\"items\":[{\"rtId\":\"" +
            FileId + "\"}]}}}");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().ListChildrenByIdAsync(FileId, cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.NotAFolder, e.Code);
    }

    [Fact]
    public async Task CreateRoot_SendsNameAndWellKnownName()
    {
        _handler.RespondData(
            "{\"runtime\":{\"systemFilesFolderRoots\":{\"create\":[{\"__typename\":\"SystemFilesFolderRoot\",\"rtId\":\"" +
            RootId + "\",\"name\":\"Docs\",\"rtWellKnownName\":\"Docs\"}]}}}");

        var root = await CreateClient().CreateRootAsync("Docs", "Docs", cancellationToken: Ct);

        using var body = JsonDocument.Parse(_handler.Requests[0].BodyText);
        Assert.Contains("systemFilesFolderRoots { create(", body.RootElement.GetProperty("query").GetString());
        Assert.Equal("Docs", body.RootElement.GetProperty("variables").GetProperty("wellKnownName").GetString());
        Assert.Equal(RootId, root.RtId);
        Assert.Equal("Docs", root.Root);
    }

    [Fact]
    public async Task GraphQlErrors_MapToFilesApiException_WithExtensionsCode()
    {
        _handler.Respond(HttpStatusCode.OK,
            "{\"errors\":[{\"message\":\"Missing role FileManagement.\",\"extensions\":{\"code\":\"FORBIDDEN\"}}],\"data\":null}");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().CreateRootAsync("Docs", "Docs", cancellationToken: Ct));

        Assert.Equal(FileErrorCodes.Forbidden, e.Code);
        Assert.Equal(HttpStatusCode.Forbidden, e.HttpStatusCode);
        Assert.True(e.IsGraphQlError);
        Assert.Contains("Missing role", e.Detail);
    }

    [Fact]
    public async Task GraphQlErrors_On400_AreStillMappedByCode()
    {
        _handler.Respond(HttpStatusCode.BadRequest,
            "{\"errors\":[{\"message\":\"Variable invalid\",\"extensions\":{\"code\":\"INVALID_VALUE\"}}]}");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().ListRootsAsync(cancellationToken: Ct));
        Assert.Equal("INVALID_VALUE", e.Code);
    }

    [Fact]
    public async Task CreateFolderIn_LooksUpTheParentKind_AndCreatesWithParentAssociation()
    {
        _handler
            .RespondData(EntryData("roots", "{\"__typename\":\"SystemFilesFolderRoot\",\"rtId\":\"" + RootId +
                                            "\",\"name\":\"Files\",\"rtWellKnownName\":\"Files\"}"))
            .RespondData("{\"runtime\":{\"systemFilesFolders\":{\"create\":[{\"__typename\":\"SystemFilesFolder\",\"rtId\":\"" +
                         FolderId + "\",\"name\":\"New\"}]}}}");

        var folder = await CreateClient().CreateFolderInAsync(RootId, "New", cancellationToken: Ct);

        using var body = JsonDocument.Parse(_handler.Requests[1].BodyText);
        var variables = body.RootElement.GetProperty("variables");
        Assert.Equal("System.Files/FolderRoot", variables.GetProperty("parentCkTypeId").GetString());
        Assert.Equal(RootId, variables.GetProperty("parentRtId").GetString());
        Assert.Equal("New", variables.GetProperty("name").GetString());
        Assert.Contains("modOption: CREATE", body.RootElement.GetProperty("query").GetString());
        Assert.Equal(FolderId, folder.RtId);
        Assert.Equal(RootId, folder.ParentRtId);
    }

    [Theory]
    [InlineData("roots", "SystemFilesFolderRoot", "systemFilesFolderRoots")]
    [InlineData("folders", "SystemFilesFolder", "systemFilesFolders")]
    [InlineData("files", "SystemFilesFileSystemItem", "systemFilesFileSystemItems")]
    public async Task Rename_UsesTheMutationOfTheEntryKind(string alias, string typeName, string mutationField)
    {
        _handler
            .RespondData(EntryData(alias, $"{{\"__typename\":\"{typeName}\",\"rtId\":\"{FolderId}\",\"name\":\"Old\"}}"))
            .RespondData($"{{\"runtime\":{{\"{mutationField}\":{{\"update\":[{{\"rtId\":\"{FolderId}\",\"name\":\"New\"}}]}}}}}}");

        await CreateClient().RenameAsync(FolderId, "New", cancellationToken: Ct);

        using var body = JsonDocument.Parse(_handler.Requests[1].BodyText);
        Assert.Contains($"{mutationField} {{ update(", body.RootElement.GetProperty("query").GetString());
        Assert.Equal("New", body.RootElement.GetProperty("variables").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Move_DeletesTheOldParentLink_AndCreatesTheNewOne()
    {
        _handler
            .RespondData(EntryData("files",
                "{\"__typename\":\"SystemFilesFileSystemItem\",\"rtId\":\"" + FileId + "\",\"name\":\"a.txt\"," +
                "\"parent\":{\"items\":[{\"__typename\":\"SystemFilesFolderRoot\",\"rtId\":\"" + RootId + "\"}]}}"))
            .RespondData(EntryData("folders",
                "{\"__typename\":\"SystemFilesFolder\",\"rtId\":\"" + FolderId + "\",\"name\":\"Docs\"," +
                "\"parent\":{\"items\":[{\"__typename\":\"SystemFilesFolderRoot\",\"rtId\":\"" + RootId + "\"}]}}"))
            .RespondData("{\"runtime\":{\"systemFilesFileSystemItems\":{\"update\":[{\"rtId\":\"" + FileId + "\"}]}}}");

        await CreateClient().MoveAsync(FileId, FolderId, cancellationToken: Ct);

        using var body = JsonDocument.Parse(_handler.Requests[2].BodyText);
        var query = body.RootElement.GetProperty("query").GetString()!;
        Assert.Contains("systemFilesFileSystemItems { update(", query);
        Assert.Contains("modOption: DELETE", query);
        Assert.Contains("modOption: CREATE", query);
        var variables = body.RootElement.GetProperty("variables");
        Assert.Equal("System.Files/FolderRoot", variables.GetProperty("fromCkTypeId").GetString());
        Assert.Equal(RootId, variables.GetProperty("fromRtId").GetString());
        Assert.Equal("System.Files/Folder", variables.GetProperty("toCkTypeId").GetString());
        Assert.Equal(FolderId, variables.GetProperty("toRtId").GetString());
    }

    [Fact]
    public async Task Move_OfARoot_IsRefused()
    {
        _handler.RespondData(EntryData("roots",
            "{\"__typename\":\"SystemFilesFolderRoot\",\"rtId\":\"" + RootId + "\",\"name\":\"Docs\"}"));

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().MoveAsync(RootId, FolderId, cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.InvalidRequest, e.Code);
        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task Delete_SendsRuntimeEntitiesDelete_WithTheKindsTypeId()
    {
        _handler
            .RespondData(EntryData("folders",
                "{\"__typename\":\"SystemFilesFolder\",\"rtId\":\"" + FolderId + "\",\"name\":\"Docs\"}"))
            .RespondData("{\"runtime\":{\"runtimeEntities\":{\"delete\":true}}}");

        await CreateClient().DeleteAsync(FolderId, cancellationToken: Ct);

        using var body = JsonDocument.Parse(_handler.Requests[1].BodyText);
        Assert.Contains("runtimeEntities { delete(entities: $rtEntityIds) }",
            body.RootElement.GetProperty("query").GetString());
        var entity = body.RootElement.GetProperty("variables").GetProperty("rtEntityIds")[0];
        Assert.Equal("System.Files/Folder", entity.GetProperty("ckTypeId").GetString());
        Assert.Equal(FolderId, entity.GetProperty("rtId").GetString());
    }

    [Fact]
    public async Task Delete_ProtectedRoot_SurfacesTheGraphQlCode()
    {
        _handler
            .RespondData(EntryData("roots",
                "{\"__typename\":\"SystemFilesFolderRoot\",\"rtId\":\"" + RootId + "\",\"name\":\"Files\"}"))
            .Respond(HttpStatusCode.OK,
                "{\"errors\":[{\"message\":\"The root 'Files' is protected.\",\"extensions\":{\"code\":\"PROTECTED_ROOT\"}}]}");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().DeleteAsync(RootId, cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.ProtectedRoot, e.Code);
        Assert.Equal(HttpStatusCode.Conflict, e.HttpStatusCode);
    }

    [Fact]
    public async Task Delete_UnknownRtId_IsPathNotFound_WithoutMutation()
    {
        _handler.RespondData(EntryData("roots", null));

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().DeleteAsync(FolderId, cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.PathNotFound, e.Code);
        Assert.Single(_handler.Requests);
    }

    [Fact]
    public async Task DeleteImpact_OfAFolder_UsesTheStatsEndpoint_OfAFile_HasNoStats()
    {
        _handler
            .RespondData(EntryData("folders",
                "{\"__typename\":\"SystemFilesFolder\",\"rtId\":\"" + FolderId + "\",\"name\":\"Docs\"}"))
            .Respond(HttpStatusCode.OK, "{\"folders\":1,\"files\":4,\"bytes\":10,\"hiddenEntries\":2,\"complete\":true}")
            .RespondData(EntryData("files",
                "{\"__typename\":\"SystemFilesFileSystemItem\",\"rtId\":\"" + FileId + "\",\"name\":\"a.txt\"," +
                "\"content\":{\"binaryId\":\"65a0000000000000000000ff\",\"contentType\":\"text/plain\",\"size\":\"12\"}}"))
            .Respond(HttpStatusCode.OK, "{\"counts\":{\"" + FileId + "\":2}}");
        var client = CreateClient();

        var folderImpact = await client.GetDeleteImpactAsync(FolderId, cancellationToken: Ct);
        var fileImpact = await client.GetDeleteImpactAsync(FileId, cancellationToken: Ct);

        Assert.Equal(Files + $"items/{FolderId}/stats", _handler.Requests[1].Wire);
        Assert.Equal(4, folderImpact.Stats!.Files);
        Assert.Equal(2, folderImpact.Stats.HiddenEntries);
        Assert.Null(fileImpact.Stats);
        Assert.Null(folderImpact.LinkedCount);
        Assert.Equal(12, fileImpact.Entry.Size);
        Assert.Equal(2, fileImpact.LinkedCount);
        Assert.Equal(Files + "linked-counts", _handler.Requests[3].Wire);
        Assert.Equal(4, _handler.Requests.Count);
    }

    // ---- review fixes: timeouts, status mapping ---------------------------------------------------

    [Fact]
    public async Task Upload_IsNotCutOffByMaxTimeout_ButByTheTransferTimeout()
    {
        var options = new AssetServiceClientOptions
        {
            EndpointUri = Endpoint, TenantId = Tenant, MaxTimeout = 50, FileTransferTimeout = TimeSpan.FromSeconds(30)
        };
        _handler.RespondAfter(TimeSpan.FromMilliseconds(300), FileEntryJson);
        using var client = new FilesClient(options, _token, _handler, false);

        var entry = await client.UploadAsync("Files", "a.txt", new MemoryStream([1]), cancellationToken: Ct);

        Assert.Equal(FileId, entry.RtId);
    }

    [Fact]
    public async Task Upload_AboveTheTransferTimeout_ThrowsTimeoutException()
    {
        var options = new AssetServiceClientOptions
        {
            EndpointUri = Endpoint, TenantId = Tenant, FileTransferTimeout = TimeSpan.FromMilliseconds(50)
        };
        _handler.RespondAfter(TimeSpan.FromSeconds(10), FileEntryJson);
        using var client = new FilesClient(options, _token, _handler, false);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            client.UploadAsync("Files", "a.txt", new MemoryStream([1]), cancellationToken: Ct));
    }

    [Fact]
    public async Task Metadata_AboveMaxTimeout_ThrowsTimeoutException_CallerCancel_StaysCancellation()
    {
        var options = new AssetServiceClientOptions { EndpointUri = Endpoint, TenantId = Tenant, MaxTimeout = 50 };
        _handler.RespondAfter(TimeSpan.FromSeconds(10), "{}").RespondAfter(TimeSpan.FromSeconds(10), "{}");
        using var client = new FilesClient(options, _token, _handler, false);

        await Assert.ThrowsAsync<TimeoutException>(() => client.GetCapabilitiesAsync(Ct));

        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cancel.CancelAfter(20);
        var slowOptions = new AssetServiceClientOptions { EndpointUri = Endpoint, TenantId = Tenant, MaxTimeout = 60_000 };
        using var slowClient = new FilesClient(slowOptions, _token, _handler, false);
        var e = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => slowClient.GetCapabilitiesAsync(cancel.Token));
        Assert.IsNotType<TimeoutException>(e);
    }

    [Fact]
    public async Task GraphQlLimitExceeded_FromTheDeleteCap_Is409_Not413()
    {
        _handler
            .RespondData(EntryData("folders",
                "{\"__typename\":\"SystemFilesFolder\",\"rtId\":\"" + FolderId + "\",\"name\":\"Big\"}"))
            .Respond(HttpStatusCode.OK,
                "{\"errors\":[{\"message\":\"More than 2000 entries.\",\"extensions\":{\"code\":\"LIMIT_EXCEEDED\"}}]}");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().DeleteAsync(FolderId, Ct));

        Assert.Equal(FileErrorCodes.LimitExceeded, e.Code);
        Assert.Equal(HttpStatusCode.Conflict, e.HttpStatusCode);
        Assert.True(e.IsGraphQlError);
    }

    [Fact]
    public async Task GraphQlInvalidRequest_FromTheSingleParentRule_Is400()
    {
        _handler.Respond(HttpStatusCode.OK,
            "{\"errors\":[{\"message\":\"An entry has exactly one parent.\",\"extensions\":{\"code\":\"INVALID_REQUEST\"}}]}");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().ListRootsAsync(cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.InvalidRequest, e.Code);
        Assert.Equal(HttpStatusCode.BadRequest, e.HttpStatusCode);
    }

    [Fact]
    public async Task Rest400_WithoutCode_IsInvalidRequest()
    {
        _handler.Respond(HttpStatusCode.BadRequest, "{\"title\":\"One or more validation errors occurred.\",\"status\":400}",
            "application/problem+json");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().GetFolderStatsAsync("Files", cancellationToken: Ct));
        Assert.Equal(FileErrorCodes.InvalidRequest, e.Code);
        Assert.Equal(HttpStatusCode.BadRequest, e.HttpStatusCode);
    }

    [Fact]
    public void AssetServicesClient_Files_IsCreatedOnce_UnderConcurrentAccess()
    {
        var client = new AssetServicesClient(
            new AssetServiceClientOptions { EndpointUri = Endpoint, TenantId = Tenant },
            new ServiceClientAccessToken());

        var seen = new System.Collections.Concurrent.ConcurrentBag<IFilesClient>();
        Parallel.For(0, 32, _ => seen.Add(client.Files));

        Assert.Single(seen.Distinct());
    }

    [Fact]
    public void DefaultClients_ShareOneHttpClient_AndDisposeDoesNotCloseIt()
    {
        var options = new AssetServiceClientOptions { EndpointUri = Endpoint, TenantId = Tenant };
        var field = typeof(FilesClient).GetField("_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var a = new FilesClient(options, _token);
        var b = new FilesClient(options, _token);

        Assert.Same(field.GetValue(a), field.GetValue(b));
        a.Dispose();
        var shared = (HttpClient)field.GetValue(b)!;
        Assert.Equal(Timeout.InfiniteTimeSpan, shared.Timeout);
        // A disposed HttpClient throws ObjectDisposedException here; the shared one must survive a.Dispose().
        Assert.Null(Record.Exception(() => shared.CancelPendingRequests()));
    }

    // ---- linked counts, tenant-aware capabilities, FORBIDDEN ---------------------------------------

    [Fact]
    public async Task GetLinkedCounts_PostsTheRtIds_AndReturnsTheCounts()
    {
        _handler.Respond(HttpStatusCode.OK, "{\"counts\":{\"" + FileId + "\":3,\"" + FolderId + "\":0}}");

        var counts = await CreateClient().GetLinkedCountsAsync([FileId, FolderId, FileId, OtherFolderId], Ct);

        var request = Assert.Single(_handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(Files + "linked-counts", request.Wire);
        Assert.Equal("Bearer tok-1", request.Authorization);
        using var body = JsonDocument.Parse(request.BodyText);
        Assert.Equal(3, body.RootElement.GetProperty("rtIds").GetArrayLength()); // de-duplicated
        Assert.Equal(3, counts[FileId]);
        Assert.Equal(0, counts[FolderId]);
        Assert.False(counts.ContainsKey(OtherFolderId)); // hidden or unknown entries are omitted by the server
    }

    [Fact]
    public async Task GetLinkedCounts_MoreThan500_IsSentInBatches()
    {
        var ids = Enumerable.Range(1, 501).Select(i => i.ToString("x24")).ToList();
        _handler.Respond(HttpStatusCode.OK, "{\"counts\":{\"" + ids[0] + "\":1}}")
            .Respond(HttpStatusCode.OK, "{\"counts\":{\"" + ids[500] + "\":4}}");

        var counts = await CreateClient().GetLinkedCountsAsync(ids, Ct);

        Assert.Equal(2, _handler.Requests.Count);
        using var first = JsonDocument.Parse(_handler.Requests[0].BodyText);
        using var second = JsonDocument.Parse(_handler.Requests[1].BodyText);
        Assert.Equal(500, first.RootElement.GetProperty("rtIds").GetArrayLength());
        Assert.Equal(1, second.RootElement.GetProperty("rtIds").GetArrayLength());
        Assert.Equal(1, counts[ids[0]]);
        Assert.Equal(4, counts[ids[500]]);
    }

    [Fact]
    public async Task GetLinkedCounts_EmptyInput_SendsNothing_InvalidId_IsRefused()
    {
        var counts = await CreateClient().GetLinkedCountsAsync([], Ct);
        Assert.Empty(counts);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateClient().GetLinkedCountsAsync(["nope"], Ct));
        Assert.Empty(_handler.Requests);
    }

    [Fact]
    public async Task DeleteImpact_OfAFile_OnAServerWithoutLinkedCounts_HasNoCount()
    {
        _handler
            .RespondData(EntryData("files",
                "{\"__typename\":\"SystemFilesFileSystemItem\",\"rtId\":\"" + FileId + "\",\"name\":\"a.txt\"}"))
            .Respond(HttpStatusCode.MethodNotAllowed);

        var impact = await CreateClient().GetDeleteImpactAsync(FileId, Ct);

        Assert.Null(impact.LinkedCount);
        Assert.Null(impact.Stats);
    }

    [Fact]
    public async Task Capabilities_CarryAvailabilityDeleteCapAndLinkedCounts()
    {
        _handler.Respond(HttpStatusCode.OK,
            "{\"available\":false,\"reason\":\"SYSTEM_FILES_MISSING\",\"apiVersion\":1,\"maxDeleteEntries\":2000," +
            "\"linkedCounts\":true,\"reservedRootNames\":[]}");

        var caps = await CreateClient().GetCapabilitiesAsync(Ct);

        Assert.False(caps.Available);
        Assert.Equal("SYSTEM_FILES_MISSING", caps.Reason);
        Assert.Equal(2000, caps.MaxDeleteEntries);
        Assert.True(caps.LinkedCounts);
    }

    [Fact]
    public async Task Capabilities_FromAnOlderServer_ReadAsAvailable()
    {
        _handler.Respond(HttpStatusCode.OK, "{\"apiVersion\":1,\"maxUploadBytes\":1}");

        var caps = await CreateClient().GetCapabilitiesAsync(Ct);

        Assert.True(caps.Available);
        Assert.Null(caps.Reason);
        Assert.False(caps.LinkedCounts);
    }

    [Fact]
    public async Task GraphQlDataPermissionDenial_OnRename_IsForbidden403()
    {
        _handler
            .RespondData(EntryData("files",
                "{\"__typename\":\"SystemFilesFileSystemItem\",\"rtId\":\"" + FileId + "\",\"name\":\"a.txt\"}"))
            .Respond(HttpStatusCode.OK,
                "{\"errors\":[{\"message\":\"Not allowed.\",\"extensions\":{\"code\":\"FORBIDDEN\"}}]}");

        var e = await Assert.ThrowsAsync<FilesApiException>(() => CreateClient().RenameAsync(FileId, "b.txt", Ct));

        Assert.Equal(FileErrorCodes.Forbidden, e.Code);
        Assert.Equal(HttpStatusCode.Forbidden, e.HttpStatusCode);
        Assert.True(e.IsGraphQlError);
    }

    // ---- wiring ---------------------------------------------------------------------------------

    [Fact]
    public void AssetServicesClient_ExposesOneFilesClient()
    {
        var client = new AssetServicesClient(
            new AssetServiceClientOptions { EndpointUri = Endpoint, TenantId = Tenant },
            new ServiceClientAccessToken());

        Assert.IsType<FilesClient>(client.Files);
        Assert.Same(client.Files, client.Files);
    }

    // ---- helpers --------------------------------------------------------------------------------

    private static string RootsData(params (string WellKnownName, string RtId)[] roots) =>
        "{\"runtime\":{\"systemFilesFolderRoot\":{\"items\":[" + string.Join(",", roots.Select(r =>
            $"{{\"__typename\":\"SystemFilesFolderRoot\",\"rtId\":\"{r.RtId}\",\"name\":\"{r.WellKnownName}\",\"rtWellKnownName\":\"{r.WellKnownName}\"}}")) +
        "]}}}";

    private static string Folder(string rtId, string name) =>
        $"{{\"__typename\":\"SystemFilesFolder\",\"rtId\":\"{rtId}\",\"name\":\"{name}\"}}";

    private static string File(string rtId, string name, long size) =>
        $"{{\"__typename\":\"SystemFilesFileSystemItem\",\"rtId\":\"{rtId}\",\"name\":\"{name}\"," +
        $"\"content\":{{\"binaryId\":\"65a0000000000000000000ff\",\"contentType\":\"application/pdf\",\"size\":{size}}}}}";

    /// <summary>Answer of the children query: the container found under <paramref name="containerAlias" />.</summary>
    private static string ChildrenData(string containerAlias, params string[] children)
    {
        var connection = $"{{\"totalCount\":{children.Length},\"pageInfo\":{{\"endCursor\":null,\"hasNextPage\":false}}," +
                         $"\"items\":[{string.Join(",", children)}]}}";
        var container = $"{{\"items\":[{{\"rtId\":\"x\",\"children\":{connection}}}]}}";
        var empty = "{\"items\":[]}";
        return "{\"runtime\":{" +
               $"\"roots\":{(containerAlias == "roots" ? container : empty)}," +
               $"\"folders\":{(containerAlias == "folders" ? container : empty)}," +
               $"\"files\":{empty}}}}}";
    }

    /// <summary>Answer of the entry-by-id query with <paramref name="itemJson" /> under <paramref name="alias" />.</summary>
    private static string EntryData(string alias, string? itemJson)
    {
        string Part(string name) => name == alias && itemJson != null ? $"{{\"items\":[{itemJson}]}}" : "{\"items\":[]}";
        return $"{{\"runtime\":{{\"roots\":{Part("roots")},\"folders\":{Part("folders")},\"files\":{Part("files")}}}}}";
    }

    private sealed class NonSeekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
    }
}

/// <summary>
///     Content-Disposition parsing (RFC 6266 / RFC 5987).
/// </summary>
public class ContentDispositionParserTests
{
    [Theory]
    [InlineData("attachment; filename=\"a.pdf\"; filename*=UTF-8''%C3%A4%20b.pdf", "ä b.pdf")]
    [InlineData("attachment; filename*=UTF-8''%E2%82%AC%20rates.pdf; filename=\"EUR rates.pdf\"", "€ rates.pdf")]
    [InlineData("inline; filename=\"report.csv\"", "report.csv")]
    [InlineData("attachment; filename=plain.txt", "plain.txt")]
    [InlineData("attachment; filename=\"quo\\\"te.txt\"", "quo\"te.txt")]
    [InlineData("attachment; FILENAME*=utf-8'en'%E6%97%A5%E6%9C%AC.txt", "日本.txt")]
    [InlineData("attachment; filename*=ISO-8859-1''%E4.txt", "ä.txt")]
    [InlineData("attachment; filename=\"fallback.txt\"; filename*=UTF-8''%ZZbroken", "fallback.txt")]
    [InlineData("attachment; filename=\"fallback.txt\"; filename*=UTF-8''%FF%FE", "fallback.txt")]
    [InlineData("attachment; filename=\"semi;colon.txt\"", "semi;colon.txt")]
    [InlineData("attachment", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void GetFileName_PrefersFileNameStar_ThenFileName(string? header, string? expected)
    {
        Assert.Equal(expected, ContentDispositionParser.GetFileName(header));
    }

    [Fact]
    public void GetFileName_RoundTripsTheServerFormat()
    {
        // The server writes: attachment; filename="<ascii>"; filename*=UTF-8''<Uri.EscapeDataString(name)>
        const string name = "Übersicht Q1 (final) – 100%.xlsx";
        var header = $"attachment; filename=\"Ubersicht Q1 (final) _ 100%.xlsx\"; filename*=UTF-8''{Uri.EscapeDataString(name)}";

        Assert.Equal(name, ContentDispositionParser.GetFileName(header));
        Assert.Equal(Encoding.UTF8.GetByteCount(name), Encoding.UTF8.GetByteCount(ContentDispositionParser.GetFileName(header)!));
    }
}
