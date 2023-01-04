using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Updater.Client
{
    [AutoRegister]
    [SingleInstance]
    public class UpdateVerifier : IUpdateVerifier
    {
        public Task<bool> IsUpdateValid(FileInfo file, string hash)
        {
            return Task.Run(() =>
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(file.FullName);
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-","").ToLower() == hash;
            });
        }
    }
}