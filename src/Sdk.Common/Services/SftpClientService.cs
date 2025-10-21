using System.Text.RegularExpressions;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Configuration for SFTP node, specifying connection details and local directory for file operations.
/// </summary>
public record SftpNodeConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// The username used for authentication to the SFTP server.
    /// </summary>
    public required string User { get; set; }

    /// <summary>
    /// The password used for authentication to the SFTP server.
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// The hostname or IP address of the SFTP server to connect to.
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    /// The port number used to connect to the SFTP server.
    /// </summary>
    public required int Port { get; set; }

    /// <summary>
    /// The local directory where files will be downloaded from the SFTP server.
    /// </summary>
    public required string LocalDirectory { get; set; }
}

/// <summary>
/// Service for handling SFTP operations
/// </summary>
public class SftpClientService : ISftpClientService
{
    private readonly SftpNodeConfiguration _config;
    private readonly ILogger<SftpClientService> _logger;
    private readonly SftpClient _client;
    private bool _isConnected;

    /// <summary>
    /// Provides a service for interacting with SFTP servers, including file uploads, downloads, and deletions.
    /// </summary>
    public SftpClientService(SftpNodeConfiguration sftpNodeConfiguration, ILogger<SftpClientService>? logger = null)
    {
        _logger = logger ?? NullLogger<SftpClientService>.Instance;
        _config = sftpNodeConfiguration;
        _client = GetClient();
    }

    /// <summary>
    /// Uploads a single file from the specified local file path to a remote directory on the SFTP server.
    /// </summary>
    /// <param name="localFilePath">The full path to the local file that needs to be uploaded.</param>
    /// <param name="remoteDirectory">The path to the destination directory on the SFTP server where the file will be uploaded.</param>
    /// <param name="deleteLocalFile">Determines whether the local file should be deleted after a successful upload. Defaults to false.</param>
    public void UploadSingleFile(string localFilePath, string remoteDirectory, bool deleteLocalFile = false)
    {
        Connect();
        Upload(localFilePath, remoteDirectory);
        if (deleteLocalFile)
            DeleteLocalFile(localFilePath);
        Disconnect();
    }

    /// <summary>
    /// Uploads multiple files from the specified local directory to a remote directory on the SFTP server.
    /// </summary>
    /// <param name="localDirectory">The path to the local directory containing files to be uploaded.</param>
    /// <param name="remoteDirectory">The path to the destination directory on the SFTP server.</param>
    /// <param name="searchPattern">A search string used to locate specific files in the local directory for upload.</param>
    /// <param name="deleteLocalFiles">Determines whether the local files should be deleted after a successful upload.</param>
    /// <returns>True if all files are uploaded successfully, otherwise false.</returns>
    public bool UploadFiles(string localDirectory, string remoteDirectory, string searchPattern,
        bool deleteLocalFiles = false)
    {
        var files = Directory.GetFiles(localDirectory, searchPattern);
        if (files.Length == 0)
        {
            _logger.LogInformation("Abort upload, there are no files available for upload");
            return false;
        }

        Connect();
        if (!_isConnected)
        {
            _logger.LogError("Upload aborted, unable to establish SFTP connection");
            return false;
        }

        var allUploadsSuccessful = true;
        foreach (var file in files)
        {
            var uploadSuccess = Upload(file, remoteDirectory);
            allUploadsSuccessful &= uploadSuccess;
            if (uploadSuccess && deleteLocalFiles)
                DeleteLocalFile(file);
        }
        Disconnect();
        return allUploadsSuccessful;
    }

    /// <summary>
    /// Downloads a file from the specified remote location to a local directory
    /// and deletes the file from the remote location.
    /// </summary>
    /// <param name="remoteFilePath">The full path of the file on the remote SFTP server.</param>
    /// <param name="localDirectory">The local directory to which the file will be downloaded.</param>
    public void DownloadAndDeleteSingleFile(string remoteFilePath, string localDirectory)
    {
        Connect();
        Download(remoteFilePath, localDirectory);
        DeleteRemoteFile(remoteFilePath);
        Disconnect();
    }

    /// <summary>
    /// Downloads all files from the specified remote directory that match the given search pattern to the local directory,
    /// and deletes the files from the remote directory after successful download.
    /// </summary>
    /// <param name="remoteDirectory">The remote directory on the SFTP server to search for files.</param>
    /// <param name="localDirectory">The local directory where the files will be downloaded.</param>
    /// <param name="searchPattern">The search pattern to match files in the remote directory (e.g., "*.txt").</param>
    public void DownloadAndDeleteFiles(string remoteDirectory, string localDirectory, string searchPattern)
    {
        Connect();
        var files = _client.ListDirectory(remoteDirectory)
            .Where(f => !f.IsDirectory && Match(f.Name, searchPattern))
            .ToList();

        if (files.Count == 0)
        {
            _logger.LogInformation("Abort download, there are no files available for download");
            return;
        }

        foreach (var file in files)
        {
            // var remoteFilePath = Path.Combine(remoteDirectory, file.Name);
            var remoteFilePath = CombinePathCrossPlatform(remoteDirectory, file.Name);
            
            Download(remoteFilePath, localDirectory);
            DeleteRemoteFile(remoteFilePath);
        }

        Disconnect();
    }
    
    private static string CombinePathCrossPlatform(string basePath, string fileName)
    {
        if (basePath.StartsWith("/") || basePath.StartsWith("~")) // SFTP/Unix path
        {
            return $"{basePath.TrimEnd('/')}/{fileName.TrimStart('/')}";
        }
        else
        {
            return Path.Combine(basePath, fileName);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (_isConnected)
                Disconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError("Error disconnecting from SFTP during dispose: {ExMessage}", ex.Message);
        }
        finally
        {
            _client.Dispose();
        }
    }

    private bool Upload(string localFilePath, string remoteDirectory)
    {
        if (!File.Exists(localFilePath))
        {
            _logger.LogInformation("Abort upload, the file does not exist: '{LocalFilePath}'", localFilePath);
            return false;
        }

        var localFileName = Path.GetFileName(localFilePath);
        _logger.LogDebug("Uploading file: '{LocalFileName}'", localFileName);
        
        try
        {
            // First, try the specified directory
            var remoteFilePath = $"{remoteDirectory.TrimEnd('/')}/{localFileName}";
            _logger.LogDebug($"Attempting to upload to: '{remoteFilePath}'");

            try
            {
                EnsureRemoteDirectoryExists(remoteDirectory);
            }
            catch (SftpPermissionDeniedException)
            {
                _logger.LogWarning("No permission to create directory '{RemoteDirectory}', falling back to user's home directory", remoteDirectory);
                // Fall back to user's home directory
                remoteDirectory = ".";
                remoteFilePath = $"{remoteDirectory}/{localFileName}";
            }

            using var fileStream = File.OpenRead(localFilePath);
            _client.UploadFile(fileStream, remoteFilePath);
            _logger.LogInformation("File uploaded: '{LocalFileName}' to '{RemoteFilePath}'", localFileName, remoteFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Error uploading file '{LocalFileName}': {ExMessage}", localFileName, ex.Message);
            throw;
        }
    }

    private void EnsureRemoteDirectoryExists(string remoteDirectory)
    {
        if (string.IsNullOrEmpty(remoteDirectory))
            return;

        var pathParts = remoteDirectory.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var currentPath = "";

        foreach (var part in pathParts)
        {
            currentPath += "/" + part;
            if (!_client.Exists(currentPath))
            {
                _logger.LogDebug("Creating remote directory: '{CurrentPath}'", currentPath);
                _client.CreateDirectory(currentPath);
            }
        }
    }

    private void Download(string remoteFilePath, string localDirectory)
    {
        var remoteFileName = Path.GetFileName(remoteFilePath);
        _logger.LogDebug("Downloading file: '{RemoteFilePath}'", remoteFilePath);
        
        var localFilePath = Path.Combine(localDirectory, remoteFileName);
        using var fileStream = File.Create(localFilePath);
        
        _client.DownloadFile(remoteFilePath, fileStream);
        _logger.LogInformation("File downloaded: '{RemoteFilePath}'", remoteFilePath);
    }

    private void DeleteLocalFile(string localFilePath)
    {
        _logger.LogDebug("Deleting local file: '{LocalFilePath}'", localFilePath);
        File.Delete(localFilePath);
        _logger.LogDebug("File local deleted: '{LocalFilePath}'", localFilePath);
    }

    private void DeleteRemoteFile(string remoteFilePath)
    {
        _logger.LogDebug("Deleting remote file: '{RemoteFilePath}'", remoteFilePath);
        _client.DeleteFile(remoteFilePath);
        _logger.LogInformation("File remote deleted: '{RemoteFilePath}'", remoteFilePath);
    }

    private void Connect()
    {
        if (_isConnected)
            return;
        try
        {
            _client.Connect();
            _isConnected = true;
            _logger.LogDebug("SFTP connection established");
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError("Error connecting to SFTP: {ExMessage}", ex.Message);
            throw;
        }
    }

    private void Disconnect()
    {
        if (!_isConnected)
            return;
        try
        {
            _client.Disconnect();
            _isConnected = false;
            _logger.LogDebug("SFTP connection closed");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error disconnecting from SFTP: {ExMessage}", ex.Message);
        }
    }

    private SftpClient GetClient()
    {
        // The customer uses double quotes in the password.
        // These must be escaped in the JSON, so this is undone here.
        var password = _config.Password.Replace("\\\"", "\"");
        return new SftpClient(_config.Host, _config.Port, _config.User, password);
    }

    private static bool Match(string text, string pattern)
    {
        var regex = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(text, regex, RegexOptions.IgnoreCase);
    }
}