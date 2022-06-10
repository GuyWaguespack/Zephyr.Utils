using System;

using Azure.Core;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;

namespace Zephyr.Filesystem
{
    public class AzureClient
    {
        public string ConnectionString { get; set; }

        public BlobClient BlobClient { get; set; }
        public ShareClient ShareClient { get; set; }

        public AzureClient() { }

        public AzureClient(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}
