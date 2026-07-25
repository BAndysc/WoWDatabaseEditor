using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using WDE.CMangosConditions.Data;

namespace WDE.CMangosConditions.Test
{
    public class ConditionSubjectTests
    {
        private static Task<IReadOnlyList<MangosConditionJson>> LoadConditions() =>
            new MangosConditionDataProvider(new TestRuntimeDataService()).GetConditions();

        [Test]
        public async Task Tags_ParseIntoSubjectFlags()
        {
            var byId = (await LoadConditions()).ToDictionary(c => c.Id);
            Assert.AreEqual(MangosConditionSubject.Player, byId[2].Subject);          // CONDITION_ITEM
            Assert.AreEqual(MangosConditionSubject.Unit, byId[1].Subject);            // CONDITION_AURA
            Assert.AreEqual(MangosConditionSubject.WorldObject, byId[4].Subject);     // CONDITION_AREAID
            Assert.AreEqual(MangosConditionSubject.Map, byId[42].Subject);           // CONDITION_WORLDSTATE
            Assert.AreEqual(MangosConditionSubject.SourceCreature, byId[33].Subject); // CONDITION_LAST_WAYPOINT
            Assert.AreEqual(MangosConditionSubject.None, byId[12].Subject);          // CONDITION_ACTIVE_GAME_EVENT (no tags)
        }

        [Test]
        public void Converter_IsCaseAndDashInsensitive()
        {
            Assert.AreEqual(MangosConditionSubject.SourceCreature,
                JsonConvert.DeserializeObject<MangosConditionSubject>("[\"Source-Creature\"]"));
            Assert.AreEqual(MangosConditionSubject.WorldObject | MangosConditionSubject.Player,
                JsonConvert.DeserializeObject<MangosConditionSubject>("[\"worldobject\", \"PLAYER\"]"));
            Assert.AreEqual(MangosConditionSubject.None,
                JsonConvert.DeserializeObject<MangosConditionSubject>("[\"nonsense\"]"));
        }

        [Test]
        public void DefaultTargetName_MapsSubject()
        {
            Assert.AreEqual("player", MangosConditionSubject.Player.DefaultTargetName());
            Assert.AreEqual("object", MangosConditionSubject.WorldObject.DefaultTargetName());
            Assert.AreEqual("target", MangosConditionSubject.Unit.DefaultTargetName());
            Assert.AreEqual("target", MangosConditionSubject.None.DefaultTargetName());
        }
    }
}
