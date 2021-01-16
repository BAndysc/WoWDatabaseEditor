using WDE.SmartScriptEditor.Data;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public interface ISmartDataEditorModel
    {
        bool InsertOnSave { get; }
        SmartGenericJsonData GetSource();
        public bool IsSourceEmpty();
    }
}
