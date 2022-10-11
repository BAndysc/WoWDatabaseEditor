using System;
using System.Collections.Generic;
using WDE.Common.Services;

namespace WDE.Sessions.Sessions
{
    public struct SessionData : ISettings
    {
        public bool? DeleteOnSave { get; set; }
        public string? CurrentSessionFile { get; set; }
        public List<Session>? Sessions { get; set; }
        public List<Session>? DeletedSessions { get; set; }

        public struct Session
        {
            public string SessionName { get; set; }
            public string FileName { get; set; }
            public DateTime CreatedTime { get; set; }
            public DateTime LastModifiedTime { get; set; }
        }
    }
}