using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Sessions.Sessions
{
    [AutoRegister]
    [SingleInstance]
    public class SessionService : ObservableCollection<IEditorSessionStub>, ISessionService
    {
        private readonly ISolutionItemSqlGeneratorRegistry queryGenerator;
        private readonly IFileSystem fs;
        private readonly IUserSettings userSettings;
        private readonly SessionSerializerDeserializer serializerDeserializer;
        private ObservableCollection<IEditorSessionStub> deleted = new();
        public ObservableCollection<IEditorSessionStub> DeletedSessions => deleted;

        public SessionService(ISolutionItemSqlGeneratorRegistry queryGenerator,
            IFileSystem fs, IUserSettings userSettings, IEventAggregator eventAggregator,
            SessionSerializerDeserializer serializerDeserializer)
        {
            this.queryGenerator = queryGenerator;
            this.fs = fs;
            this.userSettings = userSettings;
            this.serializerDeserializer = serializerDeserializer;

            var saved = userSettings.Get<SessionData>();
            deleteOnSave = saved.DeleteOnSave;
            EditorSessionStub? currentSessionToLoad = null;
            if (saved.Sessions != null)
            {
                foreach (var sess in saved.Sessions)
                {
                    var stub = new EditorSessionStub(sess.SessionName, sess.FileName, sess.CreatedTime, sess.LastModifiedTime);
                    if (!fs.Exists(GetPath(stub)))
                        continue;
                    Add(stub);
                    if (sess.FileName == saved.CurrentSessionFile)
                        currentSessionToLoad = stub;
                }
            }

            if (saved.DeletedSessions != null)
            {
                foreach (var sess in saved.DeletedSessions)
                {
                    var stub = new EditorSessionStub(sess.SessionName, sess.FileName, sess.CreatedTime,
                        sess.LastModifiedTime);
                    if ((DateTime.Now - sess.LastModifiedTime) > TimeSpan.FromDays(14))
                    {
                        var file = fs.ResolvePhysicalPath(GetPath(stub));
                        if (file.Exists)
                            file.Delete();
                        continue;
                    }
                    
                    deleted.Add(stub);
                }   
            }

            if (currentSessionToLoad != null)
            {
                eventAggregator.GetEvent<AllModulesLoaded>()
                    .Subscribe(() =>
                    {
                        SafeOpen(currentSessionToLoad);
                    }, true);
            }
        }

        public ISolutionItem? Find(ISolutionItem item)
        {
            return CurrentSession?.Find(item);
        }

        public string GetPath(IEditorSessionStub stub)
        {
            return Path.Join("~/sessions", stub.FileName);
        }

        private void SafeOpen(IEditorSessionStub? stub)
        {
            try
            {
                Open(stub);
            }
            catch (Exception e)
            {
                LOG.LogError(e, "Error while opening session");
            }
        }
        
        public void Open(IEditorSessionStub? stub)
        {
            if (stub == null)
            {
                Close();
                return;
            }

            var path = GetPath(stub);
            if (!fs.Exists(path))
                return;
            
            var lines = fs.ReadLines(path);
            try
            {
                CurrentSession = serializerDeserializer.Deserialize(lines, stub);
            }
            catch (Exception)
            {
                CurrentSession = null;
                throw;
            }
            finally
            {
                Save();
            }
        }

        private void Close()
        {
            CurrentSession = null;
        }
        
        public void Save()
        {
            userSettings.Update(new SessionData()
            {
                DeleteOnSave = deleteOnSave,
                CurrentSessionFile = CurrentSession?.FileName,
                Sessions = this.Select(s => new SessionData.Session(){FileName = s.FileName, SessionName = s.Name, CreatedTime = s.Created, LastModifiedTime = s.LastModified}).ToList(),
                DeletedSessions = deleted.Select(s => new SessionData.Session(){FileName = s.FileName, SessionName = s.Name, CreatedTime = s.Created, LastModifiedTime = s.LastModified}).ToList()
            });
            
            if (CurrentSession == null)
                return;
            
            fs.WriteAllText(GetPath(CurrentSession), serializerDeserializer.Serialize(CurrentSession));
        }

        public async Task UpdateQuery(ISolutionItemDocument document)
        {
            if (CurrentSession == null)
                return;
            
            if (IsPaused)
                return;

            if (document is ISplitSolutionItemQueryGenerator solutionItemDocument)
                await UpdateQuery(await solutionItemDocument.GenerateSplitQuery());
            else
                await UpdateQuery(new List<(ISolutionItem, IQuery)>(){(document.SolutionItem, await document.GenerateQuery())});
        }

        public async Task UpdateQuery(ISolutionItem solutionItem)
        {
            if (CurrentSession == null)
                return;
            
            if (IsPaused)
                return;
            
            var query = await queryGenerator.GenerateSplitSql(solutionItem);
            await UpdateQuery(query);
        }

        private async Task UpdateQuery(IList<(ISolutionItem, IQuery)> query)
        {
            if (CurrentSession == null)
                return;
            
            if (IsPaused)
                return;
            
            foreach (var q in query)
                CurrentSession.Insert(q.Item1, q.Item2.QueryString);

            CurrentSession.LastModified = DateTime.Now;
            this.FirstOrDefault(t => t.FileName == CurrentSession.FileName).Do(t => t.LastModified = DateTime.Now);

            Save();
        }
        
        public void BeginSession(string sessionName)
        {
            var sess = new EditorSession(sessionName, GenerateSessionFileName(sessionName), DateTime.Now, DateTime.Now);
            Add(sess);
            CurrentSession = sess;
            Save();
        }

        private string GenerateSessionFileName(string sessionName)
        {
            var baseSessionName = sessionName.ToAlphanumerical();
            sessionName = baseSessionName.Replace(" ", "_") + ".sql";
            int index = 1;
            while (fs.Exists(Path.Join("~/sessions", sessionName)))
            {
                sessionName = $"{baseSessionName.Replace(" ", "_")}_{index:00}.sql";
                index++;
            }

            return sessionName;
        }

        public string? GenerateCurrentQuery()
        {
            if (CurrentSession == null)
                return null;
            
            StringBuilder sb = new();
            foreach (var pair in CurrentSession)
            {
                sb.AppendLine(pair.Item2);
                if (!string.IsNullOrEmpty(pair.Item2) && pair.Item2[^1] != '\n')
                    sb.AppendLine();
            }

            return sb.ToString();
        }
        
        public void Finalize(string fileName)
        {
            if (CurrentSession == null)
                return;
            
            File.WriteAllText(fileName, GenerateCurrentQuery());
        }

        public void ForgetCurrent()
        {
            if (CurrentSession == null)
                return;

            ForgetSession(CurrentSession);
        }

        public void RemoveItem(ISolutionItem item)
        {
            if (CurrentSession == null)
                return;

            CurrentSession.Insert(item, "");

            Save();
        }

        public void RestoreSession(IEditorSessionStub stub)
        {
            var restoredStub = deleted.RemoveFirstIf(i => i.FileName == stub.FileName);
            if (restoredStub != null)
            {
                Add(restoredStub);
                if (!IsOpened)
                    Open(restoredStub);
            }
        }

        public void ForgetSession(IEditorSessionStub stub)
        {
            var deletedSessionStub = this.RemoveFirstIf(i => i.FileName == stub.FileName);
            if (deletedSessionStub != null)
                deleted.Add(deletedSessionStub);

            if ((CurrentSession == null || CurrentSession.FileName == stub.FileName) && Count > 0)
                Open(this[0]);
            else
            {
                CurrentSession = null;
                Save();
            }
        }

        private bool isPaused;

        public bool? DeleteOnSave
        {
            get => deleteOnSave;
            set
            {
                deleteOnSave = value;
                Save();
            }
        }

        public bool IsPaused
        {
            get => isPaused;
            set
            {
                isPaused = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsPaused)));
            }
        }

        public bool IsNonEmpty => CurrentSession?.Any() ?? false;
        public bool IsOpened => CurrentSession != null;
        
        private IEditorSession? session;
        private bool? deleteOnSave;

        public IEditorSession? CurrentSession
        {
            get => session;
            set
            {
                if (session != null)
                    session.CollectionChanged -= SessionOnCollectionChanged;
                session = value;
                if (session != null)
                    session.CollectionChanged += SessionOnCollectionChanged;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentSession)));
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsOpened)));
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsNonEmpty)));
            }
        }

        private void SessionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsNonEmpty)));
        }
    }
}