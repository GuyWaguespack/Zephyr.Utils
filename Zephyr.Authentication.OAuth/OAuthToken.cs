using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Http;

namespace Zephyr.Authentication.OAuth
{
    public class OAuthToken
    {
        private const string v1DefaultAddress = "https://login.microsoftonline.com/{{TenantId}}/oauth2/token";
        private const string v2DefaultAddress = "https://login.microsoftonline.com/{{TenantId}}/oauth2/v2.0/token";

        [JsonProperty("access_token")]
        public string AccessToken { get; internal set; }

        [JsonProperty("token_type")]
        public string TokenType { get; internal set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; internal set; } = 0;

        [JsonProperty("refresh_token", NullValueHandling = NullValueHandling.Ignore)]
        public string RefreshToken { get; internal set; }

        [JsonProperty("ext_expires_in")]
        public int ExtExpiresIn { get; internal set; }

        [JsonProperty("expires_on", NullValueHandling = NullValueHandling.Ignore)]
        public string ExpiresOn { get; internal set; }

        [JsonProperty("not_before", NullValueHandling = NullValueHandling.Ignore)]
        public string NotBefore { get; internal set; }

        [JsonIgnore]
        public DateTime ExpireDate
        {
            get
            {
                return TokenCreated.AddSeconds(ExpiresIn);
            }
        }

        // Settable Properties
        [JsonIgnore]
        public string UrlTemplate { get; set; }

        [JsonIgnore]
        public int RefreshIfExpiresIn { get; set; } = 300;      // 5 Minutes

        [JsonIgnore]
        public string TenantId { get; set; }

        [JsonIgnore]
        public string ClientId { get; set; }

        [JsonIgnore]
        public string ClientSecret { get; set; }

        [JsonIgnore]
        public string Scope { get; set; }           // Use V2 address if included, else use V1 address.

        [JsonProperty("resource", NullValueHandling = NullValueHandling.Ignore)]
        public string Resource { get; set; }

        [JsonProperty("token_created", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime TokenCreated { get; set; } = new DateTime(1971, 11, 19, 4, 26, 0);       // Arbitrary Date Way In The Past

        private string GetBaseAddress()
        {
            string urlTemplate = v1DefaultAddress;
            if (!String.IsNullOrWhiteSpace(UrlTemplate))
                urlTemplate = UrlTemplate;
            else if (!String.IsNullOrWhiteSpace(Scope))
                urlTemplate = v2DefaultAddress;

            // Replace {{xxxxx}} Values In Template With Class Propeties
            Regex r = new Regex("{{(.*?)}}", RegexOptions.None);

            string baseAddress = urlTemplate;
            MatchCollection matches = r.Matches(urlTemplate);
            foreach (Match match in matches)
            {
                string property = match.Groups[1].Value?.Trim();
                string value = typeof(OAuthToken).GetProperty(property)?.GetValue(this, null)?.ToString();
                baseAddress = baseAddress.Replace(match.Value, value);
            }

            return baseAddress;
        }

        private bool RenewToken()
        {
            bool renew = false;

            DateTime now = DateTime.Now;
            DateTime refresh = ExpireDate.Subtract(new TimeSpan(0, 0, RefreshIfExpiresIn));

            if (now > refresh)
                renew = true;

            return renew;
        }

        public string GetToken(bool forceRenew = false)
        {
            if (RenewToken() || forceRenew)
            {
                HttpClient client = new HttpClient();
                string baseAddress = GetBaseAddress();

                // Build Form Variables
                Dictionary<string, string> form = new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", ClientId},
                    {"client_secret", ClientSecret}
                };

                if (!String.IsNullOrWhiteSpace(Scope))
                    form.Add("scope", Scope);
                else if (!String.IsNullOrWhiteSpace(Resource))
                    form.Add("resource", Resource);

                // Call Token Url
                HttpResponseMessage tokenResponse = client.PostAsync(baseAddress, new FormUrlEncodedContent(form)).Result;
                var jsonContent = tokenResponse.Content.ReadAsStringAsync().Result;
                OAuthToken tok = JsonConvert.DeserializeObject<OAuthToken>(jsonContent);

                // Set OAuth Token Properties
                this.AccessToken = tok.AccessToken;
                this.ExpiresIn = tok.ExpiresIn;
                this.TokenType = tok.TokenType;
                this.ExtExpiresIn = tok.ExtExpiresIn;
                this.RefreshToken = tok.RefreshToken;   // v1 token only
                this.ExpiresOn = tok.ExpiresOn;         // v1 token only
                this.NotBefore = tok.NotBefore;         // v1 token only
                if (!String.IsNullOrWhiteSpace(tok.Resource))
                    this.Resource = tok.Resource;

                TokenCreated = DateTime.Now;
            }

            return AccessToken;
        }
    }
}
