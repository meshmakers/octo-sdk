namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Files;

/// <summary>
///     Kinds of entries of the platform file system (System.Files, AB#6171).
/// </summary>
public static class FileEntryKinds
{
    /// <summary>A file (<c>System.Files/FileSystemItem</c>).</summary>
    public const string File = "file";

    /// <summary>A folder (<c>System.Files/Folder</c>).</summary>
    public const string Folder = "folder";

    /// <summary>A folder root (<c>System.Files/FolderRoot</c>).</summary>
    public const string Root = "root";
}

/// <summary>
///     Type ids and role ids of the System.Files construction kit model.
/// </summary>
public static class FileSystemCkTypeIds
{
    /// <summary>Type id of a file.</summary>
    public const string FileSystemItem = "System.Files/FileSystemItem";

    /// <summary>Type id of a folder.</summary>
    public const string Folder = "System.Files/Folder";

    /// <summary>Type id of a folder root.</summary>
    public const string FolderRoot = "System.Files/FolderRoot";

    /// <summary>Well-known name of the default root seeded in every tenant.</summary>
    public const string DefaultRootWellKnownName = "Files";

    /// <summary>Identity role required to create folder roots (everything else needs no extra role).</summary>
    public const string FileManagementRole = "FileManagement";
}

/// <summary>
///     A file, folder or root of the platform file system. The REST upload endpoints answer it, and the
///     GraphQL based metadata calls of <see cref="IFilesClient" /> map their results to it.
/// </summary>
public class FileEntryDto
{
    /// <summary>Runtime id of the entity.</summary>
    public string RtId { get; set; } = string.Empty;

    /// <summary>Type id (System.Files/FileSystemItem, System.Files/Folder, System.Files/FolderRoot).</summary>
    public string CkTypeId { get; set; } = string.Empty;

    /// <summary>"file", "folder" or "root" (see <see cref="FileEntryKinds" />).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Name of the entry.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Well-known name of the root the entry was addressed in, when known. For a root it is the root's
    ///     own well-known name.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>Path below the root ('/'-separated, incl. the name), when known; empty for a root.</summary>
    public string? Path { get; set; }

    /// <summary>Runtime id of the parent folder or root, when known.</summary>
    public string? ParentRtId { get; set; }

    /// <summary>Size in bytes (files).</summary>
    public long? Size { get; set; }

    /// <summary>Content type (files).</summary>
    public string? ContentType { get; set; }

    /// <summary>GridFS id of the content (files).</summary>
    public string? BinaryId { get; set; }

    /// <summary>Creation time (UTC).</summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>Last change (UTC).</summary>
    public DateTime? ChangedAt { get; set; }

    /// <summary>Subject that created the entry.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>True when an upload replaced the content of an existing file.</summary>
    public bool Replaced { get; set; }

    /// <summary>True for roots and folders.</summary>
    public bool IsContainer => Kind is FileEntryKinds.Root or FileEntryKinds.Folder;
}

/// <summary>
///     What the file API of the cluster offers (<c>GET {tenant}/v1/files/capabilities</c>).
/// </summary>
public class FilesCapabilitiesDto
{
    /// <summary>
    ///     The file system is usable in this tenant. False when the tenant has no System.Files model yet
    ///     (<see cref="Reason" /> = <c>SYSTEM_FILES_MISSING</c>). Older servers do not send it; it then reads true.
    /// </summary>
    public bool Available { get; set; } = true;

    /// <summary>Why the file system is not available (<c>SYSTEM_FILES_MISSING</c>), null when it is.</summary>
    public string? Reason { get; set; }

    /// <summary>Largest number of entries one delete of a folder or root may remove (LIMIT_EXCEEDED above).</summary>
    public int MaxDeleteEntries { get; set; }

    /// <summary>The linked-counts endpoint exists (<see cref="IFilesClient.GetLinkedCountsAsync" />).</summary>
    public bool LinkedCounts { get; set; }

    /// <summary>Version of the file API contract.</summary>
    public int ApiVersion { get; set; }

    /// <summary>Largest file one upload may carry, in bytes.</summary>
    public long MaxUploadBytes { get; set; }

    /// <summary>The streamed zip endpoint exists.</summary>
    public bool ZipDownload { get; set; }

    /// <summary>Largest number of files in one zip download.</summary>
    public int ZipMaxFiles { get; set; }

    /// <summary>Largest sum of source sizes of one zip download, in bytes.</summary>
    public long ZipMaxBytes { get; set; }

    /// <summary>Largest file clients should preview inline, in bytes (advisory).</summary>
    public long PreviewMaxBytes { get; set; }

    /// <summary>The server deletes folders and roots recursively.</summary>
    public bool RecursiveDelete { get; set; }

    /// <summary>The folder statistics endpoint exists.</summary>
    public bool FolderStats { get; set; }

    /// <summary>Well-known names a folder root cannot use.</summary>
    public List<string> ReservedRootNames { get; set; } = [];
}

/// <summary>
///     Deep statistics of a folder or root: what the caller can see below it, plus how many entries are
///     hidden from the caller by data permissions.
/// </summary>
public class FolderStatsDto
{
    /// <summary>Visible folders below the folder (all levels).</summary>
    public long Folders { get; set; }

    /// <summary>Visible files below the folder (all levels).</summary>
    public long Files { get; set; }

    /// <summary>Total size of the visible files, in bytes.</summary>
    public long Bytes { get; set; }

    /// <summary>Visible files linked to another entity (any association other than the folder tree).</summary>
    public long LinkedFiles { get; set; }

    /// <summary>
    ///     Entries below the folder the caller cannot see. A delete of the folder includes them and fails
    ///     as a whole when the caller may not delete them.
    /// </summary>
    public long HiddenEntries { get; set; }

    /// <summary>False when the server walk hit its limit (counts are lower bounds).</summary>
    public bool Complete { get; set; }

    /// <summary>Up to five linked files with up to three of their visible linked entities.</summary>
    public List<LinkedFileSampleDto> LinkedSamples { get; set; } = [];
}

/// <summary>
///     A file linked to business entities.
/// </summary>
public class LinkedFileSampleDto
{
    /// <summary>Runtime id of the file.</summary>
    public string RtId { get; set; } = string.Empty;

    /// <summary>Name of the file.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Path below the folder the statistics were asked for.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Linked entities visible to the caller.</summary>
    public List<LinkedEntityDto> Entities { get; set; } = [];
}

/// <summary>
///     An entity a file is linked to.
/// </summary>
public class LinkedEntityDto
{
    /// <summary>Runtime id.</summary>
    public string RtId { get; set; } = string.Empty;

    /// <summary>Type id.</summary>
    public string CkTypeId { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
///     Request of a streamed zip download: files and folders addressed by path and/or rtId. Folders are
///     included with everything below them.
/// </summary>
public class ZipRequestDto
{
    /// <summary>Entries addressed by root well-known name + path.</summary>
    public List<FileRefDto>? Items { get; set; }

    /// <summary>Entries addressed by rtId.</summary>
    public List<string>? RtIds { get; set; }

    /// <summary>File name of the zip (server default "files.zip"; ".zip" is appended when missing).</summary>
    public string? FileName { get; set; }
}

/// <summary>
///     A file system entry addressed by path.
/// </summary>
public class FileRefDto
{
    /// <summary>Well-known name of the folder root.</summary>
    public string Root { get; set; } = string.Empty;

    /// <summary>Path below the root; empty for the root itself.</summary>
    public string? Path { get; set; }
}

/// <summary>
///     One page of a file system listing.
/// </summary>
public class FilesPage
{
    /// <summary>Entries of this page.</summary>
    public List<FileEntryDto> Items { get; set; } = [];

    /// <summary>Total number of entries of the listing (all pages).</summary>
    public long TotalCount { get; set; }

    /// <summary>Cursor to pass as <c>after</c> for the next page.</summary>
    public string? EndCursor { get; set; }

    /// <summary>True when another page follows.</summary>
    public bool HasNextPage { get; set; }
}

/// <summary>
///     What a delete of an entry would take with it. The server deletes roots and folders recursively.
/// </summary>
public class DeleteImpactDto
{
    /// <summary>The entry that would be deleted.</summary>
    public FileEntryDto Entry { get; set; } = new();

    /// <summary>
    ///     Deep statistics below the entry (roots and folders); <c>null</c> for a file — the server's
    ///     statistics endpoint answers containers only.
    /// </summary>
    public FolderStatsDto? Stats { get; set; }

    /// <summary>
    ///     Number of links of a file to other entities (every association except the folder tree, as far as the
    ///     caller can see them); <c>null</c> for roots and folders (see <see cref="FolderStatsDto.LinkedFiles" />)
    ///     and when the server has no linked-counts endpoint.
    /// </summary>
    public int? LinkedCount { get; set; }
}

/// <summary>
///     Request of <c>POST {tenant}/v1/files/linked-counts</c>.
/// </summary>
public class LinkedCountsRequestDto
{
    /// <summary>Runtime ids of files or folders (at most 500 per request).</summary>
    public List<string> RtIds { get; set; } = [];
}

/// <summary>
///     Answer of <c>POST {tenant}/v1/files/linked-counts</c>.
/// </summary>
public class LinkedCountsDto
{
    /// <summary>Links per rtId; entries not visible to the caller are missing.</summary>
    public Dictionary<string, int> Counts { get; set; } = new();
}

/// <summary>
///     What an upload does when the target name already exists in the folder.
/// </summary>
public enum FileConflictMode
{
    /// <summary>Answer 409 NAME_CONFLICT (server default).</summary>
    Fail,

    /// <summary>Store new content for the existing file (rtId and links stay).</summary>
    Replace,

    /// <summary>Store under the next free name ("a (1).pdf").</summary>
    KeepBoth
}

/// <summary>
///     A streamed download: the content stream plus the metadata of the response. Dispose it to release the
///     HTTP response.
/// </summary>
public sealed class FileDownload : IDisposable, IAsyncDisposable
{
    private readonly IDisposable? _owner;

    /// <summary>
    ///     Constructor.
    /// </summary>
    /// <param name="content">The content stream</param>
    /// <param name="fileName">File name from Content-Disposition, if any</param>
    /// <param name="contentType">Content type, if any</param>
    /// <param name="contentLength">Content length, if known</param>
    /// <param name="eTag">ETag (the binary id, quoted), if any</param>
    /// <param name="owner">Object disposed together with the download (the HTTP response)</param>
    public FileDownload(Stream content, string? fileName, string? contentType, long? contentLength,
        string? eTag = null, IDisposable? owner = null)
    {
        Content = content;
        FileName = fileName;
        ContentType = contentType;
        ContentLength = contentLength;
        ETag = eTag;
        _owner = owner;
    }

    /// <summary>The content stream (read it once, forward only).</summary>
    public Stream Content { get; }

    /// <summary>File name sent by the server (RFC 6266/5987 <c>filename*</c> preferred over <c>filename</c>).</summary>
    public string? FileName { get; }

    /// <summary>Content type sent by the server.</summary>
    public string? ContentType { get; }

    /// <summary>Content length, when the server sent one (zip downloads are streamed without).</summary>
    public long? ContentLength { get; }

    /// <summary>ETag sent by the server.</summary>
    public string? ETag { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Content.Dispose();
        _owner?.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
        _owner?.Dispose();
    }
}
