using System;
using WDE.Common.Managers;
using WDE.Common.Menu;

namespace WDE.Conditions.MenuItems
{
    public class ConditionsEditorMenuItem<T>: IMenuDocumentItem where T: IDocument
    {
        public string ItemName { get; }
        #nullable enable
        private readonly object[]? constructorParams;

        public ConditionsEditorMenuItem(string itemName, object[]? constructorParams)
        {
            ItemName = itemName;
            this.constructorParams = constructorParams;
        }

        public IDocument EditorDocument() => (T)Activator.CreateInstance(typeof(T), constructorParams)!;
    }
}