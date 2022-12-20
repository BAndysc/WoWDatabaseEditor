using System;
using WDE.Common.History;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.PacketViewer.ViewModels
{
    public class SelectedPacketHistory : HistoryHandler, IDisposable
    {
        private IDisposable disposable;
        
        public SelectedPacketHistory(PacketDocumentViewModel vm)
        {
            disposable = vm.ToObservableValue().Where(e => e.PropertyName == nameof(PacketDocumentViewModel.SelectedPacket))
                .SubscribeAction(args =>
                {
                    if (args.New is null)
                        return;
                    
                    var @new = args.New as PacketViewModel;
                    var @old = args.Old as PacketViewModel;
                    PushAction(new AnonymousHistoryAction("Selected packet " + @new!.Id,
                        () => vm.SelectedPacket = @old,
                        () => vm.SelectedPacket = @new));
                });
        }

        public void Dispose()
        {
            disposable.Dispose();
        }
    }
}