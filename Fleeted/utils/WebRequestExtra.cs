using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fleeted.utils;

public static class WebRequestExtra
{
    public static async Task<string> GetBodyFromWebRequest(string url)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url),
        };
        using (var response = await client.SendAsync(request))
        {
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            return body;
        }
    }
}