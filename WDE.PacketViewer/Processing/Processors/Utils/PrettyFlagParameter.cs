using WDE.Common.Parameters;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Processing.Processors.Utils;

[AutoRegister]
public class PrettyFlagParameter
{
    private ulong gameBuild;
    private IParameter<long> unitFlagsParameter;
    private IParameter<long> unitFlags2Parameter;
    private IParameter<long> FactionTemplateParameter;
    private IParameter<long> emoteParameter;
    private IParameter<long> npcFlagsParameter;
    private IParameter<long> gameobjectBytes1Parameter;
    private IParameter<long>[] unitBytesParameters = new IParameter<long>[3];
    private readonly IParameter<long> modelParameter;
    private readonly IParameter<long> objectName;
    private readonly IParameter<long> itemName;
    private readonly IParameter<long> gameObjectFlags;
    private readonly IParameter<long> gameobjectModel;
    private readonly IParameter<long> unitBytes0PostMop;
    private readonly IParameter<long> unitBytes0PreMop;

    public PrettyFlagParameter(IParameterFactory parameterFactory)
    {
        unitFlagsParameter = parameterFactory.Factory("UnitFlagParameter");
        unitFlags2Parameter = parameterFactory.Factory("UnitFlags2Parameter");
        FactionTemplateParameter = parameterFactory.Factory("FactionTemplateParameter");
        emoteParameter = parameterFactory.Factory("EmoteParameter");
        npcFlagsParameter = parameterFactory.Factory("NpcFlagParameter");
        gameobjectModel = parameterFactory.Factory("GameObjectDisplayInfoParameter");
        gameobjectBytes1Parameter = parameterFactory.Factory("GameobjectBytes1Parameter");
        unitBytes0PostMop = parameterFactory.Factory("UnitBytes0PostMopParameter");
        unitBytes0PreMop = parameterFactory.Factory("UnitBytes0PreMopParameter");
        unitBytesParameters[1] = parameterFactory.Factory("UnitBytes1Parameter");
        unitBytesParameters[2] = parameterFactory.Factory("UnitBytes2Parameter");
        modelParameter = parameterFactory.Factory("CreatureModelDataParameter");
        objectName = parameterFactory.Factory("CreatureGameobjectNameParameter");
        itemName = parameterFactory.Factory("ItemParameter");
        gameObjectFlags = parameterFactory.Factory("GameObjectFlagParameter");
    }

    public void InitializeBuild(ulong gameBuild)
    {
        this.gameBuild = gameBuild;
    }
    
    public IParameter<long>? GetPrettyParameter(string field)
    {
        switch (field)
        {
            case "UNIT_VIRTUAL_ITEM_SLOT_ID":
            case "UNIT_VIRTUAL_ITEM_SLOT_ID + 1":
            case "UNIT_VIRTUAL_ITEM_SLOT_ID + 2":
            case "UNIT_VIRTUAL_ITEM_SLOT_ID + 3":
            case "UNIT_VIRTUAL_ITEM_SLOT_ID + 4":
            case "UNIT_VIRTUAL_ITEM_SLOT_ID + 5":
                return itemName;
            case "OBJECT_FIELD_ENTRY":
                return objectName;
            case "UNIT_FIELD_DISPLAYID":
            case "UNIT_FIELD_NATIVEDISPLAYID":
                return modelParameter;
            case "UNIT_FIELD_FACTIONTEMPLATE":
            case "GAMEOBJECT_FACTION":
                return FactionTemplateParameter;
            case "GAMEOBJECT_FLAGS":
                return gameObjectFlags;
            case "UNIT_FIELD_FLAGS":
                return unitFlagsParameter;
            case "UNIT_FIELD_FLAGS_2":
                return unitFlags2Parameter;
            case "UNIT_NPC_EMOTESTATE":
                return emoteParameter;
            case "UNIT_NPC_FLAGS":
                return npcFlagsParameter;
            case "UNIT_FIELD_BYTES_0":
                return gameBuild >= 17359 ? unitBytes0PostMop : unitBytes0PreMop;
            case "UNIT_FIELD_BYTES_1":
                return unitBytesParameters[1];
            case "UNIT_FIELD_BYTES_2":
                return unitBytesParameters[2];
            case "GAMEOBJECT_BYTES_1":
                return gameobjectBytes1Parameter;
            case "GAMEOBJECT_DISPLAYID":
                return gameobjectModel;
        }

        return null;
    }
}