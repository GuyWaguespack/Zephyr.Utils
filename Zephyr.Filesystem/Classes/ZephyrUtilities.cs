using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

//using Alphaleonis.Win32.Filesystem;

namespace Zephyr.Filesystem
{
    /// <summary>
    /// A class of static, helper methods around working with ZephyrFiles and ZephyrDirectories.
    /// </summary>
    public static class ZephryUtilities
    {
        /// <summary>
        /// Determines the type of URL that was passed in.
        /// Basic Rules :
        /// - All directories must end with a slash (/ or \)
        /// - Implementation type is determined by the Url "root".
        ///   s3://         = Amazon S3 Storage
        ///   azblob://     = Azure Blob Storage
        ///   azshare://    = Azure File Storage
        ///   \\            = Network Url
        ///   default       = Local
        /// </summary>
        /// <param name="url">The FullName or URL.</param>
        /// <returns></returns>
        public static UrlType GetUrlType(string url)
        {
            string azRegexStr = @"^https:\/\/(.*?)\.(file|blob)\.core\.windows\.net\/(.*?)\/(.*)$";
            Regex azRegex = new Regex(azRegexStr);
            Match azMatch = azRegex.Match(url);

            UrlType type = UrlType.Unknown;


            if (url != null)
            {
                if (url.StartsWith("s3://", StringComparison.OrdinalIgnoreCase))
                {
                    if (IsDirectory(url))
                        type = UrlType.AwsS3Directory;
                    else
                        type = UrlType.AwsS3File;
                }
                else if (azMatch.Success)
                {
                    if (IsDirectory(url))
                        type = UrlType.AzureDirectory;
                    else
                        type = UrlType.AzureFile;
                }
                else if (url.StartsWith("\\"))
                {
                    if (IsDirectory(url))
                        type = UrlType.NetworkDirectory;
                    else
                        type = UrlType.NetworkFile;
                }
                else
                {
                    if (IsDirectory(url))
                        type = UrlType.LocalDirectory;
                    else
                        type = UrlType.LocalFile;
                }
            }

            return type;
        }

        /// <summary>
        /// In Windows, it is impossible to determine by name alone if a URL is a directory or a file with no extension.  Thus
        /// the decision was made that all directory url's MUST end with a forward or backward slash (/ or \).  This removes any
        /// ambiguity.   This standard should be carried forward to all other implementation types (Aws, Azure, FTP, etc...)
        /// </summary>
        /// <param name="url">The full path to the object.</param>
        /// <returns></returns>
        public static bool IsDirectory(string url)
        {
            bool rc = false;
            if (url != null)
                rc = (url.EndsWith("/") || url.EndsWith(@"\"));
            return rc;
        }

        /// <summary>
        /// In Windows, it is impossible to determine by name alone if a URL is a directory or a file with no extension.  Thus
        /// the decision was made that all directory url's MUST end with a forward or backward slash (/ or \).  This removes any
        /// ambiguity.   This standard should be carried forward to all other implementation types (Aws, Azure, FTP, etc...)
        /// </summary>
        /// <param name="url">The full path to the object.</param>
        /// <returns></returns>
        public static bool IsFile(string url)
        {
            return !IsDirectory(url);
        }

        /// <summary>
        /// Gets a ZephyrFile implementation matching the URL type passed in.
        /// </summary>
        /// <param name="url">The Fullname or URL of the file.</param>
        /// <param name="clients">A collection of connection clients.</param>
        /// <returns>A ZephyrFile implementation.</returns>
        public static ZephyrFile GetZephyrFile(string url, Clients clients = null)
        {
            ZephyrFile file = null;
            UrlType type = GetUrlType(url);
            switch (type)
            {
                case UrlType.LocalFile:
                    file = new LocalZephyrFile(url);
                    break;
                case UrlType.NetworkFile:
                    file = new LocalZephyrFile(url);
                    break;
                case UrlType.AwsS3File:
                    file = new AwsS3ZephyrFile(clients?.aws, url);
                    break;
                case UrlType.AzureFile:
                    file = new AzureZephyrFile(clients?.azure, url);
                    break;
                default:
                    throw new Exception($"Url [{url}] Is Not A Known File Type.");
            }

            return file;
        }

        /// <summary>
        /// Gets a ZephyrFile implementation matching the URL type passed in.
        /// </summary>
        /// <param name="url">The Fullname or URL of the directory.</param>
        /// <param name="clients">A collection of connection clients.</param>
        /// <returns>A ZephyrFile implementation.</returns>
        public static ZephyrDirectory GetZephyrDirectory(string url, Clients clients = null)
        {
            ZephyrDirectory dir = null;
            UrlType type = GetUrlType(url);
            switch (type)
            {
                case UrlType.LocalDirectory:
                    dir = new LocalZephyrDirectory(url);
                    break;
                case UrlType.NetworkDirectory:
                    dir = new LocalZephyrDirectory(url);
                    break;
                case UrlType.AwsS3Directory:
                    dir = new AwsS3ZephyrDirectory(clients?.aws, url);
                    break;
                case UrlType.AzureDirectory:
                    dir = new AzureZephyrDirectory(clients?.azure, url);
                    break;
                default:
                    throw new Exception($"Url [{url}] Is Not A Known Directory Type.");
            }

            return dir;
        }

        /// <summary>
        /// Static method to create a ZephyrFile whose implementation if based on the Fullname / URL passed in.
        /// </summary>
        /// <param name="fileName">The Fullname or URL of the file.</param>
        /// <param name="clients">A collection of connection clients.</param>
        /// <param name="overwrite">Will overwrite the file if it already exists.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>A ZephyrFile instance.</returns>
        public static ZephyrFile CreateFile(string fileName, Clients clients = null, bool overwrite = true, bool verbose = true, String callbackLabel = null, Action<string, string> callback = null)
        {
            ZephyrFile file = ZephryUtilities.GetZephyrFile(fileName, clients);
            return file.Create(overwrite, verbose, callbackLabel, callback);
        }

        /// <summary>
        /// Static method to create a ZephyrDirectory whose implementation is based on the Fullname / URL passed in.
        /// </summary>
        /// <param name="dirName">The Fullname or URL of the directory.</param>
        /// <param name="clients">A collection of connection clients.</param>
        /// <param name="failIfExists">Throws an error if the directory already exists.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>A ZephyrDirectory instance.</returns>
        public static ZephyrDirectory CreateDirectory(string dirName, Clients clients = null, bool failIfExists = false, bool verbose = true, String callbackLabel = null, Action<string, string> callback = null)
        {
            ZephyrDirectory dir = ZephryUtilities.GetZephyrDirectory(dirName, clients);
            return dir.Create(failIfExists, verbose, callbackLabel, callback);
        }

        /// <summary>
        /// Static method to delete a ZephyrFile or ZephyrDirectory based on the Fullname / URL passed in.
        /// </summary>
        /// <param name="name">The Fullname or URL of the file or directory.</param>
        /// <param name="clients">A collection of connection clients.</param>
        /// <param name="recurse">Remove all objects in the directory as well.  If set to "false", directory must be empty or an exception will be thrown.</param>
        /// <param name="stopOnError">Stop deleting objects in the directory if an error is encountered.</param>
        /// <param name="verbose">Log each object that is deleted from the directory.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        public static void Delete(string name, Clients clients = null, bool recurse = true, bool stopOnError = true, bool verbose = true, String callbackLabel = null, Action<string, string> callback = null)
        {
            if (ZephryUtilities.IsDirectory(name))
            {
                ZephyrDirectory dir = ZephryUtilities.GetZephyrDirectory(name, clients);
                dir.Delete(recurse, stopOnError, verbose, callbackLabel, callback);
            }
            else
            {
                ZephyrFile file = ZephryUtilities.GetZephyrFile(name, clients);
                file.Delete(stopOnError, verbose, callbackLabel, callback);
            }
        }

        /// <summary>
        /// Static method to determine if a file or directory exists based on the Fullname/URL passed in.
        /// </summary>
        /// <param name="name">The Fullname or URL of the file or directory.</param>
        /// <param name="clients">A collection of connection clients.</param>
        /// <returns>Whether or now the file or directory already exists.</returns>
        public static bool Exists(string name, Clients clients = null)
        {
            if (ZephryUtilities.IsDirectory(name))
            {
                ZephyrDirectory dir = ZephryUtilities.GetZephyrDirectory(name, clients);
                return dir.Exists;
            }
            else
            {
                ZephyrFile file = ZephryUtilities.GetZephyrFile(name, clients);
                return file.Exists;
            }
        }

        /// <summary>
        /// Gets a ZephyrFile implementation matching the URL type passed in.
        /// </summary>
        /// <param name="url">The Fullname or URL of the directory.</param>
        /// <param name="clients">A collection of connection clients.</param>
        /// <returns>A ZephyrFile implementation.</returns>
        public static string PathCombine(params string[] paths)
        {
            ZephyrDirectory dir = null;
            UrlType type = GetUrlType(paths[0]);
            switch (type)
            {
                case UrlType.LocalDirectory:
                    dir = new LocalZephyrDirectory();
                    break;
                case UrlType.NetworkDirectory:
                    dir = new LocalZephyrDirectory();
                    break;
                case UrlType.AwsS3Directory:
                    dir = new AwsS3ZephyrDirectory(null);
                    break;
                case UrlType.AzureDirectory:
                    dir = new AzureZephyrDirectory(null);
                    break;
                default:
                    throw new Exception($"Url [{paths[0]}] Is Not A Known Directory Type.");
            }

            return dir.PathCombine(paths);
        }
        public static AwsClient InitAwsClient(RegionEndpoint region = null, string accessKey = null, string secretKey = null)
        {
            AwsClient client = null;

            bool hasAccessKey = (!String.IsNullOrWhiteSpace(accessKey));
            bool hasSecretKey = (!String.IsNullOrWhiteSpace(secretKey));
            bool hasRegion = (region != null);

            if (hasAccessKey && hasSecretKey)
            {
                if (hasRegion)
                    client = new AwsClient(accessKey, secretKey, region);
                else
                    client = new AwsClient(accessKey, secretKey);
            }
            else if (hasRegion)
                client = new AwsClient(region);
            else
                client = new AwsClient();     // Pull All Details From Environemnt Variables / Credentails Files

            return client;
        }

        public static AzureClient InitAzureClient(string connectionString)
        {
            AzureClient client = null;
            client = new AzureClient(connectionString);     
            return client;
        }

    }
}
