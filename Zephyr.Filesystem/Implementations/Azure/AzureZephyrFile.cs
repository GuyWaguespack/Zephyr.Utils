using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace Zephyr.Filesystem
{
    /// <summary>
    /// The implementation of ZephyrFile using Azure Container (Blob) and Share storage.
    /// </summary>
    public class AzureZephyrFile : ZephyrFile
    {

        private string UrlPattern = @"^https:\/\/(.*?)\.(file|blob)\.core\.windows\.net\/(.*?)\/(.*)$";        // Gets Storage Account, Storage Type, Storage Name, and Object Name

        private AzureClient _client = null;

        /// <summary>
        /// The name of the file or blob in Azure.
        /// </summary>
        public override string Name { get { return FullName.Substring(FullName.LastIndexOf(@"/") + 1); } }

        /// <summary>
        /// The Fullname / URL of the file or blob in Azure.
        /// </summary>
        public override string FullName
        {
            get { return _fullName; }
            set
            {
                _fullName = value;
                Match match = Regex.Match(value, UrlPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    StorageAccount = match.Groups[1]?.Value;
                    StorageType = Enum.Parse<AzureStorageType>(match.Groups[2]?.Value);
                    StorageName = match.Groups[3]?.Value;
                    Key = match.Groups[4]?.Value;
                }
            }
        }

        private string _fullName;

        /// <summary>
        /// The Azure Storage Account associated with the file or blob.
        /// </summary>
        public string StorageAccount { get; internal set; }

        /// <summary>
        /// Indicates whether the AzureZephyrFile represents a blob or a share file.
        /// </summary>
        public AzureStorageType StorageType { get; internal set; }

        /// <summary>
        /// The name of the Azure blob container or file share
        /// </summary>
        public string StorageName { get; internal set; }

        /// <summary>
        /// The Key (blob) or Path (share) to the item in Azure.
        /// </summary>
        public string Key { get; internal set; }


        /// <summary>
        /// Creates an empty AzureZephyrFile
        /// </summary>
        /// <param name="client">The client class used to connect to Azure.</param>
        public AzureZephyrFile(AzureClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Creates an AzureZephyrFile representing the url passed in.
        /// </summary>
        /// <param name="client">The client class used to connect to Azure.</param>
        /// <param name="fullName">The Fullname or URL for the Azure file or blob.</param>
        public AzureZephyrFile(AzureClient client, string url)
        {
            _client = client;
            FullName = url;
        }

        /// <summary>
        /// Does the file or blob exist.
        /// </summary>
        public override bool Exists
        {
            get
            {
                bool exists = false;
                if (_client == null)
                    throw new Exception($"AzureClient Not Set.");

                if (this.StorageType == AzureStorageType.blob)
                {
                    BlobClient blob = new BlobClient(_client.ConnectionString, this.StorageName, this.Key);
                    exists = blob.Exists();
                }
                else if (this.StorageType == AzureStorageType.file)
                {
                    ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                    ShareDirectoryClient dir = share.GetRootDirectoryClient();
                    ShareFileClient file = dir.GetFileClient(this.Key);
                    exists = file.Exists();
                }
                else
                    throw new Exception($"Unknown AzureStorageType [{this.StorageType}] For File [{this.FullName}]");

                return exists;
            }
        }

        /// <summary>
        /// Implementation of the ZephyrFile Close method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        public override void Close(bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            if (IsOpen)
            {
                Flush();
                this.Stream.Close();
                if (verbose)
                    Logger.Log($"Memory Stream [{FullName}] Has Been Closed.", callbackLabel, callback);
            }
            else
            {
                if (verbose)
                    Logger.Log($"Memory Stream [{FullName}] Is Already Closed.", callbackLabel, callback);
            }
            this.Stream = null;
        }

        /// <summary>
        /// Implementation of the ZephyrFile Create method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="overwrite">Will overwrite the file if it already exists.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>An instance of a AzureZephyrFile.</returns>
        public override ZephyrFile Create(bool overwrite = true, bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            try
            {
                if (this.Exists && !overwrite)
                    throw new Exception($"File [{this.FullName}] Already Exists.");

                if (_client == null)
                    throw new Exception($"AzureClient Not Set.");

                this.Stream = new System.IO.MemoryStream();
                Flush();

                if (verbose)
                    Logger.Log($"File [{FullName}] Was Created.", callbackLabel, callback);
                return this;
            }
            catch (Exception e)
            {
                Logger.Log($"ERROR - {e.Message}", callbackLabel, callback);
                throw;
            }
        }

        /// <summary>
        /// Implementation of the ZephyrFile CreateDirectory method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="dirName">Full name or URL of the directory to be created.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>An AzureZephyrDirectory implementation.</returns>
        public override ZephyrDirectory CreateDirectory(string fullName, bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            return new AzureZephyrDirectory(_client, fullName);
        }

        /// <summary>
        /// Implementation of the ZephyrFile CreateFile method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="fullName">Full name or URL of the file to be created.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>An AzureZephyrFile implementation.</returns>
        public override ZephyrFile CreateFile(string fullName, bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            return new AzureZephyrFile(_client, fullName);
        }

        /// <summary>
        /// Implementation of the ZephyrFile Delete method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="stopOnError">Throw an exception when an error occurs.</param>
        /// <param name="verbose">Log details of file deleted.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>"true" if completely successful, "false" if any part failed</returns>
        public override bool Delete(bool stopOnError = true, bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            bool success = true;
            try
            {
                if (this.StorageType == AzureStorageType.blob)
                {
                    BlobContainerClient container = new BlobContainerClient(_client.ConnectionString, this.StorageName);
                    container.DeleteBlobIfExists(this.Key);
                }
                else if (this.StorageType == AzureStorageType.file)
                {
                    ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                    ShareDirectoryClient dir = share.GetRootDirectoryClient();
                    ShareFileClient file = dir.GetFileClient(this.Key);

                    file.DeleteIfExists();
                }
                else
                    throw new Exception($"Unknown AzureStorageType [{this.StorageType}] For File [{this.FullName}]");

                if (verbose)
                    Logger.Log($"File [{FullName}] Was Deleted.", callbackLabel, callback);
            }
            catch (Exception e)
            {
                success = false;
                Logger.Log(e.Message, callbackLabel, callback);
                if (stopOnError)
                    throw;
            }

            return success;
        }

        /// <summary>
        /// Implementation of the ZephyrFile Flush method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        public override void Flush(bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            if (this.StorageType == AzureStorageType.blob)
            {
                BlobContainerClient container = new BlobContainerClient(_client.ConnectionString, this.StorageName);
                container.DeleteBlobIfExists(this.Key);
                this.Stream.Position = 0;
                container.UploadBlob(this.Key, this.Stream);
            }
            else if (this.StorageType == AzureStorageType.file)
            {
                ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                ShareDirectoryClient dir = share.GetRootDirectoryClient();
                ShareFileClient file = dir.GetFileClient(this.Key);

                file.DeleteIfExists();
                file.Create(this.Stream.Length);

                // Buffering Code Below Adapted Directly From Gaurav Mantri's StackOverflow Answer
                // https://stackoverflow.com/questions/61001985/issue-with-azure-chunked-upload-to-fileshare-via-azure-storage-files-shares-libr

                BinaryReader reader = new BinaryReader(this.Stream);
                if (this.Stream.Length > 0)
                {
                    this.Stream.Position = 0;
                    int blockSize = 1 * 1024 * 1024;    // 1 MB
                    long offset = 0;

                    while (true)
                    {
                        byte[] buffer = reader.ReadBytes(blockSize);
                        if (buffer.Length == 0)
                            break;

                        MemoryStream uploadChunk = new MemoryStream();
                        uploadChunk.Write(buffer, 0, buffer.Length);
                        uploadChunk.Position = 0;

                        Azure.HttpRange httpRange = new Azure.HttpRange(offset, buffer.Length);
                        file.UploadRange(httpRange, uploadChunk);
                        offset += buffer.Length;
                    }

                    reader.Close();
                }
            }
            else
                throw new Exception($"Unknown AzureStorageType [{this.StorageType}] For File [{this.FullName}]");

        }

        /// <summary>
        /// Implementation of the ZephyrFile Open method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="access">Specifies to open Stream with "Read" or "Write" access.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>The open Stream for the AzureZephyrFile.</returns>
        public override Stream Open(AccessType access, bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            this.Stream = new System.IO.MemoryStream();


            if (this.StorageType == AzureStorageType.blob)
            {
                BlobContainerClient container = new BlobContainerClient(_client.ConnectionString, this.StorageName);
                BlobClient client = container.GetBlobClient(this.Key);
                if (this.Exists)
                    client.DownloadTo(this.Stream);
            }
            else if (this.StorageType == AzureStorageType.file)
            {
                ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                ShareDirectoryClient dir = share.GetRootDirectoryClient();
                ShareFileClient file = dir.GetFileClient(this.Key);

                if (this.Exists)
                {
                    ShareFileProperties properties = file.GetProperties();
                    if (properties.ContentLength > 0)
                    {
                        ShareFileDownloadInfo response = file.Download();
                        response.Content.CopyTo(this.Stream);
                    }
                }
                
            }
            else
                throw new Exception($"Unknown AzureStorageType [{this.StorageType}] For File [{this.FullName}]");

            if (access == AccessType.Read)
                this.Stream.Position = 0;

            return this.Stream;
        }
    }
}
