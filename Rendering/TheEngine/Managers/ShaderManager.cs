using System;
using System.Collections.Generic;
using System.IO;
using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Config;
using TheEngine.Handles;
using TheEngine.Interfaces;

namespace TheEngine.Managers
{
    public class ShaderManager : IShaderManager, IDisposable
    {
        private Dictionary<(string path, bool instancing), ShaderHandle> shaderHandles;
        private List<Shader> byHandleShaders;

        private readonly Engine engine;

        private readonly FileSystemWatcher watcher;
        private readonly FileSystemWatcher watcher2;

        private volatile bool reloadAllShaders = false;

        internal ShaderManager(Engine engine)
        {
            shaderHandles = new ();
            byHandleShaders = new List<Shader>();
            this.engine = engine;

            watcher = new FileSystemWatcher();
            SetupWatcher(watcher, "data");
            
            watcher2 = new FileSystemWatcher();
            SetupWatcher(watcher2, "internalShaders");
        }

        private void SetupWatcher(FileSystemWatcher watcher, string path)
        {
            watcher.Path = Path.Combine(Directory.GetCurrentDirectory(), path);
            Console.WriteLine("Observing " + watcher.Path);

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            
            watcher.Changed += Watcher_Changed;
            watcher.EnableRaisingEvents = true;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            reloadAllShaders = true;
        }

        internal void Update()
        {
            if (reloadAllShaders)
            {
                foreach (var usedShader in shaderHandles.Keys)
                {
                    var handle = shaderHandles[usedShader];

                    try
                    {
                        var recompiled = engine.Device.CreateShader(usedShader.path, new string[] { Constants.SHADER_INCLUDE_DIR, RemoveFileName(usedShader.path) }, usedShader.instancing);
                        byHandleShaders[handle.Handle].Dispose();
                        byHandleShaders[handle.Handle] = recompiled;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Couldn't compile reshader " + usedShader.path + " " + e.Message);
                    }
                }

                engine.materialManager.InvalidateShaderCache();
                reloadAllShaders = false;
            }
        }

        private string RemoveFileName(string path)
        {
            int lastSplash = path.LastIndexOf("/");

            if (lastSplash == -1)
                return ".";

            return path.Substring(0, lastSplash);
        }

        public ShaderHandle LoadShader(string path, bool instanced)
        {
            var shaderDir = RemoveFileName(path);

            if (shaderHandles.TryGetValue((path, instanced), out var shader))
                return shader;

            var newShader = engine.Device.CreateShader(path, new string[] { Constants.SHADER_INCLUDE_DIR, shaderDir }, instanced);

            byHandleShaders.Add(newShader);

            var handle = new ShaderHandle(byHandleShaders.Count - 1);

            shaderHandles.Add((path, instanced), handle);

            return handle;
        }

        public void Dispose()
        {
            foreach (var shader in byHandleShaders)
                shader.Dispose();

            byHandleShaders.Clear();
            shaderHandles.Clear();

            watcher.Dispose();
            watcher2.Dispose();
        }

        internal Shader GetShaderByHandle(ShaderHandle materialHandle)
        {
            return byHandleShaders[materialHandle.Handle];
        }
    }
}
