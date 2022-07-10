using System;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.Parameters.ViewModels;

public enum AnimTiers : byte
{
    Ground = 0,
    Swim = 1,
    Hover = 2,
    Fly = 3,
    Submerged = 4,
}

public enum StandStates : byte
{
    Stand = 0,
    Sit = 1,
    SitChair = 2,
    Sleep = 3,
    SitLowChair = 4,
    SitMediumChair = 5,
    SitHighChair = 6,
    Dead = 7,
    Kneel = 8,
    Submerged = 9,
}

public partial class UnitBytes1EditorViewModel : ObservableBase, IDialog
{
    public int DesiredWidth => 400;
    public int DesiredHeight => 310;
    public string Title => "Unit bytes 1";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;

    public UnitBytes1EditorViewModel(long bytes1)
    {
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
        Accept = new DelegateCommand(() => CloseOk?.Invoke());
        this.bytes1 = bytes1;
    }

    public StandStates StandState
    {
        get
        {
            var split = bytes1.SplitBytes1();
            return (StandStates)split.standState;
        }
        set => Bytes1 = (bytes1 & ~0xFF) | ((uint)value & 0xFF);
    }
    
    public AnimTiers AnimTier
    {
        get
        {
            var split = bytes1.SplitBytes1();
            return (AnimTiers)split.animTier;
        }
        set => Bytes1 = (bytes1 & ~0xFF000000) | (((uint)value & 0xFF) << 24);
    }
    
    public byte VisibilityState
    {
        get
        {
            var split = bytes1.SplitBytes1();
            return split.visFlags;
        }
        set => Bytes1 = (bytes1 & ~0xFF0000) | (((uint)value & 0xFF) << 16);
    }
    
    public byte PetTalents
    {
        get
        {
            var split = bytes1.SplitBytes1();
            return split.petTalents;
        }
        set => Bytes1 = (bytes1 & ~0xFF00) | (((uint)value & 0xFF) << 8);
    }
    
    private long bytes1;
    public long Bytes1
    {
        get => bytes1;
        set
        {
            bytes1 = value;
            RaisePropertyChanged(nameof(PetTalents));
            RaisePropertyChanged(nameof(VisibilityState));
            RaisePropertyChanged(nameof(StandState));
            RaisePropertyChanged(nameof(AnimTier));
            RaisePropertyChanged(nameof(Bytes1));
        }
    }
}