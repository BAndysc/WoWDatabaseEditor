using System;
using AvaloniaGraph.ViewModels;

namespace WDE.QuestChainEditor.ViewModels;

public enum QuestConnectionType
{
    Hierarchy,
    And,
    Or,
    OneOf
}

public class QuestConnectionViewModel : ConnectionViewModel<QuestViewModel, QuestConnectionViewModel>
{
    private QuestConnectionType connectionType;

    public QuestConnectionType ConnectionType
    {
        get { return connectionType; }
        set
        {
            connectionType = value;
            switch (connectionType)
            {
                case QuestConnectionType.Hierarchy:
                    Text = "";
                    break;
                case QuestConnectionType.And:
                    Text = "AND";
                    break;
                case QuestConnectionType.Or:
                    Text = "OR";
                    break;
                case QuestConnectionType.OneOf:
                    Text = "XOR";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            RaisePropertyChanged(nameof(ConnectionType));
            RaisePropertyChanged(nameof(Text));
        }
    }

    public bool IsHierarchyConnection => ConnectionType == QuestConnectionType.Hierarchy;
    public bool IsAndConnection => ConnectionType == QuestConnectionType.And;
    public bool IsOrConnection => ConnectionType == QuestConnectionType.Or;
    public bool IsOneOfConnection => ConnectionType == QuestConnectionType.OneOf;
    
    public QuestConnectionViewModel(OutputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> from, 
        InputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> to,
        QuestConnectionType connectionType) : base(from, to)
    {
        ConnectionType = connectionType;
    }

    public QuestConnectionViewModel(InputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> to,
        QuestConnectionType connectionType) : base(to)
    {
        ConnectionType = connectionType;
    }

    public QuestConnectionViewModel(OutputConnectorViewModel<QuestViewModel, QuestConnectionViewModel> from,
        QuestConnectionType connectionType) : base(from)
    {
        ConnectionType = connectionType;
    }

    public string Text { get; private set; } = "";
}