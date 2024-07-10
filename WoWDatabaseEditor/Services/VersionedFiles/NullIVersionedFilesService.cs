using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.VersionedFiles;

[FallbackAutoRegister]
public class NullIVersionedFilesService : IVersionedFilesService
{
    private Dictionary<string, OpenedFile> openedFiles = new();
    private Dictionary<FileHandle, OpenedFile> handleToFile = new();
    private int nextHandle = 0;

    private class OpenedFile
    {
        public OpenedFile(FileHandle handle, FileInfo file)
        {
            Handle = handle;
            File = file;
        }

        public FileHandle Handle { get; }
        public FileInfo File { get; }
    }

    public FileHandle OpenFile(FileInfo fileInfo)
    {
        var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, fileInfo.FullName);
        if (openedFiles.TryGetValue(relativePath, out var file))
            return file.Handle;
        var handle = new FileHandle(nextHandle++);
        openedFiles[relativePath] = new OpenedFile(handle, fileInfo);
        handleToFile[handle] = openedFiles[relativePath];
        return handle;
    }

    public bool IsValidHandle(FileHandle handle) => handleToFile.ContainsKey(handle);

    public bool IsStaged(FileHandle handle) => false;

    public FileInfo? GetFullOriginalPath(FileHandle handle) => null;

    public FileInfo GetFullPath(FileHandle handle) => handleToFile[handle].File;

    public void MarkModified(FileHandle handle) { }

    public string ReadAllText(FileHandle handle) => File.ReadAllText(handleToFile[handle].File.FullName);

    public string ReadAllOriginalText(FileHandle handle) => "";

    public async Task<string> ReadAllTextAsync(FileHandle handle) => await File.ReadAllTextAsync(handleToFile[handle].File.FullName);

    public void WriteOriginalText(FileHandle handle, string? text) { }

    public void WriteOriginalBytes(FileHandle handle, byte[]? bytes) { }

    public void WriteAllText(FileHandle handle, string text) => File.WriteAllText(handleToFile[handle].File.FullName, text);

    public void WriteAllBytes(FileHandle handle, byte[] bytes) => File.WriteAllBytes(handleToFile[handle].File.FullName, bytes);

    public FileStream OpenStream(FileHandle handle, FileMode mode, FileAccess access) => File.Open(handleToFile[handle].File.FullName, mode, access);

    public IEnumerable<FileHandle> ModfiedFiles() => Array.Empty<FileHandle>();

    public int StagedFilesCount => 0;

    public event Action? OnStagedFilesChanged;

    public void RestoreOriginal(FileHandle fileHandle) { }

    public void MarkCurrentAsOriginal(FileHandle fileHandle) { }

    public void Copy(FileInfo sourceFile, FileHandle destinationHandle) => File.Copy(sourceFile.FullName, handleToFile[destinationHandle].File.FullName);
}