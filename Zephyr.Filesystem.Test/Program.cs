using System;
using Zephyr.Filesystem;

namespace Zephyr.Filesystem.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string azureFile = @"https://mystorageaccount.blob.core.windows.net/sandbox/test.txt";
            string awsFile = @"s3://mybucket/test.txt";
            string localFile = @"/Users/john/Desktop/test.txt";
            string networkFile = @"\\server\MyShare$\test.txt";

            string azureConnectionString = @"DefaultEndpointsProtocol=https;AccountName=mystorageaccount;AccountKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx;EndpointSuffix=core.windows.net";

            // Initialize Clients
            Clients clients = new Clients();
            clients.AwsInitialize();
            clients.AzureInitialize(azureConnectionString);

            // Get Azure Blob or Share File
            AzureZephyrFile zFileAzure = new AzureZephyrFile(clients.azure, azureFile);
            Console.WriteLine($"FullName >> {zFileAzure.FullName}");
            Console.WriteLine($"Name     >> {zFileAzure.Name}");
            Console.WriteLine($"Exits    >> {zFileAzure.Exists}");
            Console.WriteLine(zFileAzure.ReadAllText(false));
            Console.WriteLine("===================");

            // Get AWS File
            AwsS3ZephyrFile zFileAws = new AwsS3ZephyrFile(clients.aws, awsFile);
            Console.WriteLine($"FullName >> {zFileAws.FullName}");
            Console.WriteLine($"Name     >> {zFileAws.Name}");
            Console.WriteLine($"Exits    >> {zFileAws.Exists}");
            Console.WriteLine(zFileAws.ReadAllText(false));
            Console.WriteLine("===================");

            // Get Local or Network File
            LocalZephyrFile zFileLocal = new LocalZephyrFile(localFile);
            Console.WriteLine($"FullName >> {zFileLocal.FullName}");
            Console.WriteLine($"Name     >> {zFileLocal.Name}");
            Console.WriteLine($"Exits    >> {zFileLocal.Exists}");
            Console.WriteLine(zFileLocal.ReadAllText(false));
            Console.WriteLine("===================");

            // Get Local or Network File
            NetworkZephyrFile zFileNetwork = new NetworkZephyrFile(networkFile);
            Console.WriteLine($"FullName >> {zFileNetwork.FullName}");
            Console.WriteLine($"Name     >> {zFileNetwork.Name}");
            Console.WriteLine($"Exits    >> {zFileNetwork.Exists}");
            Console.WriteLine(zFileNetwork.ReadAllText(false));
            Console.WriteLine("===================");


        }
    }
}
