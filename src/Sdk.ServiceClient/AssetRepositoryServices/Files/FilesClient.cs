using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;
using Microsoft.Extensions.Options;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Files;

/// <summary>
///     Implementation of <see cref="IFilesClient" /> over <see cref="HttpClient" />: REST for bytes
///     (<c>{endpoint}/{tenant}/v1/files</c>), the tenant's GraphQL endpoint (<c>{endpoint}/tenants/{tenant}/GraphQL</c>)
///     for metadata.
/// </summary>
/// <remarks>
///     A plain <see cref="HttpClient" /> instead of RestSharp because uploads and downloads must stream
///     (100 MB files) and the problem-details <c>code</c> and the GraphQL <c>extensions.code</c> must reach the
///     caller; the shared GraphQL client of <c>TenantClient</c> drops both. The bearer token is read from the
///     access token object on every request, so a refreshed token is used at once.
/// </remarks>
public sealed class FilesClient : IFilesClient, IDisposable
{
    private const string ContainerTypeIdsLiteral = "[\"System.Files/FolderRoot\", \"System.Files/Folder\"]";
    private const string ChildTypeIdsLiteral = "[\"System.Files/Folder\", \"System.Files/FileSystemItem\"]";
    private const string SortByName = "[{attributePath: \"name\", sortOrder: ASCENDING}]";

    private const string RootFields =
        "__typename rtId name rtWellKnownName rtCreationDateTime rtChangedDateTime rtCreatedBy";

    private const string FolderFields = "__typename rtId name rtCreationDateTime rtChangedDateTime rtCreatedBy";

    private const string FileFields =
        "__typename rtId name rtCreationDateTime rtChangedDateTime rtCreatedBy content { binaryId filename contentType size }";

    private const string ParentSelection =
        "parent(ckTypeIds: " + ContainerTypeIdsLiteral + ", first: 2) { items { __typename " +
        "... on SystemFilesFolderRoot { rtId } ... on SystemFilesFolder { rtId } } }";

    private const string ChildrenConnection =
        "totalCount pageInfo { endCursor hasNextPage } items { __typename " +
        "... on SystemFilesFolder { " + FolderFields + " } ... on SystemFilesFileSystemItem { " + FileFields + " } }";

    private const string ChildrenArguments =
        "ckTypeIds: " + ChildTypeIdsLiteral + ", first: $first, after: $after, fieldFilter: $filter, sortOrder: " +
        SortByName;

    /// <summary>
    ///     Name lookups send no sortOrder: the engine's sorted association path counts the unfiltered
    ///     associations as totalCount when a fieldFilter is set and answers INCOMPLETE_SLICE as soon as the folder
    ///     has more than one child. A lookup needs no order anyway.
    /// </summary>
    private const string LookupChildrenArguments =
        "ckTypeIds: " + ChildTypeIdsLiteral + ", first: $first, after: $after, fieldFilter: $filter";

    internal const string RootsQuery =
        "query filesRoots($first: Int, $after: String) { runtime { systemFilesFolderRoot(first: $first, after: $after, " +
        "sortOrder: " + SortByName + ") { totalCount pageInfo { endCursor hasNextPage } items { " + RootFields +
        " } } } }";

    internal const string RootByNameQuery =
        "query filesRootByName($filter: [FieldFilter]) { runtime { systemFilesFolderRoot(fieldFilter: $filter, first: 10) " +
        "{ items { " + RootFields + " } } } }";

    internal const string EntryByIdQuery =
        "query filesEntry($rtId: OctoObjectId!) { runtime { " +
        "roots: systemFilesFolderRoot(rtId: $rtId) { items { " + RootFields + " } } " +
        "folders: systemFilesFolder(rtId: $rtId) { items { " + FolderFields + " " + ParentSelection + " } } " +
        "files: systemFilesFileSystemItem(rtId: $rtId) { items { " + FileFields + " " + ParentSelection + " } } } }";

    internal const string ChildrenQuery =
        "query filesChildren($rtId: OctoObjectId!, $first: Int, $after: String, $filter: [FieldFilter]) { runtime { " +
        "roots: systemFilesFolderRoot(rtId: $rtId) { items { rtId children(" + ChildrenArguments + ") { " +
        ChildrenConnection + " } } } " +
        "folders: systemFilesFolder(rtId: $rtId) { items { rtId children(" + ChildrenArguments + ") { " +
        ChildrenConnection + " } } } " +
        "files: systemFilesFileSystemItem(rtId: $rtId) { items { rtId } } } }";

    internal const string ChildLookupQuery =
        "query filesChildLookup($rtId: OctoObjectId!, $first: Int, $after: String, $filter: [FieldFilter]) { runtime { " +
        "roots: systemFilesFolderRoot(rtId: $rtId) { items { rtId children(" + LookupChildrenArguments + ") { " +
        ChildrenConnection + " } } } " +
        "folders: systemFilesFolder(rtId: $rtId) { items { rtId children(" + LookupChildrenArguments + ") { " +
        ChildrenConnection + " } } } " +
        "files: systemFilesFileSystemItem(rtId: $rtId) { items { rtId } } } }";

    internal const string CreateRootMutation =
        "mutation filesCreateRoot($name: String!, $wellKnownName: String!) { runtime { systemFilesFolderRoots { " +
        "create(entities: [{name: $name, rtWellKnownName: $wellKnownName}]) { " + RootFields + " } } } }";

    internal const string CreateFolderMutation =
        "mutation filesCreateFolder($name: String!, $parentCkTypeId: RtCkTypeId!, $parentRtId: OctoObjectId!) { " +
        "runtime { systemFilesFolders { create(entities: [{name: $name, parent: [{modOption: CREATE, " +
        "target: {ckTypeId: $parentCkTypeId, rtId: $parentRtId}}]}]) { " + FolderFields + " } } } }";

    internal const string DeleteMutation =
        "mutation filesDelete($rtEntityIds: [RtEntityId]!) { runtime { runtimeEntities { delete(entities: $rtEntityIds) } } }";

    /// <summary>Largest number of rtIds the server accepts per linked-counts request.</summary>
    internal const int LinkedCountsBatchSize = 500;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Regex RtIdPattern = new("^[0-9a-fA-F]{24}$", RegexOptions.Compiled);

    private static readonly Lock SharedClientGate = new();
    private static HttpClient? _sharedValidatingClient;
    private static HttpClient? _sharedAnyCertificateClient;

    private readonly IServiceClientAccessToken _accessToken;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly AssetServiceClientOptions _options;

    /// <summary>
    ///     Constructor for dependency injection.
    /// </summary>
    /// <param name="options">Asset services client options (endpoint and tenant)</param>
    /// <param name="accessToken">Access token of the asset services client</param>
    public FilesClient(IOptions<AssetServiceClientOptions> options, IAssetServiceClientAccessToken accessToken)
        : this(options.Value, accessToken)
    {
    }

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="options">Asset services client options (endpoint and tenant)</param>
    /// <param name="accessToken">Access token</param>
    /// <remarks>
    ///     Uses a process-wide <see cref="HttpClient" /> over one pooled <see cref="SocketsHttpHandler" />
    ///     (connections recycled every five minutes, so DNS changes are picked up), so short-lived clients — the
    ///     MCP server creates an asset client per tool call — do not open a connection pool each.
    /// </remarks>
    public FilesClient(AssetServiceClientOptions options, IServiceClientAccessToken accessToken)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        _httpClient = SharedHttpClient();
        _ownsHttpClient = false;
    }

    /// <summary>
    ///     Constructor with an explicit message handler (custom transports, tests).
    /// </summary>
    /// <param name="options">Asset services client options (endpoint and tenant)</param>
    /// <param name="accessToken">Access token</param>
    /// <param name="handler">HTTP message handler</param>
    /// <param name="disposeHandler">Dispose the handler with the client</param>
    public FilesClient(AssetServiceClientOptions options, IServiceClientAccessToken accessToken,
        HttpMessageHandler handler, bool disposeHandler = true)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        // Timeouts are applied per call (SendWithTimeoutAsync): a client-wide timeout would cut off large uploads.
        _httpClient = new HttpClient(handler, disposeHandler) { Timeout = Timeout.InfiniteTimeSpan };
        _ownsHttpClient = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    ///     The process-wide client. One per certificate-validation mode, because
    ///     <see cref="ServerCertificateTrust.AllowAnyServerCertificate" /> may be switched on at start-up after a
    ///     first client was created.
    /// </summary>
    private static HttpClient SharedHttpClient()
    {
        lock (SharedClientGate)
        {
            if (ServerCertificateTrust.AllowsAnyServerCertificate)
            {
                return _sharedAnyCertificateClient ??= CreateSharedClient();
            }

            return _sharedValidatingClient ??= CreateSharedClient();
        }
    }

    private static HttpClient CreateSharedClient()
    {
        var handler = ServerCertificateTrust.Apply(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        });
        return new HttpClient(handler, false) { Timeout = Timeout.InfiniteTimeSpan };
    }

    /// <summary>Timeout of metadata, stats and capabilities calls (<see cref="ServiceClientOptions.MaxTimeout" />).</summary>
    private TimeSpan RequestTimeout => _options.MaxTimeout > 0
        ? TimeSpan.FromMilliseconds(_options.MaxTimeout)
        : Timeout.InfiniteTimeSpan;

    /// <summary>
    ///     Timeout of byte transfers (<see cref="AssetServiceClientOptions.FileTransferTimeout" />): the whole upload,
    ///     a download until its response headers arrive.
    /// </summary>
    private TimeSpan TransferTimeout => _options.FileTransferTimeout > TimeSpan.Zero
        ? _options.FileTransferTimeout
        : Timeout.InfiniteTimeSpan;

    // =============================================================================================
    // REST
    // =============================================================================================

    /// <inheritdoc />
    public async Task<FilesCapabilitiesDto> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, RestUri("capabilities"));
        return await SendForJsonAsync<FilesCapabilitiesDto>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileEntryDto> UploadAsync(string root, string path, Stream content, string? contentType = null,
        FileConflictMode conflict = FileConflictMode.Fail, bool createFolders = false,
        CancellationToken cancellationToken = default)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var segments = FilePathEncoder.Split(path, nameof(path));
        if (segments.Count == 0)
        {
            throw new ArgumentException("The path must end with the file name.", nameof(path));
        }

        var query = $"?conflict={ConflictValue(conflict)}" + (createFolders ? "&createFolders=true" : string.Empty);
        using var request = new HttpRequestMessage(HttpMethod.Put,
            RestUri(FilePathEncoder.EncodeRoute(root, segments) + query))
        {
            Content = new UploadContent(content, contentType)
        };
        return await SendForJsonAsync<FileEntryDto>(request, cancellationToken, transfer: true).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileEntryDto> UploadToFolderAsync(string folderRtId, string name, Stream content,
        string? contentType = null, FileConflictMode conflict = FileConflictMode.Fail,
        CancellationToken cancellationToken = default)
    {
        ValidateRtId(folderRtId, nameof(folderRtId));
        ValidateName(name, nameof(name));
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post,
            RestUri($"items/{folderRtId}/content?name={Uri.EscapeDataString(name)}&conflict={ConflictValue(conflict)}"))
        {
            Content = new UploadContent(content, contentType)
        };
        return await SendForJsonAsync<FileEntryDto>(request, cancellationToken, transfer: true).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FileEntryDto> ReplaceContentAsync(string fileRtId, Stream content, string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRtId(fileRtId, nameof(fileRtId));
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        using var request = new HttpRequestMessage(HttpMethod.Put, RestUri($"items/{fileRtId}/content"))
        {
            Content = new UploadContent(content, contentType)
        };
        return await SendForJsonAsync<FileEntryDto>(request, cancellationToken, transfer: true).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<FileDownload> DownloadAsync(string root, string path, bool inline = false,
        CancellationToken cancellationToken = default)
    {
        var segments = FilePathEncoder.Split(path, nameof(path));
        if (segments.Count == 0)
        {
            throw new ArgumentException("The path must name a file.", nameof(path));
        }

        var uri = RestUri(FilePathEncoder.EncodeRoute(root, segments) + (inline ? "?inline=true" : string.Empty));
        return DownloadCoreAsync(new HttpRequestMessage(HttpMethod.Get, uri), cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileDownload> DownloadByIdAsync(string fileRtId, bool inline = false,
        CancellationToken cancellationToken = default)
    {
        ValidateRtId(fileRtId, nameof(fileRtId));
        var uri = RestUri($"items/{fileRtId}/content" + (inline ? "?inline=true" : string.Empty));
        return DownloadCoreAsync(new HttpRequestMessage(HttpMethod.Get, uri), cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileDownload> DownloadZipAsync(ZipRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if ((request.Items?.Count ?? 0) + (request.RtIds?.Count ?? 0) == 0)
        {
            throw new ArgumentException("Select at least one file or folder (items or rtIds).", nameof(request));
        }

        foreach (var item in request.Items ?? [])
        {
            if (string.IsNullOrWhiteSpace(item.Root))
            {
                throw new ArgumentException("Every zip item needs a root.", nameof(request));
            }

            FilePathEncoder.Split(item.Path, nameof(request));
        }

        foreach (var rtId in request.RtIds ?? [])
        {
            ValidateRtId(rtId, nameof(request));
        }

        var message = new HttpRequestMessage(HttpMethod.Post, RestUri("zip"))
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        return DownloadCoreAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public Task<FileDownload> DownloadZipAsync(IEnumerable<string> rtIds, string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        if (rtIds == null)
        {
            throw new ArgumentNullException(nameof(rtIds));
        }

        return DownloadZipAsync(new ZipRequestDto { RtIds = rtIds.ToList(), FileName = fileName }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FolderStatsDto> GetFolderStatsAsync(string root, string? path = null,
        CancellationToken cancellationToken = default)
    {
        var segments = FilePathEncoder.Split(path, nameof(path));
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("The root well-known name must not be empty.", nameof(root));
        }

        var query = $"stats?root={Uri.EscapeDataString(root)}";
        if (segments.Count > 0)
        {
            query += $"&path={Uri.EscapeDataString(FilePathEncoder.Join(segments))}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, RestUri(query));
        return await SendForJsonAsync<FolderStatsDto>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FolderStatsDto> GetFolderStatsByIdAsync(string folderRtId,
        CancellationToken cancellationToken = default)
    {
        ValidateRtId(folderRtId, nameof(folderRtId));
        using var request = new HttpRequestMessage(HttpMethod.Get, RestUri($"items/{folderRtId}/stats"));
        return await SendForJsonAsync<FolderStatsDto>(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, int>> GetLinkedCountsAsync(IEnumerable<string> rtIds,
        CancellationToken cancellationToken = default)
    {
        if (rtIds == null)
        {
            throw new ArgumentNullException(nameof(rtIds));
        }

        var ids = rtIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var rtId in ids)
        {
            ValidateRtId(rtId, nameof(rtIds));
        }

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in ids.Chunk(LinkedCountsBatchSize))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, RestUri("linked-counts"))
            {
                Content = JsonContent.Create(new LinkedCountsRequestDto { RtIds = batch.ToList() },
                    options: JsonOptions)
            };
            var answer = await SendForJsonAsync<LinkedCountsDto>(request, cancellationToken).ConfigureAwait(false);
            foreach (var pair in answer.Counts)
            {
                result[pair.Key] = pair.Value;
            }
        }

        return result;
    }

    // =============================================================================================
    // GraphQL
    // =============================================================================================

    /// <inheritdoc />
    public async Task<FilesPage> ListRootsAsync(int first = 100, string? after = null,
        CancellationToken cancellationToken = default)
    {
        ValidateFirst(first);
        var data = await GraphQlAsync(RootsQuery, new Dictionary<string, object?>
        {
            ["first"] = first,
            ["after"] = after
        }, cancellationToken).ConfigureAwait(false);

        var connection = data.GetProperty("runtime").GetProperty("systemFilesFolderRoot");
        return ParsePage(connection, null);
    }

    /// <inheritdoc />
    public async Task<FileEntryDto> GetEntryAsync(string root, string? path = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("The root well-known name must not be empty.", nameof(root));
        }

        var segments = FilePathEncoder.Split(path, nameof(path));

        var rootData = await GraphQlAsync(RootByNameQuery, new Dictionary<string, object?>
        {
            ["filter"] = NameFilter("rtWellKnownName", root)
        }, cancellationToken).ConfigureAwait(false);
        var roots = Items(rootData.GetProperty("runtime").GetProperty("systemFilesFolderRoot"))
            .Select(i => ParseEntry(i, FileEntryKinds.Root)).ToList();
        var current = PickByName(roots, root, r => r.Root ?? string.Empty, root, FileErrorCodes.RootNotFound);
        current.Root = current.Root ?? root;
        current.Path = string.Empty;

        for (var i = 0; i < segments.Count; i++)
        {
            var displayPath = $"{root}/{FilePathEncoder.Join(segments.Take(i + 1))}";
            if (!current.IsContainer)
            {
                throw new FilesApiException(FileErrorCodes.PathNotFound, HttpStatusCode.NotFound,
                    $"'{displayPath}' not found.");
            }

            var page = await QueryChildrenAsync(current.RtId, 10, null, NameFilter("name", segments[i]),
                cancellationToken).ConfigureAwait(false);
            var next = PickByName(page.Items, segments[i], e => e.Name, displayPath, FileErrorCodes.PathNotFound);
            next.Root = current.Root;
            next.Path = FilePathEncoder.Join(segments.Take(i + 1));
            next.ParentRtId = current.RtId;
            current = next;
        }

        return current;
    }

    /// <inheritdoc />
    public async Task<FileEntryDto?> GetEntryByIdAsync(string rtId, CancellationToken cancellationToken = default)
    {
        return (await FindByIdAsync(rtId, cancellationToken).ConfigureAwait(false))?.Entry;
    }

    /// <inheritdoc />
    public async Task<FilesPage> ListChildrenAsync(string root, string? path = null, int first = 100,
        string? after = null, CancellationToken cancellationToken = default)
    {
        ValidateFirst(first);
        var folder = await GetEntryAsync(root, path, cancellationToken).ConfigureAwait(false);
        if (!folder.IsContainer)
        {
            throw new FilesApiException(FileErrorCodes.NotAFolder, HttpStatusCode.Conflict,
                $"'{root}/{path}' is not a folder.");
        }

        var page = await QueryChildrenAsync(folder.RtId, first, after, null, cancellationToken).ConfigureAwait(false);
        foreach (var item in page.Items)
        {
            item.Root = folder.Root;
            item.Path = string.IsNullOrEmpty(folder.Path) ? item.Name : $"{folder.Path}/{item.Name}";
        }

        return page;
    }

    /// <inheritdoc />
    public Task<FilesPage> ListChildrenByIdAsync(string folderRtId, int first = 100, string? after = null,
        CancellationToken cancellationToken = default)
    {
        ValidateRtId(folderRtId, nameof(folderRtId));
        ValidateFirst(first);
        return QueryChildrenAsync(folderRtId, first, after, null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileEntryDto> CreateRootAsync(string name, string wellKnownName,
        CancellationToken cancellationToken = default)
    {
        ValidateName(name, nameof(name));
        ValidateName(wellKnownName, nameof(wellKnownName));
        if (wellKnownName.IndexOf('/') >= 0)
        {
            throw new ArgumentException($"'{wellKnownName}' is not a valid root well-known name.",
                nameof(wellKnownName));
        }

        var data = await GraphQlAsync(CreateRootMutation, new Dictionary<string, object?>
        {
            ["name"] = name,
            ["wellKnownName"] = wellKnownName
        }, cancellationToken).ConfigureAwait(false);

        var created = FirstCreated(data.GetProperty("runtime").GetProperty("systemFilesFolderRoots"));
        var entry = ParseEntry(created, FileEntryKinds.Root);
        entry.Path = string.Empty;
        return entry;
    }

    /// <inheritdoc />
    public async Task<FileEntryDto> CreateFolderAsync(string root, string? parentPath, string name,
        CancellationToken cancellationToken = default)
    {
        ValidateName(name, nameof(name));
        var parent = await GetEntryAsync(root, parentPath, cancellationToken).ConfigureAwait(false);
        if (!parent.IsContainer)
        {
            throw new FilesApiException(FileErrorCodes.NotAFolder, HttpStatusCode.Conflict,
                $"'{root}/{parentPath}' is not a folder.");
        }

        var folder = await CreateFolderCoreAsync(parent.RtId, CkTypeIdOf(parent.Kind), name, cancellationToken)
            .ConfigureAwait(false);
        folder.Root = parent.Root;
        folder.Path = string.IsNullOrEmpty(parent.Path) ? folder.Name : $"{parent.Path}/{folder.Name}";
        return folder;
    }

    /// <inheritdoc />
    public async Task<FileEntryDto> CreateFolderInAsync(string parentRtId, string name,
        CancellationToken cancellationToken = default)
    {
        ValidateName(name, nameof(name));
        var parent = await RequireByIdAsync(parentRtId, cancellationToken).ConfigureAwait(false);
        if (!parent.Entry.IsContainer)
        {
            throw new FilesApiException(FileErrorCodes.NotAFolder, HttpStatusCode.Conflict,
                $"'{parent.Entry.Name}' is not a folder.");
        }

        return await CreateFolderCoreAsync(parent.Entry.RtId, CkTypeIdOf(parent.Entry.Kind), name, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RenameAsync(string rtId, string newName, CancellationToken cancellationToken = default)
    {
        ValidateName(newName, nameof(newName));
        var target = await RequireByIdAsync(rtId, cancellationToken).ConfigureAwait(false);
        var field = MutationFieldOf(target.Entry.Kind);
        var document =
            $"mutation filesRename($rtId: OctoObjectId!, $name: String!) {{ runtime {{ {field} {{ " +
            "update(entities: [{rtId: $rtId, item: {name: $name}}]) { rtId name } } } }";
        await GraphQlAsync(document, new Dictionary<string, object?>
        {
            ["rtId"] = target.Entry.RtId,
            ["name"] = newName
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MoveAsync(string rtId, string targetFolderRtId, CancellationToken cancellationToken = default)
    {
        ValidateRtId(targetFolderRtId, nameof(targetFolderRtId));
        var source = await RequireByIdAsync(rtId, cancellationToken).ConfigureAwait(false);
        if (source.Entry.Kind == FileEntryKinds.Root)
        {
            throw new FilesApiException(FileErrorCodes.InvalidRequest, HttpStatusCode.BadRequest,
                "A folder root cannot be moved.");
        }

        var target = await RequireByIdAsync(targetFolderRtId, cancellationToken).ConfigureAwait(false);
        if (!target.Entry.IsContainer)
        {
            throw new FilesApiException(FileErrorCodes.NotAFolder, HttpStatusCode.Conflict,
                $"'{target.Entry.Name}' is not a folder.");
        }

        if (string.Equals(source.Entry.ParentRtId, target.Entry.RtId, StringComparison.OrdinalIgnoreCase))
        {
            return; // already there
        }

        var variables = new Dictionary<string, object?>
        {
            ["rtId"] = source.Entry.RtId,
            ["toCkTypeId"] = CkTypeIdOf(target.Entry.Kind),
            ["toRtId"] = target.Entry.RtId
        };
        var parentDelete = string.Empty;
        var parentVariables = string.Empty;
        if (source.Entry.ParentRtId != null && source.ParentCkTypeId != null)
        {
            variables["fromCkTypeId"] = source.ParentCkTypeId;
            variables["fromRtId"] = source.Entry.ParentRtId;
            parentVariables = ", $fromCkTypeId: RtCkTypeId!, $fromRtId: OctoObjectId!";
            parentDelete = "{modOption: DELETE, target: {ckTypeId: $fromCkTypeId, rtId: $fromRtId}}, ";
        }

        var field = MutationFieldOf(source.Entry.Kind);
        var document =
            $"mutation filesMove($rtId: OctoObjectId!{parentVariables}, $toCkTypeId: RtCkTypeId!, $toRtId: OctoObjectId!) " +
            $"{{ runtime {{ {field} {{ update(entities: [{{rtId: $rtId, item: {{parent: [{parentDelete}" +
            "{modOption: CREATE, target: {ckTypeId: $toCkTypeId, rtId: $toRtId}}]}}]) { rtId } } } }";
        await GraphQlAsync(document, variables, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string rtId, CancellationToken cancellationToken = default)
    {
        var target = await RequireByIdAsync(rtId, cancellationToken).ConfigureAwait(false);
        var data = await GraphQlAsync(DeleteMutation, new Dictionary<string, object?>
        {
            ["rtEntityIds"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["ckTypeId"] = CkTypeIdOf(target.Entry.Kind),
                    ["rtId"] = target.Entry.RtId
                }
            }
        }, cancellationToken).ConfigureAwait(false);

        if (data.TryGetProperty("runtime", out var runtime) &&
            runtime.TryGetProperty("runtimeEntities", out var entities) &&
            entities.TryGetProperty("delete", out var deleted) &&
            deleted.ValueKind == JsonValueKind.False)
        {
            throw new FilesApiException(FileErrorCodes.Unknown, HttpStatusCode.InternalServerError,
                $"The server did not delete '{target.Entry.Name}'.", true);
        }
    }

    /// <inheritdoc />
    public async Task<DeleteImpactDto> GetDeleteImpactAsync(string rtId, CancellationToken cancellationToken = default)
    {
        var target = await RequireByIdAsync(rtId, cancellationToken).ConfigureAwait(false);
        if (target.Entry.IsContainer)
        {
            return new DeleteImpactDto
            {
                Entry = target.Entry,
                Stats = await GetFolderStatsByIdAsync(target.Entry.RtId, cancellationToken).ConfigureAwait(false)
            };
        }

        int? linkedCount;
        try
        {
            var counts = await GetLinkedCountsAsync([target.Entry.RtId], cancellationToken).ConfigureAwait(false);
            linkedCount = counts.TryGetValue(target.Entry.RtId, out var count) ? count : 0;
        }
        catch (FilesApiException e) when (e.HttpStatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed)
        {
            linkedCount = null; // server without the linked-counts endpoint
        }

        return new DeleteImpactDto { Entry = target.Entry, LinkedCount = linkedCount };
    }

    // =============================================================================================
    // internals
    // =============================================================================================

    private async Task<FileEntryDto> CreateFolderCoreAsync(string parentRtId, string parentCkTypeId, string name,
        CancellationToken cancellationToken)
    {
        var data = await GraphQlAsync(CreateFolderMutation, new Dictionary<string, object?>
        {
            ["name"] = name,
            ["parentCkTypeId"] = parentCkTypeId,
            ["parentRtId"] = parentRtId
        }, cancellationToken).ConfigureAwait(false);

        var entry = ParseEntry(FirstCreated(data.GetProperty("runtime").GetProperty("systemFilesFolders")),
            FileEntryKinds.Folder);
        entry.ParentRtId = parentRtId;
        return entry;
    }

    private async Task<FilesPage> QueryChildrenAsync(string folderRtId, int first, string? after, object? filter,
        CancellationToken cancellationToken)
    {
        var data = await GraphQlAsync(filter == null ? ChildrenQuery : ChildLookupQuery, new Dictionary<string, object?>
        {
            ["rtId"] = folderRtId,
            ["first"] = first,
            ["after"] = after,
            ["filter"] = filter
        }, cancellationToken).ConfigureAwait(false);

        var runtime = data.GetProperty("runtime");
        foreach (var alias in new[] { "roots", "folders" })
        {
            var container = Items(runtime.GetProperty(alias)).FirstOrDefault();
            if (container.ValueKind == JsonValueKind.Object)
            {
                return ParsePage(container.GetProperty("children"), folderRtId);
            }
        }

        if (Items(runtime.GetProperty("files")).Any())
        {
            throw new FilesApiException(FileErrorCodes.NotAFolder, HttpStatusCode.Conflict,
                $"'{folderRtId}' is not a folder.");
        }

        throw new FilesApiException(FileErrorCodes.PathNotFound, HttpStatusCode.NotFound,
            $"File system entry '{folderRtId}' not found.");
    }

    private async Task<ResolvedEntry> RequireByIdAsync(string rtId, CancellationToken cancellationToken)
    {
        return await FindByIdAsync(rtId, cancellationToken).ConfigureAwait(false)
               ?? throw new FilesApiException(FileErrorCodes.PathNotFound, HttpStatusCode.NotFound,
                   $"File system entry '{rtId}' not found.");
    }

    private async Task<ResolvedEntry?> FindByIdAsync(string rtId, CancellationToken cancellationToken)
    {
        ValidateRtId(rtId, nameof(rtId));
        var data = await GraphQlAsync(EntryByIdQuery, new Dictionary<string, object?> { ["rtId"] = rtId },
            cancellationToken).ConfigureAwait(false);
        var runtime = data.GetProperty("runtime");

        foreach (var (alias, kind) in new[]
                 {
                     ("roots", FileEntryKinds.Root), ("folders", FileEntryKinds.Folder), ("files", FileEntryKinds.File)
                 })
        {
            var item = Items(runtime.GetProperty(alias)).FirstOrDefault();
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var entry = ParseEntry(item, kind);
            string? parentCkTypeId = null;
            if (kind == FileEntryKinds.Root)
            {
                entry.Path = string.Empty;
            }
            else if (item.TryGetProperty("parent", out var parent))
            {
                var parentItem = Items(parent).FirstOrDefault();
                if (parentItem.ValueKind == JsonValueKind.Object)
                {
                    entry.ParentRtId = GetString(parentItem, "rtId");
                    parentCkTypeId = CkTypeIdOf(KindOfTypeName(GetString(parentItem, "__typename"))
                                                ?? FileEntryKinds.Folder);
                }
            }

            return new ResolvedEntry(entry, parentCkTypeId);
        }

        return null;
    }

    private async Task<JsonElement> GraphQlAsync(string query, Dictionary<string, object?> variables,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlUri());
        request.Content = JsonContent.Create(new Dictionary<string, object?>
        {
            ["query"] = query,
            ["variables"] = variables
        }, options: JsonOptions);
        using var response = await SendWithTimeoutAsync(request, HttpCompletionOption.ResponseContentRead,
            RequestTimeout, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedServiceAccessException("The GraphQL endpoint refused the access token.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        JsonDocument? json = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    json = JsonDocument.Parse(body);
                }
                catch (JsonException)
                {
                    json = null;
                }
            }

            if (json != null && json.RootElement.ValueKind == JsonValueKind.Object &&
                json.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            {
                throw GraphQlError(errors);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw ErrorFromBody(response.StatusCode, body);
            }

            if (json == null || !json.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Object)
            {
                throw new FilesApiException(FileErrorCodes.Unknown, HttpStatusCode.NoContent,
                    "The GraphQL endpoint returned no data.", true);
            }

            return data.Clone();
        }
        finally
        {
            json?.Dispose();
        }
    }

    private static FilesApiException GraphQlError(JsonElement errors)
    {
        var first = errors[0];
        var message = GetString(first, "message");
        string? code = null;
        if (first.TryGetProperty("extensions", out var extensions) && extensions.ValueKind == JsonValueKind.Object)
        {
            code = GetString(extensions, "code");
        }

        code = string.IsNullOrWhiteSpace(code) ? FileErrorCodes.Unknown : code!;
        if (errors.GetArrayLength() > 1)
        {
            message += $" (+{errors.GetArrayLength() - 1} more error(s))";
        }

        return new FilesApiException(code, FileErrorCodes.StatusOfGraphQl(code), message, true);
    }

    private async Task<T> SendForJsonAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken,
        bool transfer = false)
        where T : class
    {
        using var response = await SendAsync(request, HttpCompletionOption.ResponseContentRead,
                transfer ? TransferTimeout : RequestTimeout, cancellationToken)
            .ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return result ?? throw new FilesApiException(FileErrorCodes.Unknown, HttpStatusCode.NoContent,
            "The service returned no data.");
    }

    private async Task<FileDownload> DownloadCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, TransferTimeout,
                    cancellationToken)
                .ConfigureAwait(false);
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            string? disposition = null;
            if (response.Content.Headers.NonValidated.TryGetValues("Content-Disposition", out var values))
            {
                disposition = string.Join(", ", values);
            }

            var download = new FileDownload(stream,
                ContentDispositionParser.GetFileName(disposition),
                response.Content.Headers.ContentType?.ToString(),
                response.Content.Headers.ContentLength,
                response.Headers.ETag?.Tag,
                response);
            response = null; // owned by the download now
            return download;
        }
        finally
        {
            response?.Dispose();
            request.Dispose();
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completion,
        TimeSpan timeout, CancellationToken cancellationToken)
    {
        var response = await SendWithTimeoutAsync(request, completion, timeout, cancellationToken)
            .ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedServiceAccessException("The file API refused the access token.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw ErrorFromBody(response.StatusCode, body);
        }
    }

    /// <summary>
    ///     Sends with the bearer token and a per-call timeout. With <see cref="HttpCompletionOption.ResponseContentRead" />
    ///     the timeout covers the request body and the whole response; with
    ///     <see cref="HttpCompletionOption.ResponseHeadersRead" /> (downloads) it ends once the headers arrived, so a
    ///     long download is never cut off. A timeout throws <see cref="TimeoutException" />, a cancellation by the
    ///     caller stays an <see cref="OperationCanceledException" />.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithTimeoutAsync(HttpRequestMessage request,
        HttpCompletionOption completion, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ApplyAuthorization(request);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout != Timeout.InfiniteTimeSpan)
        {
            timeoutSource.CancelAfter(timeout);
        }

        try
        {
            return await _httpClient.SendAsync(request, completion, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested &&
                                                   timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"The file API did not complete {request.Method} {request.RequestUri?.AbsolutePath} within {timeout}.", e);
        }
    }

    /// <summary>
    ///     Maps an error response to <see cref="FilesApiException" />: problem details <c>code</c> first, then a
    ///     code-shaped <c>title</c>, then a fallback derived from the status (e.g. an ingress 413 page).
    /// </summary>
    internal static FilesApiException ErrorFromBody(HttpStatusCode status, string? body)
    {
        string? code = null;
        string? detail = null;
        if (!string.IsNullOrWhiteSpace(body) && body!.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                using var problem = JsonDocument.Parse(body);
                var root = problem.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    code = GetString(root, "code");
                    var title = GetString(root, "title");
                    if (string.IsNullOrWhiteSpace(code) && title != null && IsCodeShaped(title))
                    {
                        code = title;
                    }

                    detail = GetString(root, "detail") ?? title;
                }
            }
            catch (JsonException)
            {
                // not JSON after all
            }
        }

        if (detail == null && !string.IsNullOrWhiteSpace(body))
        {
            detail = body!.Length > 500 ? body.Substring(0, 500) : body;
        }

        return new FilesApiException(string.IsNullOrWhiteSpace(code) ? FileErrorCodes.FallbackFor(status) : code!,
            status, detail);
    }

    private static bool IsCodeShaped(string value) =>
        value.Length > 0 && value.All(c => c is >= 'A' and <= 'Z' or '_' or >= '0' and <= '9');

    private void ApplyAuthorization(HttpRequestMessage request)
    {
        var token = _accessToken.AccessToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private Uri RestUri(string relative)
    {
        var (endpoint, tenantId) = RequireConfiguration();
        return new Uri($"{endpoint.TrimEnd('/')}/{tenantId}/v1/files/{relative}");
    }

    private Uri GraphQlUri()
    {
        var (endpoint, tenantId) = RequireConfiguration();
        return new Uri($"{endpoint.TrimEnd('/')}/tenants/{tenantId}/GraphQL");
    }

    private (string Endpoint, string TenantId) RequireConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.EndpointUri))
        {
            throw new ServiceConfigurationMissingException("Asset Repo Services URI is missing.");
        }

        if (string.IsNullOrWhiteSpace(_options.TenantId))
        {
            throw new ServiceConfigurationMissingException("Asset Repo Services tenant ID is missing.");
        }

        return (_options.EndpointUri!, Uri.EscapeDataString(_options.TenantId!));
    }

    private static string ConflictValue(FileConflictMode conflict) => conflict switch
    {
        FileConflictMode.Fail => "fail",
        FileConflictMode.Replace => "replace",
        FileConflictMode.KeepBoth => "keepBoth",
        _ => throw new ArgumentOutOfRangeException(nameof(conflict), conflict, null)
    };

    private static object NameFilter(string attributePath, string name) => new[]
    {
        new Dictionary<string, object?>
        {
            ["attributePath"] = attributePath,
            ["operator"] = "MATCH_REG_EX",
            // Same match as the server's path resolution: case-insensitive, whole name.
            ["comparisonValue"] = $"(?i)^{Regex.Escape(name)}$"
        }
    };

    private static FileEntryDto PickByName(IReadOnlyList<FileEntryDto> candidates, string name,
        Func<FileEntryDto, string> nameOf, string displayPath, string notFoundCode)
    {
        var exact = candidates.Where(c => string.Equals(nameOf(c), name, StringComparison.Ordinal)).ToList();
        var matches = exact.Count > 0
            ? exact
            : candidates.Where(c => string.Equals(nameOf(c), name, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 0)
        {
            throw new FilesApiException(notFoundCode, HttpStatusCode.NotFound,
                notFoundCode == FileErrorCodes.RootNotFound
                    ? $"Folder root '{displayPath}' not found."
                    : $"'{displayPath}' not found.");
        }

        if (matches.Count > 1)
        {
            throw new FilesApiException(FileErrorCodes.AmbiguousPath, HttpStatusCode.Conflict,
                $"'{displayPath}' matches {matches.Count} entries; address it by rtId.");
        }

        return matches[0];
    }

    private static FilesPage ParsePage(JsonElement connection, string? parentRtId)
    {
        var page = new FilesPage();
        if (connection.ValueKind != JsonValueKind.Object)
        {
            return page;
        }

        if (connection.TryGetProperty("totalCount", out var total) && total.ValueKind == JsonValueKind.Number)
        {
            page.TotalCount = total.GetInt64();
        }

        if (connection.TryGetProperty("pageInfo", out var pageInfo) && pageInfo.ValueKind == JsonValueKind.Object)
        {
            page.EndCursor = GetString(pageInfo, "endCursor");
            page.HasNextPage = pageInfo.TryGetProperty("hasNextPage", out var hasNext) &&
                               hasNext.ValueKind == JsonValueKind.True;
        }

        foreach (var item in Items(connection))
        {
            var entry = ParseEntry(item, null);
            entry.ParentRtId ??= parentRtId;
            page.Items.Add(entry);
        }

        return page;
    }

    private static FileEntryDto ParseEntry(JsonElement item, string? kindHint)
    {
        var kind = KindOfTypeName(GetString(item, "__typename")) ?? kindHint ?? FileEntryKinds.File;
        var entry = new FileEntryDto
        {
            RtId = GetString(item, "rtId") ?? string.Empty,
            Kind = kind,
            CkTypeId = CkTypeIdOf(kind),
            Name = GetString(item, "name") ?? string.Empty,
            CreatedAt = GetDate(item, "rtCreationDateTime"),
            ChangedAt = GetDate(item, "rtChangedDateTime"),
            CreatedBy = GetString(item, "rtCreatedBy")
        };

        if (kind == FileEntryKinds.Root)
        {
            entry.Root = GetString(item, "rtWellKnownName");
        }

        if (item.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Object)
        {
            entry.BinaryId = GetString(content, "binaryId");
            entry.ContentType = GetString(content, "contentType");
            entry.Size = GetLong(content, "size");
        }

        return entry;
    }

    private static JsonElement FirstCreated(JsonElement mutationField)
    {
        if (mutationField.TryGetProperty("create", out var created) && created.ValueKind == JsonValueKind.Array &&
            created.GetArrayLength() > 0)
        {
            return created[0];
        }

        throw new FilesApiException(FileErrorCodes.Unknown, HttpStatusCode.NoContent,
            "The GraphQL create returned no entity.", true);
    }

    private static IEnumerable<JsonElement> Items(JsonElement connection)
    {
        if (connection.ValueKind == JsonValueKind.Object && connection.TryGetProperty("items", out var items) &&
            items.ValueKind == JsonValueKind.Array)
        {
            return items.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.Object).ToList();
        }

        return [];
    }

    private static string? KindOfTypeName(string? typeName) => typeName switch
    {
        "SystemFilesFolderRoot" => FileEntryKinds.Root,
        "SystemFilesFolder" => FileEntryKinds.Folder,
        "SystemFilesFileSystemItem" => FileEntryKinds.File,
        _ => null
    };

    private static string CkTypeIdOf(string kind) => kind switch
    {
        FileEntryKinds.Root => FileSystemCkTypeIds.FolderRoot,
        FileEntryKinds.Folder => FileSystemCkTypeIds.Folder,
        _ => FileSystemCkTypeIds.FileSystemItem
    };

    private static string MutationFieldOf(string kind) => kind switch
    {
        FileEntryKinds.Root => "systemFilesFolderRoots",
        FileEntryKinds.Folder => "systemFilesFolders",
        _ => "systemFilesFileSystemItems"
    };

    private static string? GetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static long? GetLong(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String &&
               long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static DateTime? GetDate(JsonElement element, string property)
    {
        var text = GetString(element, property);
        return text != null && DateTime.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date)
            ? date
            : null;
    }

    private static void ValidateRtId(string rtId, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(rtId) || !RtIdPattern.IsMatch(rtId))
        {
            throw new ArgumentException($"'{rtId}' is not a valid rtId (24 hex characters).", parameterName);
        }
    }

    private static void ValidateName(string name, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The name must not be empty.", parameterName);
        }
    }

    private static void ValidateFirst(int first)
    {
        if (first <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(first), first, "The page size must be positive.");
        }
    }

    private sealed record ResolvedEntry(FileEntryDto Entry, string? ParentCkTypeId);

    /// <summary>
    ///     Streams the caller's stream as the request body without disposing it (a <see cref="StreamContent" />
    ///     would close the caller's stream with the request).
    /// </summary>
    private sealed class UploadContent : HttpContent
    {
        private readonly Stream _source;

        public UploadContent(Stream source, string? contentType)
        {
            _source = source;
            var type = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType!;
            if (!MediaTypeHeaderValue.TryParse(type, out var mediaType))
            {
                throw new ArgumentException($"'{contentType}' is not a valid content type.", nameof(contentType));
            }

            Headers.ContentType = mediaType;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            _source.CopyToAsync(stream);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context,
            CancellationToken cancellationToken) =>
            _source.CopyToAsync(stream, cancellationToken);

        protected override bool TryComputeLength(out long length)
        {
            if (_source.CanSeek)
            {
                length = _source.Length - _source.Position;
                return true;
            }

            length = 0;
            return false;
        }
    }
}
