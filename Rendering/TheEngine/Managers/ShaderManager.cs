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
        private Dictionary<string, ShaderHandle> shaderHandles;
        private List<Shader> byHandleShaders;

        private readonly Engine engine;

        private readonly FileSystemWatcher watcher;

        private volatile bool reloadAllShaders = false;

        internal ShaderManager(Engine engine)
        {
            shaderHandles = new Dictionary<string, ShaderHandle>();
            byHandleShaders = new List<Shader>();
            this.engine = engine;

            /*watcher = new FileSystemWatcher();
            watcher.Path = engine.Configuration.ShaderDirectory;

            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            
            watcher.Changed += Watcher_Changed;
            watcher.EnableRaisingEvents = true;*/
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

                    var recompiled = engine.Device.CreateShader(usedShader, new string[] { Constants.SHADER_INCLUDE_DIR, RemoveFileName(usedShader) });

                    byHandleShaders[handle.Handle].Dispose();

                    byHandleShaders[handle.Handle] = recompiled;
                }
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

        public ShaderHandle LoadShader(string path)
        {
            path = /*engine.Configuration.ShaderDirectory + "/" +*/ path;
            var shaderDir = RemoveFileName(path);

            if (shaderHandles.TryGetValue(path, out var shader))
                return shader;

            var newShader = engine.Device.CreateShader(path, new string[] { Constants.SHADER_INCLUDE_DIR, shaderDir });

            byHandleShaders.Add(newShader);

            var handle = new ShaderHandle(byHandleShaders.Count - 1);

            shaderHandles.Add(path, handle);

            return handle;
        }

        public void Dispose()
        {
            foreach (var shader in byHandleShaders)
                shader.Dispose();

            byHandleShaders.Clear();
            shaderHandles.Clear();

            //watcher.Dispose();
        }

        internal Shader GetShaderByHandle(ShaderHandle materialHandle)
        {
            return byHandleShaders[materialHandle.Handle];
        }
    }
}
