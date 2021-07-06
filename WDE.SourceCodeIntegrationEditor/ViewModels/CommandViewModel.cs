using Prism.Mvvm;
using WDE.Common.Database;

namespace WDE.SourceCodeIntegrationEditor.ViewModels
{
    public class PermissionViewModel : BindableBase
    {
        public PermissionViewModel(uint rbacId, string enumName, IAuthRbacPermission rbacPermission)
        {
            RbacId = rbacId;
            RbacName = enumName;
            IsRbacDefined = true;
            permissionReadableName = rbacPermission.Name;
        }
        
        public PermissionViewModel(uint rbacId, string enumName, string fallbackPermissionName)
        {
            RbacId = rbacId;
            RbacName = enumName;
            IsRbacDefined = false;
            permissionReadableName = fallbackPermissionName;
        }
        
        public uint RbacId { get; }
        public string RbacName { get; }

        public PermissionViewModel? parent;
        public PermissionViewModel? Parent
        {
            get => parent;
            set => SetProperty(ref parent, value);
        }
        public bool IsRbacDefined { get; }
        public string PermissionText => $"{RbacName} ({RbacId})";

        private string permissionReadableName;
        public string PermissionReadableName
        {
            get => permissionReadableName;
            set => SetProperty(ref permissionReadableName, value);
        }
    }
    
    public class CommandViewModel : BindableBase
    {
        public CommandViewModel(string name, PermissionViewModel permission)
        {
            Name = name;
            PermissionViewModel = permission;
            CommandHelp = "Syntax: ." + name;
        }

        public bool IsSelected { get; set; }
        public PermissionViewModel PermissionViewModel { get; }
        public string Name { get; }
        public string NameWithDot => "." + Name;
        public string CommandHelp { get; set; }

        public bool IsRbacDefined => PermissionViewModel.IsRbacDefined;
        public string PermissionText => PermissionViewModel.PermissionText;
    }
}