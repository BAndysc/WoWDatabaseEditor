using System;
using WDE.Common.Sessions;

namespace WDE.Sessions.Sessions
{
    internal class EditorSessionStub : IEditorSessionStub
    {
        public EditorSessionStub(string name, string fileName, DateTime? dateTime = null, DateTime? lastModified = null)
        {
            Name = name;
            FileName = fileName;
            Created = dateTime ?? DateTime.Now;
            LastModified = lastModified ?? DateTime.Now;
        }

        public string Name { get; set; }
        public string FileName { get; set; }
        public DateTime Created { get; }
        public DateTime LastModified { get; set; }
    }
}