using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia;
using Prism.Commands;
using TheEngine;
using TheMaths;
using WDE.Common.CoreVersion;
using WDE.Common.DBC;
using WDE.Common.Disposables;
using WDE.Common.Documents;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.MPQ;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Common.Windows;
using WDE.MapRenderer.StaticData;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.WorldMap.Models;
using WDE.WorldMap.Services;

namespace WDE.MapRenderer
{
    public class GameCameraViewModel : IMapItem
    {
        private readonly GameViewModel gameViewModel;
        public float X { get; private set; }
        public float Y { get; private set;}
        public float Z { get; private set;}
        public Rect VirtualBounds { get; set; }

        public GameCameraViewModel(GameViewModel gameViewModel)
        {
            this.gameViewModel = gameViewModel;
        }
        
        public void UpdatePosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
            gameViewModel.DoRender();
        }
    }
    
    [AutoRegister]
    public class GameViewModel : ObservableBase, IWizard, IMapContext<GameCameraViewModel>
    {
        public GameManager Game { get; }
        public event Action? RequestDispose;

        private MapViewModel? selectedMap;
        
        public MapViewModel? SelectedMap
        {
            get => selectedMap;
            set => SetProperty(ref selectedMap, value);
        }

        public IMapDataProvider MapData { get; }
        
        private IEnumerable<MapViewModel> maps;
        private bool isMapVisible;

        public IEnumerable<MapViewModel> Maps
        {
            get => maps;
            set => SetProperty(ref maps, value);
        }

        public GameViewModel(IMpqService mpqService, IDbcStore dbcStore, IMapDataProvider mapData, IGameView gameView)
        {
            MapData = mapData;
            Game = new GameManager(mpqService.Open(), gameView);
            AutoDispose(new ActionDisposable(() =>
            {
                RequestDispose?.Invoke();
            }));
            
            maps = dbcStore.MapDirectoryStore
                .Select(pair =>
                {
                    dbcStore.MapStore.TryGetValue(pair.Key, out var mapName);
                    return new MapViewModel(pair.Value,  mapName, (uint)pair.Key);
                }).ToList();
            SelectedMap = Maps.FirstOrDefault(k => k.MapPath == "Kalimdor") ?? Maps.FirstOrDefault();

            AutoDispose(this
                .ToObservable(i => i.SelectedMap)
                .Where(map => map != null)
                .SubscribeAction(map =>
                {
                    Game.SetMap(map.MapPath);
                }));

            ToggleMapVisibilityCommand = new DelegateCommand(() => IsMapVisible = !IsMapVisible);

            cameraViewModel = new GameCameraViewModel(this);
            Items.Add(cameraViewModel);
            
            Game.UpdateLoop.Register(d =>
            {
                var wowPos = Game.CameraManager.Position.ToWoWPosition();
                cameraViewModel.UpdatePosition(wowPos.X, wowPos.Y, wowPos.Z);
            });
        }

        public bool IsMapVisible
        {
            get => isMapVisible;
            set => SetProperty(ref isMapVisible, value);
        }
        public ICommand ToggleMapVisibilityCommand { get; }

        public ICommand Undo { get; set; } = AlwaysDisabledCommand.Command;
        public ICommand Redo { get; set; } = AlwaysDisabledCommand.Command;
        public IHistoryManager? History { get; set; }
        public bool IsModified { get; set; }
        public string Title => "Game view";
        public ICommand Copy { get; set; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; set; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; set; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; set; } = AlwaysDisabledCommand.Command;
        public IAsyncCommand? CloseCommand { get; set; }
        public bool CanClose => true;
        public void Center(double x, double y)
        {
        }

        public event Action? RequestRender;
        public event Action<double, double>? RequestCenter;
        public event Action<double, double, double, double>? RequestBoundsToView;
        public void Initialized()
        {
        }

        private GameCameraViewModel cameraViewModel;
        public ObservableCollection<GameCameraViewModel> Items { get; } = new();
        public IEnumerable<GameCameraViewModel> VisibleItems => Items;
        public GameCameraViewModel? SelectedItem { get; set; }
        
        public void Move(GameCameraViewModel item, double x, double y)
        {
            Game.CameraManager.Relocate(new Vector3((float)x, (float)y, 200).ToOpenGlPosition());
//            item.X = x;
  //          item.Y = y;
            RequestRender?.Invoke();
        }
        public void StartMove(){}
        public void StopMove()
        {
            //if (SelectedItem != null)
            //    TeleportPlayer(SelectedItem.Name, SelectedItem.X, SelectedItem.Y);
        }
        
        public void DoRender()
        {
            RequestRender?.Invoke();
        }
    }
    
    public class MapViewModel
    {
        public MapViewModel(string mapPath, string? mapName, uint id)
        {
            MapPath = mapPath;
            MapName = mapName;
            Id = id;
        }

        public string MapPath { get; }
        public string? MapName { get; }
        public uint Id { get; }

        public override string ToString()
        {
            return MapName == null ? $"/{MapPath} ({Id})" : $"{MapName} [/{MapPath}] ({Id})";
        }
    }
    
    
    [AutoRegister]
    [SingleInstance]
    public class GameProvider : IWizardProvider
    {
        private readonly Func<GameViewModel> creator;
        public string Name => "WoW World";
        public ImageUri Image => new ImageUri("Icons/document_minimap_big.png");
        public bool IsCompatibleWithCore(ICoreVersion core) => true;
    
        public GameProvider(Func<GameViewModel> creator)
        {
            this.creator = creator;
        }
        
        public Task<IWizard> Create()
        {
            return Task.FromResult<IWizard>(creator());
        }
    }
}