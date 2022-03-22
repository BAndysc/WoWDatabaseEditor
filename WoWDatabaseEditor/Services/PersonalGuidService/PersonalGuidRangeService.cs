using System;
using System.Diagnostics;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.PersonalGuidService;

[UniqueProvider]
public interface IPersonalGuidRangeSettingsService
{
    PersonalGuidData CurrentData { get; }
    void Override(PersonalGuidData data);
}

[AutoRegister]
[SingleInstance]
public class PersonalGuidRangeService : IPersonalGuidRangeService, IPersonalGuidRangeSettingsService
{
    private readonly IUserSettings userSettings;
    private PersonalGuidData data;
    
    public PersonalGuidRangeService(IUserSettings userSettings)
    {
        this.userSettings = userSettings;
        data = userSettings.Get<PersonalGuidData>();
    }

    public bool IsConfigured => data.Enabled;

    private void Save()
    {
        userSettings.Update(data);
    }

    public uint GetNextGuid(GuidType type)
    {
        return GetNextGuidRange(type, 1);
    }

    public uint GetNextGuidRange(GuidType type, uint count)
    {
        if (!IsConfigured)
            throw new GuidServiceNotSetupException();
        if (count == 0)
            throw new Exception("Cannot get 0 guids");
        switch (type)
        {
            case GuidType.Creature:
            {
                if (data.CurrentCreature + count > data.StartCreature + data.CreatureCount)
                    throw new NoMoreGuidsException(type);
                var counter = data.CurrentCreature;
                data.CurrentCreature += count;
                Save();
                return counter;
            }
            case GuidType.GameObject:
            {
                if (data.CurrentGameObject + count > data.GameObjectCount + data.StartGameObject)
                    throw new NoMoreGuidsException(type);
                var counter = data.CurrentGameObject;
                data.CurrentGameObject += count;
                Save();
                return counter;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public PersonalGuidData CurrentData => data;
    
    public void Override(PersonalGuidData data)
    {
        this.data = data;
        Save();
    }
}

public struct PersonalGuidData : ISettings
{
    public bool Enabled { get; set; }
    public uint StartCreature { get; set; }
    public uint CurrentCreature { get; set; }
    public uint CreatureCount { get; set; }
    public uint StartGameObject { get; set; }
    public uint CurrentGameObject { get; set; }
    public uint GameObjectCount { get; set; }
}