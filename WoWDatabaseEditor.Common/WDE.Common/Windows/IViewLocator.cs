using System;
using WDE.Module.Attributes;

namespace WDE.Common.Windows
{
    [UniqueProvider]
    public interface IViewLocator
    {
        void Bind(Type viewModel, Type view);
        void Bind<T, R>();
        bool TryResolve(Type viewModel, out Type view);
        
        void BindToolBar<T, R>();
        bool TryResolveToolBar(Type viewModel, out Type toolBar);
    }
}