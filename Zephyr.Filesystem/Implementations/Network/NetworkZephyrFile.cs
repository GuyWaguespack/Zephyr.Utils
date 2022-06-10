using System;

namespace Zephyr.Filesystem
{
    public class NetworkZephyrFile : LocalZephyrFile
    {
        /// <summary>
        /// Creates an empty LocalZephyrFile object.
        /// </summary>
        public NetworkZephyrFile() : base() { }

        /// <summary>
        /// Creates an instance of LocalZephyrFile representing the Fullname / URL passed in.
        /// </summary>
        /// <param name="fullName">The Fullname or URL of the file.</param>
        public NetworkZephyrFile(string fullName) : base(fullName) { }
    }
}
