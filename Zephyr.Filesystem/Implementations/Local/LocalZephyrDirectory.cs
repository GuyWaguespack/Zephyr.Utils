using System;
using System.Collections.Generic;
using System.IO;

namespace Zephyr.Filesystem
{
    /// <summary>
    /// The implementation of ZephyrDirectory using Local-based Filesystems.
    /// </summary>
    public class LocalZephyrDirectory : ZephyrDirectory
    {
        private DirectoryInfo dirInfo = null;

        /// <summary>
        /// Creates an empty instance of a LocalZephyrDirectory
        /// </summary>
        public LocalZephyrDirectory() { }

        /// <summary>
        /// Creates an instance of a LocalZephyrDirectory representing the Fullname or URL passed in.
        /// </summary>
        /// <param name="fullPath">The Fullname or URL of the directory.</param>
        public LocalZephyrDirectory(string fullPath)
        {
            FullName = fullPath;
        }

        /// <summary>
        /// The Fullname or URL to the Local Directory.
        /// </summary>
        public override String FullName
        {
            // TODO : Why did you add the double \\ Here?
//            get { return $"{dirInfo.FullName}\\"; }
            get { return $"{dirInfo.FullName}"; }
            set { dirInfo = new DirectoryInfo(value); }
        }

        /// <summary>
        /// The name of the Local Directory.
        /// </summary>
        public override String Name { get { return dirInfo?.Name; } }

        /// <summary>
        /// The parent of the Local Directory.
        /// </summary>
        public override String Parent { get { return $"{dirInfo?.Parent?.FullName}\\"; } }


        /// <summary>
        /// The root or protocol for the Local Directory (Drive Letter or Network Server/Share)
        /// </summary>
        public override String Root { get { return dirInfo?.Root?.FullName; } }

        /// <summary>
        /// Implementation of the ZephyrDirectory Exists method using Local FileSystem.
        /// </summary>
        public override bool Exists { get { return Directory.Exists(FullName); } }

        /// <summary>
        /// Implementation of the ZephyrDirectory Separaptor method using Local FileSystem.
        /// </summary>
        public override char Separator { get { return Path.DirectorySeparatorChar; } }


        /// <summary>
        /// Implementation of the ZephyrDirectory Create method using Local FileSystem.
        /// </summary>
        /// <param name="failIfExists">Throws an error if the directory already exists.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>A LocalZephyrDictionary Instance.</returns>
        public override ZephyrDirectory Create(bool failIfExists = false, bool verbose = true, String callbackLabel = null, Action<string, string> callback = null)
        {
            if (!Directory.Exists(FullName))
                Directory.CreateDirectory(FullName);
            else if (failIfExists)
                throw new Exception($"Directory [{FullName}] Already Exists.");
            if (verbose)
                Logger.Log($"Directory [{FullName}] Was Created.", callbackLabel, callback);
            return this;
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory CreateFile method using Local FileSystem.
        /// </summary>
        /// <param name="fullName">Full name or URL of the file to be created.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>A LocalZephyrFile implementation.</returns>
        public override ZephyrFile CreateFile(string fullName, bool verbose = true, String callbackLabel = null, Action<string, string> callback = null)
        {
            return new LocalZephyrFile(fullName);
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory CreateFile method using Local FileSystem.
        /// </summary>
        /// <param name="fullName">Full name or URL of the directory to be created.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>A LocalZephyrDirectory implementation.</returns>
        public override ZephyrDirectory CreateDirectory(string fullName, bool verbose = true, String callbackLabel = null, Action<string, string> callback = null)
        {
            return new LocalZephyrDirectory(fullName);
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory Delete method using Local FileSystem.
        /// </summary>
        /// <param name="recurse">Remove all objects in the directory as well.  If set to "false", directory must be empty or an exception will be thrown.</param>
        /// <param name="stopOnError">Stop deleting objects in the directory if an error is encountered.</param>
        /// <param name="verbose">Log each object that is deleted from the directory.</param>
        /// <param name="callbackLabel">Optional "label" to be passed into the callback method.</param>
        /// <param name="callback">Optional method that is called for logging purposes.</param>
        /// <returns>"true" if completely successful, "false" if any part failed</returns>
        public override bool Delete(bool recurse = true, bool stopOnError = true, bool verbose = true, String callbackLabel = null, Action<string, string> callback = null)
        {
            bool success = true;
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(FullName);

                if (dirInfo.Exists)
                {
                    if (!recurse)
                    {
                        int dirs = dirInfo.GetDirectories().Length;
                        int files = dirInfo.GetFiles().Length;
                        if (dirs > 0 || files > 0)
                            throw new Exception($"Directory [{FullName}] is not empty.");
                    }
                    dirInfo.Delete(recurse);
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

            dirInfo = null;     // TODO : Why did I do this?
            return success;
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory GetDirectories method using Local FileSystem.
        /// </summary>
        /// <returns>An enumeration of LocalZephyrDirectory objects.</returns>
        public override IEnumerable<ZephyrDirectory> GetDirectories()
        {
            String[] directories = Directory.GetDirectories(FullName);

            List<ZephyrDirectory> synDirs = new List<ZephyrDirectory>();
            foreach (string dir in directories)
            {
                ZephyrDirectory synDir = new LocalZephyrDirectory(Path.Combine(FullName, dir));
                synDirs.Add(synDir);
            }

            return synDirs;
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory GetDirectories method using Local FileSystem.
        /// </summary>
        /// <returns>An enumeration of LocalZephyrFile objects.</returns>
        public override IEnumerable<ZephyrFile> GetFiles()
        {
            String[] files = Directory.GetFiles(FullName);
            List<ZephyrFile> synFiles = new List<ZephyrFile>();
            foreach (string file in files)
            {
                ZephyrFile synFile = new LocalZephyrFile(Path.Combine(FullName, file));
                synFiles.Add(synFile);
            }

            return synFiles;
        }

        /// <summary>
        /// Implementation of the ZephyrDirectory PathCombine method using Local FileSystem.
        /// </summary>
        /// <param name="paths">An array of strings to combine.</param>
        /// <returns>The combined paths.</returns>
        public override string PathCombine(params string[] paths)
        {
            List<string> fixedPaths = new List<string>();
            foreach (string path in paths)
            {
                if (path == "/" || path == "\\")
                    fixedPaths.Add($"_{path}");     // Windows doesn't allow blank directory names, replace with underscore.
                else
                    fixedPaths.Add(path);
            }

            return Path.Combine(fixedPaths.ToArray());
        }
    }
}
