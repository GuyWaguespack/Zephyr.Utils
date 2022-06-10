using System;
using Zephyr.Filesystem;
using System.Linq;

namespace Zephyr.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            // Initialize Clients
            Clients clients = new Clients();
            clients.AwsInitialize();
            clients.AzureInitialize();

            // *** Test Zephyr Files ***
            //string filename = @"/Users/guy/Desktop/test.txt";
            //string filename = @"s3://guywaguespack/test.txt";
            string filename = @"https://guywaguespack.blob.core.windows.net/sandbox/test.txt";
            //string filename = @"https://guywaguespack.file.core.windows.net/sandbox/test.txt";

            //ZephyrFile file = Utilities.CreateFile(filename, clients, verbose:false);
            //ZephyrFile file = Utilities.GetZephyrFile(filename, clients);

            AzureZephyrFile zFile = new AzureZephyrFile(clients.azure, filename);

            Console.WriteLine($"FullName >> {zFile.FullName}");
            Console.WriteLine($"Name     >> {zFile.Name}");
            Console.WriteLine($"Exits    >> {zFile.Exists}");

            //zFile.Open(AccessType.Read);
            //zFile.WriteAllText("Hello World");

            //ZephyrFile file = zFile.CreateFile(filename);

            //file.Open(AccessType.Read);
            //file.WriteAllText("Hello World", verbose:false);
            //file.Delete();


            // *** Test Zephyr Directories ***
            //string dirname = @"/Users/guy/Desktop/dir1/dir2/";
            //string dirname = @"s3://guywaguespack/dir1/new/";
            //string dirname = @"https://guywaguespack.blob.core.windows.net/sandbox/dir1/new/";
            //string dirname = @"https://guywaguespack.file.core.windows.net/sandbox/dir1/new/";

            //WindowsZephyrDirectory zDir = new WindowsZephyrDirectory(dirname);
            //AwsS3ZephyrDirectory zDir = new AwsS3ZephyrDirectory(clients.aws, dirname);
            //AzureZephyrDirectory zDir = new AzureZephyrDirectory(clients.azure, dirname);

            //Console.WriteLine($"FullName >> {zDir.FullName}");
            //Console.WriteLine($"Name     >> {zDir.Name}");
            //Console.WriteLine($"Parent   >> {zDir.Parent}");
            //Console.WriteLine($"Root     >> {zDir.Root}");
            //Console.WriteLine($"Exits    >> {zDir.Exists}");

            //zDir.Create();
            //zDir.Delete();

            //Console.WriteLine();
            //Console.WriteLine($"-------- Files ---------");
            //foreach (ZephyrFile file in zDir.GetFiles())
            //    Console.WriteLine($"{file.FullName}");

            //Console.WriteLine();
            //Console.WriteLine($"----- Directories ------");
            //foreach (ZephyrDirectory dir in zDir.GetDirectories())
            //    Console.WriteLine($"{dir.FullName}");

            Console.WriteLine("\nFinished");
        }
    }
}
