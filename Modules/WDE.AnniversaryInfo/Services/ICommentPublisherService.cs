using System.Threading.Tasks;
using Avalonia.Input;

namespace WDE.AnniversaryInfo.Services;

public enum CommentResult
{
    Success,
    TooManyComments,
    InvalidIpAddress,
    ServerProblem,
    InternetProblem
}

public interface ICommentPublisherService
{ 
    Task<CommentResult> Publish(string username, string text);
}