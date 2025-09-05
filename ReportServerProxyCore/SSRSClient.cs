namespace ReportServerProxyCore
{
    

    public class SSRSClient
    {
        private readonly System.Net.Http.HttpClient _httpClient;

        public SSRSClient()
        {
            var handler = new System.Net.Http.HttpClientHandler
            {
                AllowAutoRedirect = false // Important: so we can capture Set-Cookie on 302 redirects
            };

            _httpClient = new System.Net.Http.HttpClient(handler);
        }

        public async System.Threading.Tasks.Task<string[]> PostToSSRS(string ssrsLink, string ssrsData)
        {
            string url = $"{ssrsLink.TrimEnd('/')}/logon.aspx";

            System.Net.Http.FormUrlEncodedContent postData = new System.Net.Http.FormUrlEncodedContent(new[]
            {
                new System.Collections.Generic.KeyValuePair<string, string>("data", ssrsData),
                new System.Collections.Generic.KeyValuePair<string, string>("SSO", "FMS")
            });

            using System.Net.Http.HttpRequestMessage request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url)
            {
                Content = postData
            };

            using System.Net.Http.HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode && (int)response.StatusCode != 302)
            {
                throw new System.Exception($"SSRS login failed with status code {response.StatusCode}");
            }

            // Retrieve Set-Cookie headers
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                return System.Linq.Enumerable.ToArray(cookies);
            }

            return System.Array.Empty<string>();
        }
    }


}
