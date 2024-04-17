using Prism.Ioc;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.QuestChainEditor.ViewModels;

[assembly: ModuleRequiresCore("TrinityWrath", "Azeroth", "TrinityMaster", "TrinityCata", "CMaNGOS-TBC", "CMaNGOS-Classic", "CMaNGOS-WoTLK")]

namespace WDE.DummyQuestChainEditor;

public class DummyQuestChainEditorModule : ScopedModuleBase
{
    public DummyQuestChainEditorModule(IScopedContainer mainScope) : base(mainScope)
    {
    }

    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        base.RegisterTypes(containerRegistry);
        AutoRegisterByConvention(typeof(QuestChainDocumentViewModel).Assembly, moduleScope);
    }

    public override void RegisterFallbackTypes(IContainerRegistry container)
    {
        base.RegisterFallbackTypes(container);
        AutoRegisterFallbackTypesByConvention(typeof(QuestChainDocumentViewModel).Assembly, container);
    }

    public override void FinalizeRegistration(IContainerRegistry container)
    {
        base.FinalizeRegistration(container);
        RegisterToParentScope(typeof(QuestChainDocumentViewModel).Assembly, container);
    }
}