namespace WDE.MapRenderer
{
//     [AutoRegister]
//     [SingleInstance]
//     public class GameProvider : IWizardProvider
//     {
//         private readonly Func<GameViewModel> creator;
//         private readonly IMessageBoxService messageBoxService;
//         public string Name => "WoW World";
//         public ImageUri Image => new ImageUri("Icons/document_minimap_big.png");
//         public bool IsCompatibleWithCore(ICoreVersion core) => true;
//     
//         public GameProvider(Func<GameViewModel> creator, IMessageBoxService messageBoxService)
//         {
//             this.creator = creator;
//             this.messageBoxService = messageBoxService;
//         }
//         
//         public async Task<IWizard> Create()
//         {
//             if (!GlobalApplication.Supports3D)
//             {
//                 await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
//                     .SetTitle("Unable to initialize 3D")
//                     .SetMainInstruction("For some reason your PC doesn't support OpenGL")
//                     .SetContent("Cannot open 3D map")
//                     .Build());   
//             }
//             return creator();
//         }
//     }
}