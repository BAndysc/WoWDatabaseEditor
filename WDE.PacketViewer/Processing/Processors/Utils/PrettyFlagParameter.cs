using WDE.Common.Parameters;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Processing.Processors.Utils;

[AutoRegister]
[SingleInstance]
public class PrettyFlagParameter
{
    private IParameter<long> unitFlagsParameter;
    private IParameter<long> unitFlags2Parameter;
    private IParameter<long> factionParameter;
    private IParameter<long> emoteParameter;
    private IParameter<long> npcFlagsParameter;
    private IParameter<long> gameobjectBytes1Parameter;
    private IParameter<long>[] unitBytesParameters = new IParameter<long>[3];
    private readonly IParameter<long> modelParameter;
    private readonly IParameter<long> objectName;
    private readonly IParameter<long> itemName;
    private readonly IParameter<long> gameObjectFlags;
    private readonly IParameter<long> gameobjectModel;

    public PrettyFlagParameter(IParameterFactory parameterFactory)
    {
        unitFlagsParameter = parameterFactory.Factory("UnitFlagParameter");
        unitFlags2Parameter = parameterFactory.Factory("UnitFlags2Parameter");
        factionParameter = parameterFactory.Factory("FactionParameter");
        emoteParameter = parameterFactory.Factory("EmoteParameter");
        npcFlagsParameter = parameterFactory.Factory("NpcFlagParameter");
        gameobjectModel = parameterFactory.Factory("GameObjectDisplayInfoParameter");
        gameobjectBytes1Parameter = parameterFactory.Factory("GameobjectBytes1Parameter");
        unitBytesParameters[0] = parameterFactory.Factory("UnitBytes0Parameter");
        unitBytesParameters[1] = parameterFactory.Factory("UnitBytes1Parameter");
        unitBytesParameters[2] = parameterFactory.Factory("UnitBytes2Parameter");
        modelParameter = parameterFactory.Factory("CreatureModelDataParameter");
        objectName = parameterFactory.Factory("CreatureGameobjectNameParameter");
        itemName = parameterFactory.Factory("ItemParameter");
        gameObjectFlags = parameterFactory.Factory("GameObjectFlagParameter");
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
                return factionParameter;
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
                return unitBytesParameters[0];
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