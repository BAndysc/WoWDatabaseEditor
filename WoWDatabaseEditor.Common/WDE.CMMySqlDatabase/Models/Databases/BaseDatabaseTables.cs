﻿using System;
using LinqToDB;
using LinqToDB.Data;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MySqlDatabaseCommon.CommonModels;

namespace WDE.CMMySqlDatabase.Models
{
    public abstract class BaseDatabaseTables : DataConnection
    {
        public BaseDatabaseTables() : base("CMaNGOS-WoTLK-World")
        {
        }

        public ITable<GameObjectTemplateWoTLK> GameObjectTemplate => GetTable<GameObjectTemplateWoTLK>();
        public ITable<QuestTemplateWoTLK> QuestTemplate => GetTable<QuestTemplateWoTLK>();
        public ITable<MySqlQuestRequestItem> QuestRequestItems => GetTable<MySqlQuestRequestItem>();
        public ITable<GameEventWoTLK> GameEvents => GetTable<GameEventWoTLK>();
        public ITable<ConditionLineWoTLK> Conditions => GetTable<ConditionLineWoTLK>();
        public ITable<SpellScriptNameWoTLK> SpellScriptNames => GetTable<SpellScriptNameWoTLK>();
        public ITable<GossipMenuLineWoTLK> GossipMenus => GetTable<GossipMenuLineWoTLK>();
        public ITable<MySqlGossipMenuOptionWrath> GossipMenuOptions => GetTable<MySqlGossipMenuOptionWrath>();
        public ITable<MySqlGossipMenuOptionCata> SplitGossipMenuOptions => GetTable<MySqlGossipMenuOptionCata>();
        public ITable<MySqlGossipMenuOptionAction> SplitGossipMenuOptionActions => GetTable<MySqlGossipMenuOptionAction>();
        public ITable<MySqlGossipMenuOptionBox> SplitGossipMenuOptionBoxes => GetTable<MySqlGossipMenuOptionBox>();
        public ITable<NpcTextWoTLK> NpcTexts => GetTable<NpcTextWoTLK>();
        public ITable<CreatureClassLevelStatWoTLK> CreatureClassLevelStats => GetTable<CreatureClassLevelStatWoTLK>();
        public ITable<CoreCommandHelp> Commands => GetTable<CoreCommandHelp>();
        public ITable<MySqlPointOfInterest> PointsOfInterest => GetTable<MySqlPointOfInterest>();
        public ITable<MySqlCreatureText> CreatureTexts => GetTable<MySqlCreatureText>();
        public ITable<MySqlEventScriptLine> EventScripts => GetTable<MySqlEventScriptLine>();
        public ITable<MySqlWaypointScriptLine> WaypointScripts => GetTable<MySqlWaypointScriptLine>();
        public ITable<MySqlSpellScriptLine> SpellScripts => GetTable<MySqlSpellScriptLine>();
        public ITable<MySqlAreaTriggerCreateProperties> AreaTriggerCreateProperties => GetTable<MySqlAreaTriggerCreateProperties>();
        public ITable<MySqlSceneTemplate> SceneTemplates => GetTable<MySqlSceneTemplate>();
    }
}