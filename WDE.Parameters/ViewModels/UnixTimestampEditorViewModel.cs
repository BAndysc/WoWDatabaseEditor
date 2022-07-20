using System;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Managers;
using WDE.MVVM;

namespace WDE.Parameters.ViewModels;

public partial class UnixTimestampEditorViewModel : ObservableBase, IDialog
{
    public UnixTimestampEditorViewModel()
    {
        var accept = new DelegateCommand(() =>
        {
            CloseOk?.Invoke();
        }, () => dateTime >= minDate && dateTime <= maxDate);
        Accept = accept;
        Cancel = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
        On(() => DateTime, _ => accept.RaiseCanExecuteChanged());
    }

    [Notify] private string? error;
    [Notify] private DateTime minDate = DateTime.UnixEpoch;
    [Notify] private DateTime maxDate = DateTime.UnixEpoch.AddSeconds(uint.MaxValue);
    
    private DateTime dateTime;
    public DateTime DateTime
    {
        get => dateTime;
        set
        {
            //@todo: change to throw DataValidationException when Avalonia is updated
            if (value < minDate || value > maxDate)
                Error = "Date must be greater than " + minDate.ToString("yyyy-MM-dd HH:mm:ss") +
                        " and smaller than " + maxDate.ToString("yyyy-MM-dd HH:mm:ss");
            else
                Error = null;
            dateTime = value;
            RaisePropertyChanged(nameof(DateTime));
            RaisePropertyChanged(nameof(Date));
            RaisePropertyChanged(nameof(Time));
            RaisePropertyChanged(nameof(UnixSeconds));
        }
    }

    public DateTimeOffset Date
    {
        get => dateTime.Date;
        set => DateTime = value.AddMilliseconds(dateTime.TimeOfDay.TotalMilliseconds).DateTime;
    }

    public long UnixSeconds
    {
        get => ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        set => DateTime = DateTime.UnixEpoch.AddSeconds(value);
    }

    public TimeSpan Time
    {
        get => dateTime.TimeOfDay;
        set => DateTime = dateTime.Date.AddMilliseconds(value.TotalMilliseconds);
    }
    
    public int DesiredWidth => 500;
    public int DesiredHeight => 230;
    public string Title => "Date time picker";
    public bool Resizeable => true;
    public ICommand Accept { get; }
    public ICommand Cancel { get; }
    public event Action? CloseCancel;
    public event Action? CloseOk;
}