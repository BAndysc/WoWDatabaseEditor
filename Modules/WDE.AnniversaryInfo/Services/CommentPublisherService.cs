using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.AnniversaryInfo.Services;

[AutoRegister]
[SingleInstance]
public class CommentPublisherService : ICommentPublisherService
{
    private readonly IHttpClientFactory httpClientFactory;
    private string? updateServer;

    public CommentPublisherService(IHttpClientFactory httpClientFactory, IApplicationReleaseConfiguration configuration)
    {
        updateServer = configuration.GetString("UPDATE_SERVER") ?? "http://localhost";
        this.httpClientFactory = httpClientFactory;
    }
    
    public async Task<CommentResult> Publish(string username, string text)
    {
        var http = httpClientFactory.Factory();
        
        var json = JsonConvert.SerializeObject(new Request(){Username = username, Text = text});
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var result = await http.PostAsync($"{updateServer}/Comments/Add", content);
            if (result.StatusCode == HttpStatusCode.Unauthorized)
                return CommentResult.TooManyComments;
        
            if (result.StatusCode == HttpStatusCode.Forbidden)
                return CommentResult.InvalidIpAddress;

            if (result.StatusCode == HttpStatusCode.OK)
                return CommentResult.Success;

            return CommentResult.ServerProblem;
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return CommentResult.InternetProblem;
        }
    }

    private struct Request
    {
        public string Username { get; set; }
        public string Text { get; set; }
    }

    public KeyboardNavigationMode Task<CommentResult1>()
    {
        throw new NotImplementedException();
    }
}