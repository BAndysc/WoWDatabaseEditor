using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.ReactiveUI;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using WDE.Common.Tasks;
using WoWDatabaseEditorCore.Avalonia.Utils;
using WoWDatabaseEditorCore.Managers;

namespace WoWDatabaseEditorCore.Avalonia
{
    public class Program
    {
        public static Type[] PreloadedModules = new Type[]{};
        public static string ApplicationName = "WoW Database Editor 2024.1";
    }
}
