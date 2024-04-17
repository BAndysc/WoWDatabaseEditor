using System.Collections.Generic;

namespace WDE.QuestChainEditor.Services;

public struct QuestState
{
    public QuestState(uint questId, QuestStatus status, bool canStart, IReadOnlyList<(string, bool)> checks)
    {
        QuestId = questId;
        Status = status;
        CanStart = canStart;
        Checks = checks;
    }

    public uint QuestId { get; }
    public QuestStatus Status { get; }
    public bool CanStart { get; }
    public IReadOnlyList<(string, bool)> Checks { get; }
}