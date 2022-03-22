using System;
using NSubstitute;
using NUnit.Framework;
using WDE.Common.Services;
using WoWDatabaseEditorCore.Services.PersonalGuidService;

namespace WoWDatabaseEditorCore.Test.Services.PersonalGuidService;

public class PersonalGuidServiceTests
{
    private PersonalGuidRangeService Create(uint creature, uint creatureCount, uint gameObject, uint gameObjectCount)
    {
        var settings = Substitute.For<IUserSettings>();
        settings.Get<PersonalGuidData>().ReturnsForAnyArgs(new PersonalGuidData()
        {
            Enabled = true,
            StartCreature = creature,
            StartGameObject = gameObject,
            CurrentCreature = creature,
            CreatureCount = creatureCount,
            CurrentGameObject = gameObject,
            GameObjectCount = gameObjectCount
        });
        var service = new PersonalGuidRangeService(settings);
        return service;
    }
    
    [Test]
    public void TestCreatureGuids()
    {
        var service = Create(1, 5, 0, 0);
        Assert.AreEqual(1, service.GetNextGuid(GuidType.Creature));
        Assert.AreEqual(2, service.GetNextGuid(GuidType.Creature));
        Assert.AreEqual(3, service.GetNextGuid(GuidType.Creature));
        Assert.AreEqual(4, service.GetNextGuid(GuidType.Creature));
        Assert.AreEqual(5, service.GetNextGuid(GuidType.Creature));
        Assert.Throws<NoMoreGuidsException>(() => service.GetNextGuid(GuidType.Creature));
    }
    
    [Test]
    public void TestCreatureRange()
    {
        var service = Create(1, 5, 0, 0);
        Assert.AreEqual(1, service.GetNextGuidRange(GuidType.Creature, 2));
        Assert.AreEqual(3, service.GetNextGuid(GuidType.Creature));
        Assert.Throws<NoMoreGuidsException>(() => service.GetNextGuidRange(GuidType.Creature, 3));
        Assert.AreEqual(4, service.GetNextGuidRange(GuidType.Creature, 2));
        Assert.Throws<NoMoreGuidsException>(() => service.GetNextGuid(GuidType.Creature));
    }
    
    [Test]
    public void TestCreatureGameObjectsAreSeparate()
    {
        var service = Create(1, 5, 1, 5);
        Assert.AreEqual(1, service.GetNextGuid(GuidType.Creature));
        Assert.AreEqual(1, service.GetNextGuid(GuidType.GameObject));
        Assert.AreEqual(2, service.GetNextGuidRange(GuidType.Creature, 2));
        Assert.AreEqual(2, service.GetNextGuidRange(GuidType.GameObject, 2));
        Assert.AreEqual(4, service.GetNextGuid(GuidType.Creature));
        Assert.AreEqual(4, service.GetNextGuid(GuidType.GameObject));
        Assert.AreEqual(5, service.GetNextGuid(GuidType.GameObject));
        Assert.AreEqual(5, service.GetNextGuid(GuidType.Creature));
    }

    [Test]
    public void TestSettingsAreSaved()
    {
        var settings = Substitute.For<IUserSettings>();
        settings.Get<PersonalGuidData>().ReturnsForAnyArgs(new PersonalGuidData()
        {
            Enabled = true,
            StartCreature = 0,
            StartGameObject = 0,
            CurrentCreature = 0,
            CreatureCount = 4,
            CurrentGameObject = 0,
            GameObjectCount = 4
        });
        PersonalGuidData lastSettings = default;
        settings.WhenForAnyArgs(x => x.Update<PersonalGuidData>(default))
            .Do(x => lastSettings = x.Arg<PersonalGuidData>());
        var service = new PersonalGuidRangeService(settings);
        service.GetNextGuid(GuidType.Creature);
        service.GetNextGuid(GuidType.Creature);
        service.GetNextGuid(GuidType.GameObject);

        Assert.AreEqual(2, lastSettings.CurrentCreature);
        Assert.AreEqual(1, lastSettings.CurrentGameObject);
    }
}