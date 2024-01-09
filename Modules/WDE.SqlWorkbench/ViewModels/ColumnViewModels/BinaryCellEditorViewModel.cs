using System;
using System.IO;
using AsyncAwaitBestPractices.MVVM;
using Microsoft.Win32.SafeHandles;
using WDE.Common.Avalonia.Controls;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;
using WDE.SqlWorkbench.Services.UserQuestions;

namespace WDE.SqlWorkbench.ViewModels;

internal class BinaryCellEditorViewModel : BaseCellEditorViewModel
{
    private readonly BinarySparseColumnData overrideData;
    private int? maxLength;
    
    public CopyOnWriteMemory<byte> Bytes { get; private set; }

    public IAsyncCommand LoadCommand { get; }
    
    public IAsyncCommand SaveCommand { get; }
    
    public int Length
    {
        get => Bytes.Length;
        set
        {
            if (value == Bytes.Length)
                return;

            isModified = true;
            var newBytes = new byte[value];
            Bytes.Slice(0, Math.Min(value, Bytes.Length)).CopyTo(newBytes);
            Bytes = new CopyOnWriteMemory<byte>(newBytes);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(Bytes));
            ApplyChanges();
        }
    }
    
    public BinaryCellEditorViewModel(IWindowManager windowManager, IUserQuestionsService userQuestionsService, string? mySqlType, BinarySparseColumnData overrideData, BinaryColumnData data, int rowIndex, bool nullable, bool readOnly) 
        : base(mySqlType, overrideData, data, rowIndex, nullable, readOnly)
    {
        this.overrideData = overrideData;
        if (MySqlType.TryParse(mySqlType, out var type)
            && type.Kind == MySqlTypeKind.Text)
        {
            maxLength = type.AsText()!.Value.Length;
        }

        Bytes = overrideData.HasRow(rowIndex)
            ? new CopyOnWriteMemory<byte>(overrideData.AsWriteableMemory(rowIndex))
            : new CopyOnWriteMemory<byte>(data.AsReadOnlyMemory(rowIndex));
        
        LoadCommand = new AsyncAutoCommand(async () =>
        {
            var file = await windowManager.ShowOpenFileDialog("All files", "*");
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
                return;
            
            var fileLength = new FileInfo(file).Length;
            if (fileLength > 4 * 1024 * 1024)
            {
                await userQuestionsService.FileTooBigErrorAsync(fileLength, 4 * 1024 * 1024);
                return;
            }
            
            var bytes = await File.ReadAllBytesAsync(file);
            if (maxLength.HasValue && fileLength > maxLength)
            {
                await userQuestionsService.InformLoadedFileTrimmedAsync(maxLength.Value);
                bytes = bytes.AsSpan(0, maxLength.Value).ToArray();
            }
            Bytes = new CopyOnWriteMemory<byte>(bytes);
            isModified = true;
            RaisePropertyChanged(nameof(Bytes));
            RaisePropertyChanged(nameof(Length));
            ApplyChanges();
        });
        
        SaveCommand = new AsyncAutoCommand(async () =>
        {
            var file = await windowManager.ShowSaveFileDialog("All files", "*");
            if (string.IsNullOrEmpty(file))
                return;
            
            using SafeFileHandle sfh = File.OpenHandle(file, FileMode.Create, FileAccess.Write, FileShare.Read);
            RandomAccess.Write(sfh, Bytes.Slice(0, Bytes.Length), 0);
        });
    }

    public override void ApplyChanges()
    {
        if (!isModified && !Bytes.IsModified)
            return;
        
        overrideData.OverrideNull(rowIndex, IsNull);
        if (!IsNull && Bytes.IsCopy)
            overrideData.Override(rowIndex, Bytes.ModifiedArray);
    }
}