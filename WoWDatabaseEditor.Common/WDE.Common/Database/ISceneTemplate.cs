namespace WDE.Common.Database;

public interface ISceneTemplate
{
    uint SceneId { get; }
    uint Flags { get; }
    uint ScriptPackageId { get; }
    string ScriptName { get; }
}