using WDE.EventAiEditor.Editor;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Editor
{
    [AutoRegister]
    [SingleInstance]
    public class MangosEditorFeatures : IEditorFeatures
    {
        public string Name => "Mangos";
    }
}