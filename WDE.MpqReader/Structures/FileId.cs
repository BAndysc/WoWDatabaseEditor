namespace WDE.MpqReader.Structures;

public readonly struct FileId : IEquatable<FileId>
{
    public bool Equals(FileId other)
    {
        return FileName == other.FileName && FileDataId == other.FileDataId && FileType == other.FileType;
    }

    public override bool Equals(object? obj)
    {
        return obj is FileId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FileName, FileDataId, (int)FileType);
    }

    public static bool operator ==(FileId left, FileId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FileId left, FileId right)
    {
        return !left.Equals(right);
    }

    public enum Type
    {
        FileName,
        FileId
    }
    
    public readonly string FileName;
    public readonly uint FileDataId;
    public readonly Type FileType;
    
    public FileId(string fileName)
    {
        FileName = fileName;
        FileDataId = 0;
        FileType = Type.FileName;
    }
    
    public FileId(uint fileId)
    {
        FileName = null!;
        FileDataId = fileId;
        FileType = Type.FileId;
    }
    
    public static implicit operator FileId(string name)
    {
        return new FileId(name);
    }
    
    public static implicit operator FileId(uint id)
    {
        return new FileId(id);
    }
    
    public static implicit operator FileId(int id)
    {
        if (id < 0)
            throw new Exception("This wasn't expected, but if it happened, then it must be supported. Change the id to long?");
        return new FileId((uint)id);
    }

    public override string ToString()
    {
        return FileType == Type.FileName ? FileName : $"File{FileDataId}.ext";
    }

    public FileId Replace(string searchFor, string replaceWith, StringComparison options)
    {
        if (FileType == Type.FileId)
            return this;
        return FileName.Replace(searchFor, replaceWith, options);
    }
}