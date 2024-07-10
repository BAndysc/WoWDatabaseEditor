using System;
using System.IO;
using System.Security.Cryptography;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Utils;

[AutoRegister]
public class HashingUtils : IHashingUtils
{
    public MD5Hash ComputeMD5(ReadOnlySpan<byte> data)
    {
        MD5Hash output = default;
        if (!MD5.TryHashData(data, output.array, out _))
            throw new Exception("Couldn't hash data");
        return output;
    }

    public MD5Hash ComputeMD5(FileInfo file)
    {
        using var md5 = MD5.Create();
        using var stream = file.OpenRead();
        var hash = md5.ComputeHash(stream);
        MD5Hash output = default;
        for (int i = 0; i < hash.Length; ++i)
            output[i] = hash[i];
        return output;
    }
}