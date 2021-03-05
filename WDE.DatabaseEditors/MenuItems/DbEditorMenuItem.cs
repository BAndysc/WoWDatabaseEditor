using System;
using WDE.Common.Managers;
using WDE.Common.Menu;

namespace WDE.DatabaseEditors.MenuItems
{
    public class DbEditorMenuItem<T> : IMenuDocumentItem where T: IDocument
    {
        public string ItemName { get; }
        private readonly object[]? constructorParams;

        public DbEditorMenuItem(string itemName, object[]? constructorParams)
        {
            ItemName = itemName;
            this.constructorParams = constructorParams;
        }

        public IDocument EditorDocument() => (T)Activator.CreateInstance(typeof(T), constructorParams);
    }
}