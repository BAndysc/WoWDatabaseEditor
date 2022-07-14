using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.DBC;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Module.Attributes;
using WDE.WorldMap.ViewModels;

namespace WDE.WorldMap.Services
{
    [SingleInstance]
    [AutoRegister]
    public class MapDataProvider : IMapDataProvider
    {
        private static readonly string RelativePath = "~/MapData/335/";

        private readonly IUserSettings userSettings;
        private readonly IMessageBoxService messageBoxService;
        private readonly IFileSystem fileSystem;
        private readonly Lazy<IMapDataDownloadService> mapDataDownloadService;
        private string? path;
        private IEnumerable<string> miniMaps = Enumerable.Empty<string>();
        private IReadOnlyList<MapViewModel> maps = Array.Empty<MapViewModel>();
        public event PropertyChangedEventHandler? PropertyChanged;

        public string? Path
        {
            get => path;
            internal set
            {
                path = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<MapViewModel> Maps
        {
            get => maps;
            internal set
            {
                maps = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<string> MiniMaps
        {
            get => miniMaps;
            internal set
            {
                miniMaps = value;
                OnPropertyChanged();
            }
        }

        public MapDataProvider(IUserSettings userSettings,
            IMessageBoxService messageBoxService,
            IFileSystem fileSystem,
            IDbcStore dbcStore,
            IEventAggregator eventAggregator,
            Lazy<IMapDataDownloadService> mapDataDownloadService)
        {
            this.userSettings = userSettings;
            this.messageBoxService = messageBoxService;
            this.fileSystem = fileSystem;
            this.mapDataDownloadService = mapDataDownloadService;
            Path = userSettings.Get<WorldMapSettings>().Path;
            UpdateMaps();
            OnDbcLoaded(dbcStore);
            eventAggregator.GetEvent<DbcLoadedEvent>().Subscribe(OnDbcLoaded);
        }

        private void OnDbcLoaded(IDbcStore dbcStore)
        {
            Maps = dbcStore.MapDirectoryStore.Select(pair => new MapViewModel((int)pair.Key, dbcStore.MapStore[(int)pair.Key], pair.Value)).ToList();
        }

        private void UpdateMaps()
        {
            if (!VerifyPath(Path))
                return;

            MiniMaps = Directory.GetDirectories(System.IO.Path.Join(Path, "x1")).Select(d => System.IO.Path.GetFileName(d)!)
                .ToList();
        }

        public async Task LoadMaps(Progress<(long, long?)>? progress = null)
        {
            if (VerifyPath(Path))
                return;

            if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetIcon(MessageBoxIcon.Information)
                .SetTitle("Data needs to be downloaded")
                .SetMainInstruction("Need to download additional map data")
                .SetContent(
                    "The editor has to download additional map data. It has to be downloaded only once (or when the maps are updated). It is about 500 MB to download.\n\nDo you want to download now?")
                .WithYesButton(true)
                .WithNoButton(false)
                .Build()))
                return;

            try
            {
                var zipFile = System.IO.Path.GetTempFileName();
                await mapDataDownloadService.Value.DownloadMaps(new FileInfo(zipFile), progress);
                
                var destDir = fileSystem.ResolvePhysicalPath(RelativePath);
                
                await Task.Run(() => ZipFile.ExtractToDirectory(zipFile, destDir.FullName, true)).ConfigureAwait(true);

                if (!VerifyPath(destDir.FullName))
                    throw new Exception("File downloaded, but corrupted?");

                await Task.Run(() => File.Delete(zipFile)).ConfigureAwait(true);
                
                UpdatePath(destDir.FullName);
            }
            catch (Exception e)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Fatal error")
                    .SetIcon(MessageBoxIcon.Error)
                    .SetMainInstruction("Can't download maps")
                    .SetContent(e.Message)
                    .WithOkButton(true)
                    .Build());
            }
        }

        private void UpdatePath(string path)
        {
            userSettings.Update(new WorldMapSettings(){Path = path});
            Path = path;
            UpdateMaps();
        }

        private bool VerifyPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (!Directory.Exists(path))
                return false;

            if (!Directory.Exists(System.IO.Path.Join(path, "x1")))
                return false;

            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [UniqueProvider]
    public interface IMapDataProvider : INotifyPropertyChanged
    {
        string? Path { get; }
        IReadOnlyList<MapViewModel> Maps { get; }
        IEnumerable<string> MiniMaps { get; }
        Task LoadMaps(Progress<(long, long?)>? progress);
    }
}