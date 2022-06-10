using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace Zephyr.Filesystem
{
    /// <summary>
    /// The implementation of ZephyrDirectory using Azure Blob and Share Storage.
    /// </summary>
    public class AzureZephyrDirectory : ZephyrDirectory
    {
        private string UrlPattern = @"^https:\/\/(.*?)\.(file|blob)\.core\.windows\.net\/(.*?)\/(.*)$";        // Gets Storage Account, Storage Type, Storage Name, and Object Name
        private string NamePattern = @"^(https:\/\/.*?\..*\.core\.windows\.net\/.*?\/)(.*)$";       // Used To Get Parent Name, Name and Root

        private AzureClient _client = null;
        private string blobFilePlaceholderName = "_";

        /// <summary>
        /// The name of the directory in Azure.
        /// </summary>
        public override string Name
        {
            get
            {
                String name = null;
                Match match = Regex.Match(FullName, NamePattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string key = match.Groups[2].Value;
                    if (!String.IsNullOrWhiteSpace(key))
                    {
                        string[] parts = key.Split('/',StringSplitOptions.RemoveEmptyEntries);
                        name = parts[parts.Length - 1];
                    }
                }
                return name;
            }
        }

        /// <summary>
        /// The fullname / url of the directory in Azure.
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

        /// <summary>
        /// The full path / url of the parent directory in Azure.
        /// </summary>
        public override string Parent
        {
            get
            {
                String parent = null;
                Match match = Regex.Match(FullName, NamePattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string key = match.Groups[2].Value;
                    if (!String.IsNullOrWhiteSpace(key))
                    {
                        string[] parts = key.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 1)
                            parent = match.Groups[1].Value;
                        else
                            parent = match.Groups[1].Value + String.Join('/', parts, 0, parts.Length - 1) + "/";
                    }
                }
                return parent;
            }
        }

        /// <summary>
        /// The root or protocol for the Azure Blob or Share directory.
        /// </summary>
        public override string Root
        {
            get
            {
                String root = null;
                Match match = Regex.Match(FullName, NamePattern, RegexOptions.IgnoreCase);
                if (match.Success)
                    root = match.Groups[1].Value;

                return root;
            }
        }


        private string _fullName;

        /// <summary>
        /// The Azure Storage Account associated with the file or blob directory.
        /// </summary>
        public string StorageAccount { get; internal set; }

        /// <summary>
        /// Indicates whether the AzureZephyrFile represents a blob or a share directory.
        /// </summary>
        public AzureStorageType StorageType { get; internal set; }

        /// <summary>
        /// The name of the Azure blob container or directory share
        /// </summary>
        public string StorageName { get; internal set; }

        /// <summary>
        /// The Key (blob) or Path (share) to the directory in Azure.
        /// </summary>
        public string Key { get; internal set; }


        /// <summary>
        /// Creates an empty AzureZephyrDirectory
        /// </summary>
        /// <param name="client">The client class used to connect to Azure.</param>
        public AzureZephyrDirectory(AzureClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Creates an AzureZephyrDirectory representing the url passed in.
        /// </summary>
        /// <param name="client">The client class used to connect to Azure.</param>
        /// <param name="url">The Fullname or URL for the Azure blob or share directory.</param>
        public AzureZephyrDirectory(AzureClient client, string url)
        {
            _client = client;
            FullName = url;
        }


        /// <summary>
        /// Implementation of the ZephyrDirectory Exists method in Azure Blob or Share Storage.
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
                    BlobServiceClient blob = new BlobServiceClient(_client.ConnectionString);
                    BlobContainerClient container = blob.GetBlobContainerClient(this.StorageName);
                    Pageable<BlobItem> blobs = container.GetBlobs(BlobTraits.None, BlobStates.All, this.Key);
                    IEnumerator<BlobItem> blobEnumerator = blobs.GetEnumerator();
                    while (blobEnumerator.MoveNext() && !exists)
                        if (!blobEnumerator.Current.Deleted)
                            exists = true;
                }
                else if (this.StorageType == AzureStorageType.file)
                {
                    ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                    ShareDirectoryClient dir = share.GetDirectoryClient(this.Key);
                    exists = dir.Exists();
                }
                else
                    throw new Exception($"Unknown AzureStorageType [{this.StorageType}] For Directory [{this.FullName}]");

                return exists;
            }
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory Separaptor method in Azure Blob or Share Storage.
        /// </summary>
        public override char Separator { get { return '/'; } }


        /// <summary>
        /// Implementation of the ZephyrDirectory Create method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="failIfExists">Throws an error if the directory already exists.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>An AzureZephyrDirectory Instance.</returns>
        public override ZephyrDirectory Create(bool failIfExists = false, bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            if (_client == null)
                throw new Exception($"AzureClient Not Set.");

            bool exists = this.Exists;
            if (exists && failIfExists)
                throw new Exception($"Directory [{FullName}] Already Exists.");

            if (this.StorageType == AzureStorageType.blob)
            {
                if (!exists)
                {
                    BlobServiceClient blob = new BlobServiceClient(_client.ConnectionString);
                    BlobContainerClient container = blob.GetBlobContainerClient(this.StorageName);
                    BlobClient client = container.GetBlobClient(this.Key + blobFilePlaceholderName);    // Create Zero Byte "Placeholder" File
                    MemoryStream nullStream = new MemoryStream();
                    client.Upload(nullStream, !failIfExists);
                }
            }
            else if (this.StorageType == AzureStorageType.file)
            {
                if (!exists)
                {
                    ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                    ShareDirectoryClient dir = share.GetDirectoryClient(this.Key);
                    dir.Create();
                }
            }
            else
                throw new Exception($"Unknown AzureStorageType [{this.StorageType}] For Directory [{this.FullName}]");

            if (verbose)
                Logger.Log($"Directory [{FullName}] Was Created.", callbackLabel, callback);
            return this;
        }

        /// <summary>
        /// Creates an AzureZephyrDirectory implementation using the Fullname / URL passed in.
        /// </summary>
        /// <param name="fullName">Full name or URL of the directory to be created.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>An AzureZephyrDirectory implementation.</returns>
        public override ZephyrDirectory CreateDirectory(string fullName, bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            return new AzureZephyrDirectory(_client, fullName);
        }

        /// <summary>
        /// Creates an AzureZephyrFile implementation using the Fullname / URL passed in.
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
        /// Implementation of the ZephyrDirectory Delete method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="recurse">Remove all objects in the directory as well.  If set to "false", directory must be empty or an exception will be thrown.</param>
        /// <param name="stopOnError">Stop deleting objects in the directory if an error is encountered.</param>
        /// <param name="verbose">Log each object that is deleted from the directory.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>"true" if completely successful, "false" if any part failed</returns>
        public override bool Delete(bool recurse = true, bool stopOnError = true, bool verbose = true, string callbackLabel = null, Action<string, string> callback = null)
        {
            bool success = true;
            try
            {
                if (_client == null)
                    throw new Exception($"AzureClient Not Set.");

                bool exists = this.Exists;
                if (this.StorageType == AzureStorageType.blob)
                {
                    if (exists)
                    {
                        BlobServiceClient blob = new BlobServiceClient(_client.ConnectionString);
                        BlobContainerClient container = blob.GetBlobContainerClient(this.StorageName);
                        Pageable<BlobItem> blobs = container.GetBlobs(BlobTraits.None, BlobStates.All, this.Key);
                        IEnumerator<BlobItem> blobEnum = blobs.GetEnumerator();
                        int count = 0;
                        while (blobEnum.MoveNext())
                        {
                            BlobItem current = blobEnum.Current;
                            if (current.Deleted)                    // Ignore Blobs Marked For Deletion
                                continue;

                            count++;
                            if (!recurse)
                            {
                                if (count > 1)
                                    throw new Exception($"Directory [{FullName}] is not empty.");
                                else if (current.Name != this.Key + blobFilePlaceholderName)
                                    throw new Exception($"Directory [{FullName}] is not empty.");
                            }

                            if (verbose)
                                Logger.Log($"BlobItem [{this.FullName}{current.Name}] Was Deleted.", callbackLabel, callback);


                            container.DeleteBlob(current.Name);
                        }
                    }
                }
                else if (this.StorageType == AzureStorageType.file)
                {
                    if (exists)
                    {
                        ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                        ShareDirectoryClient dir = share.GetDirectoryClient(this.Key);

                        if (recurse)
                        {
                            foreach (ZephyrDirectory subDir in this.GetDirectories())
                            {
                                bool status = subDir.Delete(recurse, stopOnError, verbose, callbackLabel, callback);
                                if (!status)
                                    success = false;
                            }

                            foreach (ZephyrFile subFile in this.GetFiles())
                            {
                                bool status = subFile.Delete(stopOnError, verbose, callbackLabel, callback);
                                if (!status)
                                    success = false;
                            }
                        }
                        dir.Delete();   // Will Fail If Directory Is Not Empty
                    }
                }

                if (verbose)
                    Logger.Log($"Directory [{FullName}] Was Deleted.", callbackLabel, callback);
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
        /// Implementation of the ZephyrDirectory GetDirectories method in Azure Blob and Share Storage.
        /// </summary>
        /// <returns>An enumeration of AzureZephyrDirectory objects.</returns>
        public override IEnumerable<ZephyrDirectory> GetDirectories()
        {
            List<ZephyrDirectory> directories = new List<ZephyrDirectory>();

            if (this.StorageType == AzureStorageType.blob)
            {
                BlobServiceClient blob = new BlobServiceClient(_client.ConnectionString);
                BlobContainerClient container = blob.GetBlobContainerClient(this.StorageName);
                Pageable<BlobItem> blobs = container.GetBlobs(BlobTraits.None, BlobStates.All, this.Key);

                HashSet<string> foundDirectories = new HashSet<string>();
                foreach (BlobItem obj in blobs)
                {
                    if (!obj.Deleted)
                    {
                        string itemName = obj.Name.Substring(this.Key.Length);
                        if (itemName.Contains('/'))
                        {
                            string dirName = itemName.Substring(0, itemName.IndexOf('/')+1);
                            if (!foundDirectories.Contains(dirName)) {
                                foundDirectories.Add(dirName);
                                AzureZephyrDirectory zDir = new AzureZephyrDirectory(_client, this.PathCombine(this.FullName, dirName));
                                directories.Add(zDir);
                            }
                        }
                    }
                }
            }
            else if (this.StorageType == AzureStorageType.file)
            {
                ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                ShareDirectoryClient dir = share.GetDirectoryClient(this.Key);
                Pageable<ShareFileItem> objects = dir.GetFilesAndDirectories();

                foreach (ShareFileItem obj in objects)
                {
                    if (obj.IsDirectory)
                    {
                        AzureZephyrDirectory zDir = new AzureZephyrDirectory(_client, this.PathCombine(this.FullName, obj.Name));
                        directories.Add(zDir);
                    }
                }
            }

            return directories;
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory GetFiles method in Azure Blob and Share Storage.
        /// </summary>
        /// <returns>An enumeration of AzureZephyrFile objects.</returns>
        public override IEnumerable<ZephyrFile> GetFiles()
        {
            List<ZephyrFile> files = new List<ZephyrFile>();

            if (this.StorageType == AzureStorageType.blob)
            {
                BlobServiceClient blob = new BlobServiceClient(_client.ConnectionString);
                BlobContainerClient container = blob.GetBlobContainerClient(this.StorageName);
                Pageable<BlobItem> blobs = container.GetBlobs(BlobTraits.None, BlobStates.All, this.Key);

                foreach (BlobItem item in blobs)
                {
                    if (!item.Deleted)
                    {
                        string itemName = item.Name.Substring(this.Key.Length);
                        if (!itemName.Contains('/'))
                        {
                            AzureZephyrFile zFile = new AzureZephyrFile(_client, this.PathCombine(this.FullName, itemName));
                            files.Add(zFile);
                        }
                    }
                }
            }
            else if (this.StorageType == AzureStorageType.file)
            {
                ShareClient share = new ShareClient(_client.ConnectionString, this.StorageName);
                ShareDirectoryClient dir = share.GetDirectoryClient(this.Key);
                Pageable<ShareFileItem> sFiles = dir.GetFilesAndDirectories();

                foreach (ShareFileItem file in sFiles)
                {
                    if (!file.IsDirectory)
                    {
                        AzureZephyrFile zFile = new AzureZephyrFile(_client, this.PathCombine(this.FullName, file.Name));
                        files.Add(zFile);
                    }
                }
            }

            return files;
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory PathCombine method in Azure Blob and Share Storage.
        /// </summary>
        /// <param name="paths">An array of strings to combine.</param>
        /// <returns>The combined paths.</returns>
        public override string PathCombine(params string[] paths)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i]?.Trim();
                if (path == null)
                    continue;
                else if (path.EndsWith("/"))
                    sb.Append(path);
                else if (i == paths.Length - 1)
                    sb.Append(path);
                else
                    sb.Append($"{path}/");
            }

            return sb.ToString();
        }
    }
}
