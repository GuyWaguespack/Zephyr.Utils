using System;
using System.Security.Cryptography;
using System.Xml;
using System.IO;
using System.Text;


namespace Zephyr.Crypto
{
    public class RSA
    {
        public static string Encrypt(string str, string publicKeyFile)
        {
            System.Security.Cryptography.RSA publicKey = Load(publicKeyFile);
            return Encrypt(str, publicKey);
        }

        public static string Encrypt(string str, System.Security.Cryptography.RSA publicKey)
        {
            byte[] strBytes = Encoding.UTF8.GetBytes(str);
            byte[] encBytes = publicKey.Encrypt(strBytes, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encBytes);
        }

        public static bool TryEncrypt(string str, string publicKey, out string value)
        {
            bool status = true;
            value = str;

            try { value = Encrypt(str, publicKey); }
            catch { status = false; }

            return status;
        }

        public static bool TryEncrypt(string str, System.Security.Cryptography.RSA publicKey, out string value)
        {
            bool status = true;
            value = str;

            try { value = Encrypt(str, publicKey); }
            catch { status = false; }

            return status;
        }

        public static string Decrypt(string str, string privateKeyfile)
        {
            System.Security.Cryptography.RSA privateKey = Load(privateKeyfile);
            return Decrypt(str, privateKey);
        }

        public static string Decrypt(string encryptedStr, System.Security.Cryptography.RSA privateKey)
        {
            byte[] encBytes = Convert.FromBase64String(encryptedStr);
            byte[] strBytes = privateKey.Decrypt(encBytes, RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(strBytes);
        }

        public static bool TryDecrypt(string str, string privateKey, out string value)
        {
            bool status = true;
            value = str;

            try { value = Decrypt(str, privateKey); }
            catch { status = false; }

            return status;
        }

        public static bool TryDecrypt(string str, System.Security.Cryptography.RSA privateKey, out string value)
        {
            bool status = true;
            value = str;

            try { value = Decrypt(str, privateKey); }
            catch { status = false; }

            return status;
        }

        public static System.Security.Cryptography.RSA GenerateKeys(int keySize = 0, string publicKeyFile = null, string privateKeyFile = null)
        {
            System.Security.Cryptography.RSA rsa;
            if (keySize > 0)
                rsa = System.Security.Cryptography.RSA.Create(keySize);
            else
                rsa = System.Security.Cryptography.RSA.Create();

            if (publicKeyFile != null)
                Save(rsa, false, publicKeyFile);

            if (privateKeyFile != null)
                Save(rsa, true, privateKeyFile);

            return rsa;
        }

        // https://dejanstojanovic.net/aspnet/2018/june/loading-rsa-key-pair-from-pem-files-in-net-core-with-c/
        public static System.Security.Cryptography.RSA Load(string xmlFilePath)
        {
            RSAParameters parameters = new RSAParameters();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(File.ReadAllText(xmlFilePath));

            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus":
                            if (!string.IsNullOrEmpty(node.InnerText))
                                parameters.Modulus = Convert.FromBase64String(node.InnerText);
                            break;

                        case "Exponent":
                            if (!string.IsNullOrEmpty(node.InnerText))
                                parameters.Exponent = Convert.FromBase64String(node.InnerText);
                            break;

                        case "P":
                            if (!string.IsNullOrEmpty(node.InnerText))
                                parameters.P = Convert.FromBase64String(node.InnerText);
                            break;

                        case "Q":
                            if (!string.IsNullOrEmpty(node.InnerText))
                                parameters.Q = Convert.FromBase64String(node.InnerText);
                            break;

                        case "DP":
                            if (!string.IsNullOrEmpty(node.InnerText))
                                parameters.DP = Convert.FromBase64String(node.InnerText);
                            break;

                        case "DQ":
                            if (!string.IsNullOrEmpty(node.InnerText))
                                parameters.DQ = Convert.FromBase64String(node.InnerText);
                            break;

                        case "InverseQ":
                            if (!string.IsNullOrEmpty(node.InnerText))
                                parameters.InverseQ = Convert.FromBase64String(node.InnerText);
                            break;

                        case "D":
                            if (!string.IsNullOrEmpty(node.InnerText))
                                parameters.D = Convert.FromBase64String(node.InnerText);
                            break;

                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            return System.Security.Cryptography.RSA.Create(parameters);
        }


        public static void Save(System.Security.Cryptography.RSA rsa, bool includePrivateParameters, string xmlFilePath)
        {
            RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

            StringBuilder sb = new StringBuilder();
            sb.Append("<RSAKeyValue>");

            if (parameters.Modulus != null)
                sb.Append($"<Modulus>{ Convert.ToBase64String(parameters.Modulus) }</Modulus>");

            if (parameters.Exponent != null)
                sb.Append($"<Exponent>{ Convert.ToBase64String(parameters.Exponent) }</Exponent>");

            if (parameters.P != null)
                sb.Append($"<P>{ Convert.ToBase64String(parameters.P) }</P>");

            if (parameters.Q != null)
                sb.Append($"<Q>{ Convert.ToBase64String(parameters.Q) }</Q>");

            if (parameters.DP != null)
                sb.Append($"<DP>{ Convert.ToBase64String(parameters.DP) }</DP>");

            if (parameters.DQ != null)
                sb.Append($"<DQ>{ Convert.ToBase64String(parameters.DQ) }</DQ>");

            if (parameters.InverseQ != null)
                sb.Append($"<InverseQ>{ Convert.ToBase64String(parameters.InverseQ) }</InverseQ>");

            if (parameters.D != null)
                sb.Append($"<D>{ Convert.ToBase64String(parameters.D) }</D>");

            sb.Append("</RSAKeyValue>");

            File.WriteAllText(xmlFilePath, sb.ToString());
        }
    }
}
