namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Files;

/// <summary>
///     Typed client of the platform file system of a tenant (System.Files, AB#6171 / AB#6229).
/// </summary>
/// <remarks>
///     <para>
///         Bytes go over REST (<c>{assetServices}/{tenant}/v1/files</c>): upload, download, zip, statistics,
///         capabilities. Metadata — roots, listings, create folder, rename, move, delete — goes over the
///         tenant's generic GraphQL endpoint (<c>tenants/{tenant}/GraphQL</c>, System.Files types), as decided
///         for the platform (design Q1). Both carry the client's bearer token; access is the caller's data
///         permissions, only <see cref="CreateRootAsync" /> needs the <c>FileManagement</c> role.
///     </para>
///     <para>
///         Paths are '/'-separated below a root addressed by its well-known name (default root <c>Files</c>).
///         Every segment is URL-encoded on its own; '.' and '..' segments are refused client-side with an
///         <see cref="ArgumentException" />. Server errors throw <see cref="FilesApiException" /> with the stable
///         code (<see cref="FileErrorCodes" />); 401 throws <see cref="UnauthorizedServiceAccessException" />.
///     </para>
/// </remarks>
public interface IFilesClient
{
    // ---- REST: bytes ---------------------------------------------------------------------------

    /// <summary>
    ///     Limits and features of the file API of the cluster.
    /// </summary>
    Task<FilesCapabilitiesDto> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Uploads a file by path (<c>PUT {root}/{folders…}/{name}</c>, raw body).
    /// </summary>
    /// <param name="root">Well-known name of the folder root</param>
    /// <param name="path">Path below the root, ending with the file name</param>
    /// <param name="content">Content; streamed, the length is sent when the stream is seekable</param>
    /// <param name="contentType">Content type; null lets the server derive it from the name</param>
    /// <param name="conflict">What to do when the name exists</param>
    /// <param name="createFolders">Create missing folders on the way (otherwise 404 PATH_NOT_FOUND)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The stored file; <see cref="FileEntryDto.Replaced" /> is true when existing content was replaced</returns>
    Task<FileEntryDto> UploadAsync(string root, string path, Stream content, string? contentType = null,
        FileConflictMode conflict = FileConflictMode.Fail, bool createFolders = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Uploads a file into the folder or root addressed by rtId (<c>POST items/{rtId}/content?name=</c>).
    /// </summary>
    Task<FileEntryDto> UploadToFolderAsync(string folderRtId, string name, Stream content,
        string? contentType = null, FileConflictMode conflict = FileConflictMode.Fail,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Replaces the content of the file addressed by rtId; rtId and links stay
    ///     (<c>PUT items/{rtId}/content</c>).
    /// </summary>
    Task<FileEntryDto> ReplaceContentAsync(string fileRtId, Stream content, string? contentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads a file by path. Dispose the result.
    /// </summary>
    /// <param name="root">Well-known name of the folder root</param>
    /// <param name="path">Path of the file below the root</param>
    /// <param name="inline">Ask for inline delivery (only changes the Content-Disposition type)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<FileDownload> DownloadAsync(string root, string path, bool inline = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Downloads a file by rtId. Dispose the result.
    /// </summary>
    Task<FileDownload> DownloadByIdAsync(string fileRtId, bool inline = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Streamed zip of files and folders (folders recursively). Limits are checked by the server before
    ///     the first byte (413 LIMIT_EXCEEDED). Dispose the result.
    /// </summary>
    Task<FileDownload> DownloadZipAsync(ZipRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Streamed zip of the entries with the given rtIds. Dispose the result.
    /// </summary>
    Task<FileDownload> DownloadZipAsync(IEnumerable<string> rtIds, string? fileName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deep statistics of a root (<paramref name="path" /> empty) or folder addressed by path.
    /// </summary>
    Task<FolderStatsDto> GetFolderStatsAsync(string root, string? path = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deep statistics of a root or folder addressed by rtId.
    /// </summary>
    Task<FolderStatsDto> GetFolderStatsByIdAsync(string folderRtId, CancellationToken cancellationToken = default);

    // ---- GraphQL: metadata ---------------------------------------------------------------------

    /// <summary>
    ///     Lists the folder roots visible to the caller, sorted by name.
    /// </summary>
    Task<FilesPage> ListRootsAsync(int first = 100, string? after = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Resolves an entry by path (empty path = the root itself). Names match case-insensitively, an exact
    ///     match wins; throws ROOT_NOT_FOUND / PATH_NOT_FOUND / AMBIGUOUS_PATH like the REST API.
    /// </summary>
    Task<FileEntryDto> GetEntryAsync(string root, string? path = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     An entry by rtId (with its parent rtId), or null when it does not exist or is hidden.
    /// </summary>
    Task<FileEntryDto?> GetEntryByIdAsync(string rtId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists the direct children (folders and files, sorted by name) of a root or folder addressed by path.
    /// </summary>
    Task<FilesPage> ListChildrenAsync(string root, string? path = null, int first = 100, string? after = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists the direct children (folders and files, sorted by name) of a root or folder addressed by rtId.
    /// </summary>
    Task<FilesPage> ListChildrenByIdAsync(string folderRtId, int first = 100, string? after = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a folder root. Requires the <c>FileManagement</c> role (else FORBIDDEN); the well-known
    ///     name must be unique (NAME_CONFLICT) and not reserved (RESERVED_NAME).
    /// </summary>
    Task<FileEntryDto> CreateRootAsync(string name, string wellKnownName,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a folder below the root or folder addressed by path (<paramref name="parentPath" /> empty =
    ///     directly in the root).
    /// </summary>
    Task<FileEntryDto> CreateFolderAsync(string root, string? parentPath, string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Creates a folder below the root or folder addressed by rtId.
    /// </summary>
    Task<FileEntryDto> CreateFolderInAsync(string parentRtId, string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Renames a root, folder or file. Service-owned and blueprint roots answer PROTECTED_ROOT.
    /// </summary>
    Task RenameAsync(string rtId, string newName, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Moves a folder or file into another root or folder. A folder cannot move into itself or below
    ///     itself (MOVE_INTO_ITSELF); the name must be free in the target (NAME_CONFLICT).
    /// </summary>
    Task MoveAsync(string rtId, string targetFolderRtId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes a root, folder or file. Roots and folders are deleted recursively by the server, all or
    ///     nothing: when the caller may not delete any entry below, nothing is deleted. Check
    ///     <see cref="GetDeleteImpactAsync" /> first.
    /// </summary>
    Task DeleteAsync(string rtId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     What a delete of the entry would take with it: for roots and folders the deep statistics (counts,
    ///     bytes, linked files with samples of their linked entities, entries hidden by data permissions).
    /// </summary>
    Task<DeleteImpactDto> GetDeleteImpactAsync(string rtId, CancellationToken cancellationToken = default);
}
