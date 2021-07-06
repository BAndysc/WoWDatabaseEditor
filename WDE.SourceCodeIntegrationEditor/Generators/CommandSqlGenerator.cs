using System.Collections.Generic;
using System.Linq;
using System.Text;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.ViewModels;

namespace WDE.SourceCodeIntegrationEditor.Generators
{
    public interface ICommandSqlGenerator
    {
        string? GenerateAuth(IList<CommandViewModel> commands);
        string? GenerateWorld(IList<CommandViewModel> commands);
    }
    
    [SingleInstance]
    [AutoRegister]
    public class CommandSqlGenerator : ICommandSqlGenerator
    {
        public string? GenerateAuth(IList<CommandViewModel> commands)
        {
            var permissionsToDefine = commands
                .Where(c => !c.IsRbacDefined)
                .GroupBy(c => c.PermissionViewModel.RbacId)
                .Select(c => c.FirstOrDefault())
                .Where(c => c != null)
                .Select(c => c!.PermissionViewModel)
                .ToList();

            if (permissionsToDefine.Count == 0)
                return null;
            
            StringBuilder sb = new();
            List<string> rbacPermissions = new();
            List<string> rbacLinkedPermissions = new();
            
            foreach (var perm in permissionsToDefine)
            {
                rbacPermissions.Add($"({perm.RbacId}, {perm.PermissionReadableName.ToSqlEscapeString()})");
                if (perm.Parent != null)
                    rbacLinkedPermissions.Add($"({perm.Parent.RbacId}, {perm.RbacId})");
            }

            if (rbacPermissions.Count > 0)
            {
                sb.AppendLine("REPLACE INTO `rbac_permissions` (`id`, `name`) VALUES ");
                sb.Append(string.Join(",\n", rbacPermissions));
                sb.AppendLine(";\n");
            }

            if (rbacLinkedPermissions.Count > 0)
            {
                sb.AppendLine("REPLACE INTO `rbac_linked_permissions` (`id`, `linkedId`) VALUES ");
                sb.Append(string.Join(",\n", rbacLinkedPermissions));
                sb.AppendLine(";\n");
            }
            
            return sb.ToString();
        }

        public string? GenerateWorld(IList<CommandViewModel> commands)
        {
            if (commands.Count == 0)
                return null;
            
            StringBuilder sb = new();
            
            List<string> sqlCommands = new();
            foreach (var cmd in commands)
            {
                sqlCommands.Add($"({cmd.Name.ToSqlEscapeString()}, {cmd.CommandHelp.ToSqlEscapeString()})");
            }
            
            if (sqlCommands.Count > 0)
            {
                sb.AppendLine("REPLACE INTO `command` (`name`, `help`) VALUES ");
                sb.Append(string.Join(",\n", sqlCommands));
                sb.AppendLine(";\n");
            }
            
            return sb.ToString();
        }
    }
}