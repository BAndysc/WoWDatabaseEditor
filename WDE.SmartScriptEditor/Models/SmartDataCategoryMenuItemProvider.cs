using System;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.Providers;

namespace WDE.SmartScriptEditor.Models
{
    class SmartDataCategoryMenuItemProvider<T> : IMenuDocumentItem where T: IDocument
    {
        public string ItemName { get; }
        #nullable enable
        private object[]? constructorParam;
        
        public SmartDataCategoryMenuItemProvider(string name, object[]? editorConstructorParam)
        {
            ItemName = name;
            constructorParam = editorConstructorParam;
        }
        
        public IDocument EditorDocument() => (T)Activator.CreateInstance(typeof(T), constructorParam);
    }
}
