using System;
using WDE.Common.Managers;

namespace WDE.SmartScriptEditor.Models
{
    class SmartDataPresenter<T> : IDataDefinitionEditor where T: IDocument
    {
        private string name;
        private object[]? editorConstructorParam;

        public SmartDataPresenter(string name, object[]? editorConstructorParam)
        {
            this.name = name;
            this.editorConstructorParam = editorConstructorParam;
        }

        public string EditorName => name;
        // Generic classes with where new() declaration have to declare constructor without arguments,
        // but many ViewModel classes want to have objects like providers etc
        // so solution is simple, create it via Activator.CreateInstance which allows to pass array of constructor aruments
        public IDocument Editor => (T)Activator.CreateInstance(typeof(T), editorConstructorParam);
    }
}
