using System;
using System.Collections.Generic;
using WDE.Common.Windows;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Managers
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
            
            view = Type.GetType(viewModel.AssemblyQualifiedName!.Replace("ViewModel", "View"));

            if (view != null)
                staticBinding[viewModel] = view;
            
            return view != null;
        }
    }
}