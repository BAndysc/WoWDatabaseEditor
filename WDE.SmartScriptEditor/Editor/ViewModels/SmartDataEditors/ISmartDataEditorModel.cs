using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels.SmartDataEditors
{
    public interface ISmartDataEditorModel
    {
        bool InsertOnSave { get; }
        SmartGenericJsonData GetSource();
        public bool IsSourceEmpty();
    }
}
