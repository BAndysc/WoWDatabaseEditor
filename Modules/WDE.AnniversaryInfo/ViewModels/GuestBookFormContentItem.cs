using Prism.Mvvm;
using WDE.AnniversaryInfo.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.AnniversaryInfo.ViewModels;

public class GuestBookFormContentItem : ObservableBase, IContentItem
{
    private string text = "";
    private string username = "";
    private bool enabled = true;

    public string Text
    {
        get => text;
        set => SetProperty(ref text, value);
    }

    public string Username
    {
        get => username;
        set => SetProperty(ref username, value);
    }

    public bool Enabled
    {
        get => enabled = true;
        set => SetProperty(ref enabled, value);
    }

    public AsyncAutoCommand PublishCommand { get; }

    public GuestBookFormContentItem(ICommentPublisherService publisher,
        IMessageBoxService messageBoxService)
    {
        PublishCommand = new AsyncAutoCommand(async () =>
        {
            try
            {
                Enabled = false;
                var result = await publisher.Publish(username, text);
                if (result == CommentResult.Success)
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Success")
                        .SetMainInstruction("Comment sent successfully!")
                        .SetContent("Thank you for your comment! It will be published after moderation ASAP.")
                        .WithOkButton(true)
                        .Build());
                    Username = "";
                    Text = "";
                }
                else if (result == CommentResult.TooManyComments)
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Fail")
                        .SetMainInstruction("Too many requests!")
                        .SetContent("You are publishing too many comments, slow down.")
                        .WithOkButton(true)
                        .Build());
                }
                else
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Fail")
                        .SetMainInstruction("Couldn't send the comment")
                        .SetContent("Sadly, there is some problem connecting to the server. Please try again later.")
                        .WithOkButton(true)
                        .Build());
                }
            }
            finally
            {
                Enabled = true;
            }
        }, () => !string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Username));

        On(() => Username, _ => PublishCommand.RaiseCanExecuteChanged());
        On(() => Text, _ => PublishCommand.RaiseCanExecuteChanged());
    }
}