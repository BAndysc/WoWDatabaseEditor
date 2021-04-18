using System;
using System.Net.Http.Headers;

namespace WDE.RemoteSOAP.Helpers
{
    public static class HttpBasicAuthorizer
    {
        public static AuthenticationHeaderValue Encode(string userName, string password)
        {
            return new(
                "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{userName}:{password}")));
        }
    }
}