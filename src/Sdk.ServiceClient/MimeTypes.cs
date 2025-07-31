namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
/// Used MIME types in Octo Mesh
/// </summary>
public static class MimeTypes
{
	/// <summary>
	/// Text MIME type
	/// </summary>
	public const string MimeText = "text/plain";
	
    /// <summary>
    /// JSON MIME type
    /// </summary>
    public const string MimeTypeJson = "application/json";
    
    /// <summary>
    /// ZIP MIME type
    /// </summary>
    public const string MimeTypeZip = "application/zip";

    /// <summary>
    /// MIME type for gzipped files
    /// </summary>
    public const string MimeTypeGzip = "application/gzip";

    /// <summary>
    /// Zip a compressed MIME type
    /// </summary>
    public const string MimeTypeXZipCompressed = "application/x-zip-compressed";
    
    /// <summary>
    /// YAML MIME type
    /// </summary>
    public const string MimeTypeYaml = "application/x-yaml";

    /// <summary>
    /// Unknown file type - currently used by windows when uploading YAML files
    /// </summary>
	public const string Unknown = "application/octet-stream";
}