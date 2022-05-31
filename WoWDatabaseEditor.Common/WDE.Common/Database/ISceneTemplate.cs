namespace WDE.Common.Database;

public interface ISceneTemplate
{
    int SceneId { get; }
    uint Flags { get; }
    uint ScriptPackageId { get; }
    string ScriptName { get; }
}