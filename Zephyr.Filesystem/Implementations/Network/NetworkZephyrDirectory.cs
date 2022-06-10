using System;

namespace Zephyr.Filesystem
{
    public class NetworkZephyrDirectory : LocalZephyrDirectory
    {
        /// <summary>
        /// Creates an empty instance of a LocalZephyrDirectory
        /// </summary>
        public NetworkZephyrDirectory() { }

        /// <summary>
        /// Creates an instance of a LocalZephyrDirectory representing the Fullname or URL passed in.
        /// </summary>
        /// <param name="fullPath">The Fullname or URL of the directory.</param>
        public NetworkZephyrDirectory(string fullPath)
        {
            FullName = fullPath;
        }

    }
}
