using System;
using System.Collections.Generic;
using WDE.Common.Tasks;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Managers
{
    //[SingleInstance]
    //[AutoRegister]
    public class ViewLocator : IViewLocator
    {
        private Dictionary<Type, Type?> staticBinding = new();
        private Dictionary<Type, Type?> staticToolBarsBinding = new();
        
        public void Bind(Type viewModel, Type view)
        {
            staticBinding.Add(viewModel, view);
        }

        public void Bind<T, R>()
        {
            staticBinding.Add(typeof(T), typeof(R));
        }

        public bool TryResolve(Type? viewModel, out Type view, out string? failReason)
        {
            failReason = null;
            view = null!;

            if (viewModel == null)
            {
                failReason = "ViewModel is null";
                return false;
            }
            
            if (staticBinding.TryGetValue(viewModel, out var view_))
            {
                view = view_!;
                return view_ != null;
            }

            if (viewModel.AssemblyQualifiedName == null)
            {
                failReason = "AssemblyQualifiedName is null";
                staticBinding[viewModel] = null;
                return false;
            }

            if (!viewModel.Name.EndsWith("ViewModel"))
            {
                failReason = "ViewModel name does not end with ViewModel";
                staticBinding[viewModel] = null;
                return false;
            }

            var viewString = viewModel.AssemblyQualifiedName!.Replace("ViewModel", "View");
            string viewString2 = "";
            view = Type.GetType(viewString)!;

            if (view == null) // try backend version
            {
                var assemblyName = viewModel.Assembly.GetName().Name;
                viewString2 = viewString.Replace(assemblyName!, assemblyName + "." + GlobalApplication.Backend);
                view = Type.GetType(viewString2)!;
            }
            
            if (view == null) // try backend version + .GUI for main assembly
            {
                failReason = $"{viewString} and {viewString2} not found";
                var assemblyName = viewModel.Assembly.GetName().Name;
                view = Type.GetType(viewString.Replace(assemblyName!, assemblyName + "." + GlobalApplication.Backend).Replace(", Version=", ".GUI, Version="))!;
            }

            staticBinding[viewModel] = view;
            
            return view != null;
        }

        public void BindToolBar<T, R>()
        {
            staticToolBarsBinding.Add(typeof(T), typeof(R));
        }
        
        public bool TryResolveToolBar(Type? viewModel, out Type toolBar)
        {
            toolBar = null!;

            if (viewModel == null)
                return false;
            
            if (staticToolBarsBinding.TryGetValue(viewModel, out var view_))
            {
                toolBar = view_!;
                staticToolBarsBinding[viewModel] = toolBar;
                return view_ != null;
            }

            if (viewModel.AssemblyQualifiedName == null)
            {
                staticToolBarsBinding[viewModel] = toolBar;
                return false;
            }

            var viewString = viewModel.AssemblyQualifiedName!.Replace("ViewModel", "ToolBar").Replace("ToolBars", "Views");
            toolBar = Type.GetType(viewString)!;

            if (toolBar == null) // try backend version
            {
                var assemblyName = viewModel.Assembly.GetName().Name;
                toolBar = Type.GetType(viewString.Replace(assemblyName!, assemblyName + "." + GlobalApplication.Backend))!;
            }

            if (toolBar == null) // try backend version + .GUI
            {
                var assemblyName = viewModel.Assembly.GetName().Name;
                toolBar = Type.GetType(viewString.Replace(assemblyName!, assemblyName + "." + GlobalApplication.Backend).Replace(", Version=", ".GUI, Version="))!;
            }

            staticToolBarsBinding[viewModel] = toolBar;
            
            return toolBar != null;
        }
    }
}