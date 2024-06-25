using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface IVersionedFilesService
{
    FileHandle OpenFile(FileInfo fileInfo);

    bool IsValidHandle(FileHandle handle);

    bool IsStaged(FileHandle handle);

    FileInfo? GetFullOriginalPath(FileHandle handle);

    FileInfo GetFullPath(FileHandle handle);

    void MarkModified(FileHandle handle);

    string ReadAllText(FileHandle handle);

    string ReadAllOriginalText(FileHandle handle);

    Task<string> ReadAllTextAsync(FileHandle handle);

    void WriteOriginalText(FileHandle handle, string? text);

    void WriteOriginalBytes(FileHandle handle, byte[]? bytes);

    void WriteAllText(FileHandle handle, string text);

    void WriteAllBytes(FileHandle handle, byte[] bytes);

    FileStream OpenStream(FileHandle handle, FileMode mode, FileAccess access);

    IEnumerable<FileHandle> ModfiedFiles();

    int StagedFilesCount { get; }

    event Action OnStagedFilesChanged;

    void RestoreOriginal(FileHandle fileHandle);

    void MarkCurrentAsOriginal(FileHandle fileHandle);

    void Copy(FileInfo sourceFile, FileHandle destinationHandle);
}

[NonUniqueProvider]
public interface IVersionedFilesViewModel
{
    bool AnyPendingFiles { get; }
    int StagedFilesCount { get; }
    ICommand OpenUploadWindow { get; }
}