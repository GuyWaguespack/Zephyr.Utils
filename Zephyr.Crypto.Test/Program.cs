using System;
using System.IO;

using Zephyr.Crypto;

namespace Zephyr.Crypto.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string _plaintext = "MyPassword";

            // Test Rijndael Encryption
            string _passPhrase = "PassPhrase";
            string _saltValue = "SaltValue";
            string _iv = "1234567890123456";    // length must be block size (128) / 8 = 16

            string _encryptedText = Rijndael.Encrypt(_plaintext, _passPhrase, _saltValue, _iv);
            Console.WriteLine(_encryptedText);
            string _decryptedText = Rijndael.Decrypt(_encryptedText, _passPhrase, _saltValue, _iv);
            Console.WriteLine(_decryptedText);

            // Test RSA Encryption
            string publicKeyFile = @"../../../TestFiles/public.key";
            string pubPrivKeyFile = @"../../../TestFiles/pubPriv.key";

            RSA.GenerateKeys(2048, publicKeyFile, pubPrivKeyFile);

            string _encrypted = RSA.Encrypt(_plaintext, publicKeyFile);
            Console.WriteLine(_encrypted);
            string _decrypted = RSA.Decrypt(_encrypted, pubPrivKeyFile);
            Console.WriteLine(_decrypted);

            File.Delete(publicKeyFile);
            File.Delete(pubPrivKeyFile);
        }
    }
}
