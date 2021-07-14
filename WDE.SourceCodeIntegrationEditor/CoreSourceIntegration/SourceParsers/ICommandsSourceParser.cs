using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.Extensions;

namespace WDE.SourceCodeIntegrationEditor.CoreSourceIntegration.SourceParsers
{
    [UniqueProvider]
    public interface ICommandsSourceParser
    {
        IEnumerable<CoreCommand> GetAllCommands();
    }

    public readonly struct CoreCommand
    {
        public CoreCommand(string command, string rbacPermission)
        {
            Command = command;
            RbacPermission = rbacPermission;
        }

        public string Command { get; }
        public string RbacPermission { get; }
    }

    [SingleInstance]
    [AutoRegister]
    public class CommandsSourceParser : ICommandsSourceParser
    {
        string commandsPath = "src/server/scripts/Commands";
        
        private readonly ICoreSourceProvider coreSourceProvider;

        private static readonly Regex ReNewGroup =
            new Regex(@"(?:static\s*)?(?:std::vector<ChatCommand>|ChatCommandTable)\s*([a-zA-Z0-9_]+)\s*=?");

        // new style group reference: \{\s*"([^"]*)"\s*,\s*([a-zA-Z0-9_]+)\s*\}
        // $1 - name
        // $2 - children
        private static readonly Regex ReLineNewStyleReference = new Regex(@"\{\s*""([^""]*)""\s*,\s*([a-zA-Z0-9_]+)\s*\}");
                
        // new style command: 
        // $1 - name
        // $2 - method
        // $3 - rbac
        private static readonly Regex ReLineNewStyleCommand =
            new Regex(@"\{\s*""([^""]*)""\s*,\s*([a-zA-Z0-9_]+)\s*,\s*rbac::([A-Z_]+)\s*,\s*Console::(?:Yes|No)\s*\}");
                
        // old style command/group reference: \{\s*"([^"]*)"\s*,\s*rbac::([A-Z_]+)\s*,\s*(true|false),\s*(nullptr|&[a-zA-Z0-9_]+)\s*,\s*"[a-zA-Z ]*"\s*(?:,\s*([a-zA-Z0-9_]+))?\s*\}
        // $1 - name
        // $2 - rbac
        // $3 - true/false - console
        // $4 - handler
        // $5? children
        private static readonly Regex ReLineOldStyle =
            new Regex(
                @"\{\s*""([^""]*)""\s*,\s*rbac::([A-Z_]+)\s*,\s*(true|false),\s*(nullptr|&[a-zA-Z0-9_]+)\s*,\s*""[a-zA-Z ]*""\s*(?:,\s*([a-zA-Z0-9_]+))?\s*\}");
        
        public CommandsSourceParser(ICoreSourceProvider coreSourceProvider)
        {
            this.coreSourceProvider = coreSourceProvider;
        }

        public IEnumerable<CoreCommand> GetAllCommands()
        {
            List<CoreCommand> result = new();
            Dictionary<string, List<(string command, string? rbac, string? reference)>> commands = new();
            foreach (var cppFilePath in coreSourceProvider.GetFilesInDirectory(commandsPath, "cpp"))
            {
                var cppFile = coreSourceProvider.ReadLines(cppFilePath)
                    .Select(line => line.Trim())
                    .Between(t => t.StartsWith("/*"), t => t.EndsWith("*/"), true)
                    .Where(line => !line.StartsWith("//"))
                    .Between(line => line.Contains("GetCommands() const override"), line => line.StartsWith("return"));

                string currentGroupName = "";
                commands.Clear();
                
                foreach (var line in cppFile)
                {
                    Match m = ReNewGroup.Match(line);

                    if (m.Success)
                    {
                        currentGroupName = m.Groups[1].Value;
                        commands[currentGroupName] = new();
                        continue;
                    }
                        
                    m = ReLineNewStyleReference.Match(line);

                    if (m.Success)
                    {
                        var commandPart = m.Groups[1].Value;
                        var childrenList = m.Groups[2].Value;
                        commands[currentGroupName].Add((commandPart, null, childrenList));
                        continue;
                    }

                    m = ReLineNewStyleCommand.Match(line);

                    if (m.Success)
                    {
                        var commandPart = m.Groups[1].Value;
                        var rbac = m.Groups[3].Value;
                        commands[currentGroupName].Add((commandPart, rbac, null));
                        continue;
                    }

                    m = ReLineOldStyle.Match(line);
                    if (m.Success)
                    {
                        var commandPart = m.Groups[1].Value;
                        if (m.Groups[5].Length > 0)
                        {
                            var children = m.Groups[5].Value;
                            commands[currentGroupName].Add((commandPart, null, children));
                        }
                        else
                        {
                            var rbac = m.Groups[2].Value;
                            commands[currentGroupName].Add((commandPart, rbac, null));
                        }
                    }
                }
                if (commands.Count > 0 && !string.IsNullOrEmpty(currentGroupName))
                    DenormalizeCommands(commands, "", currentGroupName, result);
            }

            return result;
        }

        public void DenormalizeCommands(Dictionary<string, List<(string command, string? rbac, string? reference)>> commands, string prefix, string group, List<CoreCommand> result)
        {
            foreach (var pair in commands[group])
            {
                if (pair.reference != null)
                    DenormalizeCommands(commands, prefix + pair.command + " ", pair.reference, result);
                else
                    result.Add(new CoreCommand(prefix + pair.command, pair.rbac!));
            }
        }
    }
}