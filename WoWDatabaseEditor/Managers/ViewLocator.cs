using System;
using System.Collections.Generic;
using WDE.Common.Tasks;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Managers
{
    [SingleInstance]
    [AutoRegister]
    public class ViewLocator : IViewLocator
    {
        private Dictionary<Type, Type> staticBinding = new();
        
        public void Bind(Type viewModel, Type view)
        {
            staticBinding.Add(viewModel, view);
        }

        public void Bind<T, R>()
        {
            staticBinding.Add(typeof(T), typeof(R));
        }

        public bool TryResolve(Type viewModel, out Type? view)
        {
            view = null;

            if (viewModel == null)
                return false;
            
            if (staticBinding.TryGetValue(viewModel, out var view_))
            {
                view = view_!;
                return true;
            }

            if (viewModel.AssemblyQualifiedName == null)
                return false;

            var viewString = viewModel.AssemblyQualifiedName!.Replace("ViewModel", "View");
            view = Type.GetType(viewString);

            if (view == null) // try backend version
            {
                var assemblyName = viewModel.Assembly.GetName().Name;
                view = Type.GetType(viewString.Replace(assemblyName!, assemblyName + "." + GlobalApplication.Backend));
            }

            if (view != null)
                staticBinding[viewModel] = view;
            
            return view != null;
        }
    }
}