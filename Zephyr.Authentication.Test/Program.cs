using System;
using Zephyr.Authentication.OAuth;


namespace Zephyr.Authentication.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string tenantId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
            string clientId = "ffffffff-gggg-hhhh-iiii-jjjjjjjjjjjj";
            string clientSecret = "abcdefghijklmonpqrstuvwxyz12345678";
            string scope = "https://my.random.scope.com/.default";


            OAuthToken token = new OAuthToken();
            token.TenantId = tenantId;
            token.ClientId = clientId;
            token.ClientSecret = clientSecret;
            token.Scope = scope;

            token.GetToken();

            Console.WriteLine(token.AccessToken);

        }
    }
}
