using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media;
using Prism.Mvvm;
using WDE.Common.Utils;

namespace WDE.AnniversaryInfo.ViewModels;

public class ImageContentItem : BindableBase, IContentItem
{
    public ImageContentItem(IImage image)
    {
        Image = image;
    }

    public ImageContentItem() {}

    public ImageContentItem(string url)
    {
        AsyncLoad(url, new HttpClient()).ListenErrors();
    }

    public async Task AsyncLoad(string url, HttpClient client)
    {
        var result = await client.GetAsync(url);
        var stream = await result.Content.ReadAsStreamAsync();
        SetImage(new Avalonia.Media.Imaging.Bitmap(stream));
    }
    
    public void SetImage(IImage image)
    {
        Image = image;
        RaisePropertyChanged(nameof(Image));
    }
    
    public IImage? Image { get; private set; }
}