using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.Parameters.Parameters;

namespace WDE.Parameters.ViewModels;

public enum SheathStates
{
    Unarmed = 0,
    Melee   = 1,
    Ranged  = 2
}

public partial class UnitBytes2EditorViewModel : ObservableBase, IDialog
{
    public int DesiredWidth => 400;
    public int DesiredHeight => 310;
    public string Title => "Unit bytes 2";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }
    public event Action? CloseCancel;
    public event Action? CloseOk;

    public UnitBytes2EditorViewModel(UnitBytes2Parameter unitBytes2Parameter, long bytes2)
    {
        Cancel = new DelegateCommand(() => CloseCancel?.Invoke());
        Accept = new DelegateCommand(() => CloseOk?.Invoke());
        this.bytes2 = bytes2;
        PvPFlags = unitBytes2Parameter.pvpFlagsParameter.Items;
        PetFlags = unitBytes2Parameter.petFlagsParameter.Items;
        ShapeshiftForms = unitBytes2Parameter.shapeshiftFormParameter.Items!
            .Select(i => new ShapeshiftFormViewModel((byte)i.Key, i.Value.Name)).ToList();
    }
    
    public Dictionary<long, SelectOption>? PvPFlags { get; }
    public Dictionary<long, SelectOption>? PetFlags { get; }
    public List<ShapeshiftFormViewModel> ShapeshiftForms { get; }

    public SheathStates SheathState
    {
        get
        {
            var split = bytes2.SplitBytes2();
            return (SheathStates)split.sheathState;
        }
        set => Bytes2 = (bytes2 & ~0xFF) | ((uint)value & 0xFF);
    }
    
    public long PvPFlag
    {
        get
        {
            var split = bytes2.SplitBytes2();
            return split.pvpFlag;
        }
        set => Bytes2 = (bytes2 & ~0xFF00) | (((uint)value & 0xFF) << 8);
    }

    public long PetFlag
    {
        get
        {
            var split = bytes2.SplitBytes2();
            return split.petFlags;
        }
        set => Bytes2 = (bytes2 & ~0xFF0000) | (((uint)value & 0xFF) << 16);
    }
    
    public ShapeshiftFormViewModel ShapeshiftForm
    {
        get
        {
            var split = bytes2.SplitBytes2();
            return ShapeshiftForms.FirstOrDefault(x => x.Value == split.shapeshiftForm) ?? new ShapeshiftFormViewModel(split.shapeshiftForm, "(uknown)");
        }
        set => Bytes2 = (bytes2 & ~0xFF000000) | (((uint)value.Value & 0xFF) << 24);
    }

    private long bytes2;
    public long Bytes2
    {
        get => bytes2;
        set
        {
            bytes2 = value;
            RaisePropertyChanged(nameof(SheathState));
            RaisePropertyChanged(nameof(PvPFlag));
            RaisePropertyChanged(nameof(PetFlag));
            RaisePropertyChanged(nameof(ShapeshiftForm));
            RaisePropertyChanged(nameof(Bytes2));
        }
    }
}

public class ShapeshiftFormViewModel
{
    public byte Value { get; }
    public string Name { get; }

    public ShapeshiftFormViewModel(byte value, string name)
    {
        Value = value;
        Name = name;
    }

    public override string ToString()
    {
        return Name;
    }
}