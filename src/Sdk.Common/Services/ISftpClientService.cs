namespace Meshmakers.Octo.Sdk.Common.Services;

/// <summary>
/// Provides methods to interact with an SFTP server, such as uploading and downloading files.
/// </summary>
public interface ISftpClientService : IDisposable
{
    /// <summary>
    /// Uploads a single file to the specified directory on the SFTP server.
    /// </summary>
    /// <param name="localFilePath">The local file path of the file to be uploaded.</param>
    /// <param name="remoteDirectory">The remote directory on the SFTP server where the file should be uploaded.</param>
    /// <param name="deleteLocalFile">Specifies whether to delete the local file after successful upload. Default is false.</param>
    void UploadSingleFile(string localFilePath, string remoteDirectory, bool deleteLocalFile = false);

    /// <summary>
    /// Downloads a single file from the SFTP server to local directory and deletes it from the remote server
    /// </summary>
    /// <param name="remoteFilePath">The remote file path to download</param>
    /// <param name="localDirectory">The local directory to save the file</param>
    void DownloadAndDeleteSingleFile(string remoteFilePath, string localDirectory);
    
    /// <summary>
    /// Uploads files from local directory to remote directory based on search pattern
    /// </summary>
    /// <param name="localDirectory">The local directory containing files to upload</param>
    /// <param name="remoteDirectory">The remote directory to upload files to</param>
    /// <param name="searchPattern">The search pattern for files to upload</param>
    /// <param name="deleteLocalFiles">Whether to delete local files after successful upload</param>
    /// <returns>True if all uploads were successful, false otherwise</returns>
    bool UploadFiles(string localDirectory, string remoteDirectory, string searchPattern, bool deleteLocalFiles = false);
    
    /// <summary>
    /// Downloads files from remote directory to local directory based on search pattern and deletes them from remote
    /// </summary>
    /// <param name="remoteDirectory">The remote directory to download files from</param>
    /// <param name="localDirectory">The local directory to save files to</param>
    /// <param name="searchPattern">The search pattern for files to download</param>
    void DownloadAndDeleteFiles(string remoteDirectory, string localDirectory, string searchPattern);
}