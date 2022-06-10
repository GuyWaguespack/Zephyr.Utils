using System;
using System.Collections.Generic;
using System.IO;

using Zephyr.Crypto;

using Zephyr.Directory.Ldap;

namespace Zephyr.Directory.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string server = @"my.ldap.server";
            int port = 389;
            bool useSSL = false;
            int maxResults = 100;

            string username = @"SANDBOX\username";
            string password = @"MyPassword!";

            string searchFilter = @"(sn=Smith)";


            LdapServer ldap = new LdapServer(server, port, useSSL, maxResults);
            ldap.Bind(username, password);

            LdapResponse response = ldap.Search(null, searchFilter);

            Console.WriteLine(JsonTools.Serialize(response, true));

        }
    }
}
