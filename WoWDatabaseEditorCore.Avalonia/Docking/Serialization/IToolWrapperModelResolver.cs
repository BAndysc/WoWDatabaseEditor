﻿namespace WoWDatabaseEditorCore.Avalonia.Docking.Serialization
{
    public interface IToolWrapperModelResolver
    {
        AvaloniaToolDockWrapper? ResolveTool(string id);
    }
}