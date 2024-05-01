using System.Net.Http.Headers;
using System.Net.Http.Json;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Factories;
using WDE.HttpDatabase.Models;
using WDE.MySqlDatabaseCommon.Database.World;

namespace WDE.HttpDatabase;

public class HttpDatabaseProviderImpl : IAsyncDatabaseProvider
{
    private const string URL = "http://localhost:5262/";

    public bool IsConnected { get; }

    private HttpClient client;

    public HttpDatabaseProviderImpl()
    {
        client = new HttpClient();
    }

    public async Task<SelectResult> ExecuteAnyQuery(string query)
    {
        var result = await client.PostAsync(Path.Join(URL, "ExecuteSelectSql"),
            new StringContent(JsonConvert.SerializeObject(new { query }), new MediaTypeHeaderValue("application/json")));
        result.EnsureSuccessStatusCode();
        var str = await result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<SelectResult>(str);
    }

    public async Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureTemplatesAsync"));
            return JsonConvert.DeserializeObject<List<JsonCreatureTemplateWrath>>(result) ?? new List<JsonCreatureTemplateWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return Array.Empty<JsonCreatureTemplateWrath>();
        }
    }

    public async Task<IReadOnlyList<IConversationTemplate>> GetConversationTemplatesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetConversationTemplatesAsync"));
            return JsonConvert.DeserializeObject<List<JsonConversationTemplate>>(result) ?? new List<JsonConversationTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonConversationTemplate>();
        }
    }

    public async Task<IReadOnlyList<IGameEvent>> GetGameEventsAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameEventsAsync"));
            return JsonConvert.DeserializeObject<List<JsonGameEvent>>(result) ?? new List<JsonGameEvent>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameEvent>();
        }
    }

    public async Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectTemplatesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameObjectTemplatesAsync"));
            return JsonConvert.DeserializeObject<List<JsonGameObjectTemplate>>(result) ?? new List<JsonGameObjectTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameObjectTemplate>();
        }
    }

    public async Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestTemplatesAsync"));
            return JsonConvert.DeserializeObject<List<JsonQuestTemplate>>(result) ?? new List<JsonQuestTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonQuestTemplate>();
        }
    }

    public async Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesBySortIdAsync(int questSortId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestTemplatesBySortIdAsync", questSortId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonQuestTemplate>>(result) ?? new List<JsonQuestTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonQuestTemplate>();
        }
    }

    public async Task<IReadOnlyList<INpcText>> GetNpcTextsAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetNpcTextsAsync"));
            return JsonConvert.DeserializeObject<List<JsonNpcText>>(result) ?? new List<JsonNpcText>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonNpcText>();
        }
    }

    public async Task<IReadOnlyList<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureClassLevelStatsAsync"));
            return JsonConvert.DeserializeObject<List<JsonCreatureClassLevelStat>>(result) ?? new List<JsonCreatureClassLevelStat>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureClassLevelStat>();
        }
    }

    public async Task<ICreatureTemplate?> GetCreatureTemplate(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureTemplate", entry.ToString()));
            return JsonConvert.DeserializeObject<JsonCreatureTemplateWrath>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureTemplateDifficulties", entry.ToString()));
            return JsonConvert.DeserializeObject<List<JsonCreatureTemplateDifficulty>>(result) ?? new List<JsonCreatureTemplateDifficulty>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureTemplateDifficulty>();
        }
    }

    public async Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameObjectTemplate", entry.ToString()));
            return JsonConvert.DeserializeObject<JsonGameObjectTemplate>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetAreaTriggerScript", entry.ToString()));
            return JsonConvert.DeserializeObject<JsonAreaTriggerScript>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetAreaTriggerTemplate", entry.ToString()));
            return JsonConvert.DeserializeObject<JsonAreaTriggerTemplate>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetAreaTriggerTemplatesAsync"));
            return JsonConvert.DeserializeObject<List<JsonAreaTriggerTemplate>>(result) ?? new List<JsonAreaTriggerTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonAreaTriggerTemplate>();
        }
    }

    public async Task<IQuestTemplate?> GetQuestTemplate(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestTemplate", entry.ToString()));
            return JsonConvert.DeserializeObject<JsonQuestTemplate>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<IQuestObjective>> GetQuestObjectives(uint questId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestObjectives", questId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonQuestObjective>>(result) ?? new List<JsonQuestObjective>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonQuestObjective>();
        }
    }

    public async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestObjective", questId.ToString(), storageIndex.ToString()));
            return JsonConvert.DeserializeObject<JsonQuestObjective>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestObjectiveById", objectiveId.ToString()));
            return JsonConvert.DeserializeObject<JsonQuestObjective>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IQuestRequestItem?> GetQuestRequestItem(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestRequestItem", entry.ToString()));
            return JsonConvert.DeserializeObject<JsonQuestRequestItem>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<IGossipMenu>> GetGossipMenusAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGossipMenusAsync"));
            return JsonConvert.DeserializeObject<List<JsonGossipMenu>>(result) ?? new List<JsonGossipMenu>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGossipMenu>();
        }
    }

    public async Task<IGossipMenu?> GetGossipMenuAsync(uint menuId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGossipMenuAsync", menuId.ToString()));
            return JsonConvert.DeserializeObject<JsonGossipMenu>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGossipMenuOptionsAsync", menuId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonGossipMenuOptionWrath>>(result) ?? new List<JsonGossipMenuOptionWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGossipMenuOptionWrath>();
        }
    }

    public async Task<INpcText?> GetNpcText(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetNpcText", entry.ToString()));
            return JsonConvert.DeserializeObject<JsonNpcText>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<IPointOfInterest>> GetPointsOfInterestsAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetPointsOfInterestsAsync"));
            return JsonConvert.DeserializeObject<List<JsonPointOfInterest>>(result) ?? new List<JsonPointOfInterest>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonPointOfInterest>();
        }
    }

    public async Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync()
    {
        return new List<IBroadcastText>(); // broadcasts are too big
        // try
        // {
        //     var result = await client.GetStringAsync(Path.Join(URL, "GetBroadcastTextsAsync"));
        //     return JsonConvert.DeserializeObject<List<JsonBroadcastText>>(result) ?? new List<JsonBroadcastText>();
        // }
        // catch (Exception e)
        // {
        //     LOG.LogError(e);
        //     return new List<JsonBroadcastText>();
        // }
    }

    public async Task<IReadOnlyList<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureTextsByEntryAsync", entry.ToString()));
            return JsonConvert.DeserializeObject<List<JsonCreatureText>>(result) ?? new List<JsonCreatureText>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureText>();
        }
    }

    public async Task<IReadOnlyList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetLinesCallingSmartTimedActionList", timedActionList.ToString()));
            return JsonConvert.DeserializeObject<List<JsonSmartScriptLine>>(result) ?? new List<JsonSmartScriptLine>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSmartScriptLine>();
        }
    }

    public async Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetScriptForAsync", entry.ToString(), entryOrGuid.ToString(), ((int)type).ToString()));
            return JsonConvert.DeserializeObject<List<JsonSmartScriptLine>>(result) ?? new List<JsonSmartScriptLine>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSmartScriptLine>();
        }
    }

    public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(int sourceType, int sourceEntry, int sourceId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetConditionsForAsync", sourceType.ToString(), sourceEntry.ToString(), sourceId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonConditionLine>>(result) ?? new List<JsonConditionLine>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonConditionLine>();
        }
    }

    public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey manualKey)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetConditionsForAsync", keyMask.ToString(), manualKey.ToString()));
            return JsonConvert.DeserializeObject<List<JsonConditionLine>>(result) ?? new List<JsonConditionLine>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonConditionLine>();
        }
    }

    public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetConditionsForAsync", keyMask.ToString(), JsonConvert.SerializeObject(manualKeys)));
            return JsonConvert.DeserializeObject<List<JsonConditionLine>>(result) ?? new List<JsonConditionLine>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonConditionLine>();
        }
    }

    public async Task<IReadOnlyList<ISpellScriptName>> GetSpellScriptNames(int spellId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSpellScriptNames", spellId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonSpellScriptName>>(result) ?? new List<JsonSpellScriptName>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSpellScriptName>();
        }
    }

    public async Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSmartScriptEntriesByType", ((int)scriptType).ToString()));
            return JsonConvert.DeserializeObject<List<int>>(result) ?? new List<int>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<int>();
        }
    }

    public async Task<IReadOnlyList<IPlayerChoice>?> GetPlayerChoicesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetPlayerChoicesAsync"));
            return JsonConvert.DeserializeObject<List<JsonPlayerChoice>>(result) ?? new List<JsonPlayerChoice>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonPlayerChoice>();
        }
    }

    public async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetPlayerChoiceResponsesAsync"));
            return JsonConvert.DeserializeObject<List<JsonPlayerChoiceResponse>>(result) ?? new List<JsonPlayerChoiceResponse>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonPlayerChoiceResponse>();
        }
    }

    public async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetPlayerChoiceResponsesAsync", choiceId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonPlayerChoiceResponse>>(result) ?? new List<JsonPlayerChoiceResponse>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonPlayerChoiceResponse>();
        }
    }

    public async Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetBroadcastTextByTextAsync", text));
            return JsonConvert.DeserializeObject<JsonBroadcastText>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetBroadcastTextByIdAsync", id.ToString()));
            return JsonConvert.DeserializeObject<JsonBroadcastText>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetBroadcastTextLocaleByTextAsync", text));
            return JsonConvert.DeserializeObject<JsonBroadcastTextLocale>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<ICreature>> GetCreaturesByEntryAsync(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreaturesByEntryAsync", entry.ToString()));
            return JsonConvert.DeserializeObject<List<JsonCreatureWrath>>(result) ?? new List<JsonCreatureWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureWrath>();
        }
    }

    public async Task<IReadOnlyList<IGameObject>> GetGameObjectsByEntryAsync(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameObjectsByEntryAsync", entry.ToString()));
            return JsonConvert.DeserializeObject<List<JsonGameObjectWrath>>(result) ?? new List<JsonGameObjectWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameObjectWrath>();
        }
    }

    public async Task<IReadOnlyList<ICreature>> GetCreaturesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreaturesAsync"));
            return JsonConvert.DeserializeObject<List<JsonCreatureWrath>>(result) ?? new List<JsonCreatureWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureWrath>();
        }
    }

    public async Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameObjectsAsync"));
            return JsonConvert.DeserializeObject<List<JsonGameObjectWrath>>(result) ?? new List<JsonGameObjectWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameObjectWrath>();
        }
    }

    public async Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreaturesAsync", string.Join(",", guids.Select(pair => $"{pair.Entry}:{pair.Guid}"))));
            return JsonConvert.DeserializeObject<List<JsonCreatureWrath>>(result) ?? new List<JsonCreatureWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureWrath>();
        }
    }

    public async Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameObjectsAsync", string.Join(",", guids.Select(pair => $"{pair.Entry}:{pair.Guid}"))));
            return JsonConvert.DeserializeObject<List<JsonGameObjectWrath>>(result) ?? new List<JsonGameObjectWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameObjectWrath>();
        }
    }

    public async Task<IReadOnlyList<ICreature>> GetCreaturesByMapAsync(int map)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreaturesByMapAsync", map.ToString()));
            return JsonConvert.DeserializeObject<List<JsonCreatureWrath>>(result) ?? new List<JsonCreatureWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureWrath>();
        }
    }

    public async Task<IReadOnlyList<IGameObject>> GetGameObjectsByMapAsync(int map)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameObjectsByMapAsync", map.ToString()));
            return JsonConvert.DeserializeObject<List<JsonGameObjectWrath>>(result) ?? new List<JsonGameObjectWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameObjectWrath>();
        }
    }

    public async Task<IReadOnlyList<ITrinityString>> GetStringsAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetStringsAsync"));
            return JsonConvert.DeserializeObject<List<JsonTrinityString>>(result) ?? new List<JsonTrinityString>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonTrinityString>();
        }
    }

    public async Task<IReadOnlyList<IDatabaseSpellDbc>> GetSpellDbcAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSpellDbcAsync"));
            return JsonConvert.DeserializeObject<List<TrinityJsonSpellDbc>>(result) ?? new List<TrinityJsonSpellDbc>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<TrinityJsonSpellDbc>();
        }
    }

    public async Task<IReadOnlyList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSpawnGroupTemplatesAsync"));
            return JsonConvert.DeserializeObject<List<JsonSpawnGroupTemplate>>(result) ?? new List<JsonSpawnGroupTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSpawnGroupTemplate>();
        }
    }

    public async Task<IReadOnlyList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSpawnGroupSpawnsAsync"));
            return JsonConvert.DeserializeObject<List<JsonSpawnGroupSpawn>>(result) ?? new List<JsonSpawnGroupSpawn>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSpawnGroupSpawn>();
        }
    }

    public async Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSpawnGroupTemplateByIdAsync", id.ToString()));
            return JsonConvert.DeserializeObject<JsonSpawnGroupTemplate>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSpawnGroupSpawnByGuidAsync", guid.ToString(), ((int)type).ToString()));
            return JsonConvert.DeserializeObject<JsonSpawnGroupSpawn>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSpawnGroupFormation", id.ToString()));
            return JsonConvert.DeserializeObject<JsonSpawnGroupFormation>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<ISpawnGroupFormation>?> GetSpawnGroupFormations()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSpawnGroupFormations"));
            return JsonConvert.DeserializeObject<List<JsonSpawnGroupFormation>>(result) ?? new List<JsonSpawnGroupFormation>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSpawnGroupFormation>();
        }
    }

    public async Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "FindSmartScriptLinesBy", string.Join(",", conditions.Select(c => $"{(int)c.what}:{c.whatValue}:{c.parameterIndex}:{c.valueToSearch}"))));
            return JsonConvert.DeserializeObject<List<JsonSmartScriptLine>>(result) ?? new List<JsonSmartScriptLine>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSmartScriptLine>();
        }
    }

    public async Task<IReadOnlyList<ICreatureModelInfo>> GetCreatureModelInfoAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureModelInfoAsync"));
            return JsonConvert.DeserializeObject<List<JsonCreatureModelInfo>>(result) ?? new List<JsonCreatureModelInfo>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureModelInfo>();
        }
    }

    public async Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSceneTemplateAsync", sceneId.ToString()));
            return JsonConvert.DeserializeObject<JsonSceneTemplate>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<ISceneTemplate>?> GetSceneTemplatesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSceneTemplatesAsync"));
            return JsonConvert.DeserializeObject<List<JsonSceneTemplate>>(result) ?? new List<JsonSceneTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSceneTemplate>();
        }
    }

    public async Task<IPhaseName?> GetPhaseNameAsync(uint phaseId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetPhaseNameAsync", phaseId.ToString()));
            return JsonConvert.DeserializeObject<JsonPhaseName>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<IPhaseName>?> GetPhaseNamesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetPhaseNamesAsync"));
            return JsonConvert.DeserializeObject<List<JsonPhaseName>>(result) ?? new List<JsonPhaseName>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonPhaseName>();
        }
    }

    public async Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetNpcSpellClickSpells", creatureId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonNpcSpellClickSpell>>(result) ?? new List<JsonNpcSpellClickSpell>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonNpcSpellClickSpell>();
        }
    }

    public async Task<IReadOnlyList<ICreatureAddon>> GetCreatureAddons()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureAddons"));
            return JsonConvert.DeserializeObject<List<JsonCreatureAddonWrath>>(result) ?? new List<JsonCreatureAddonWrath>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureAddonWrath>();
        }
    }

    public async Task<IReadOnlyList<ICreatureTemplateAddon>> GetCreatureTemplateAddons()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureTemplateAddons"));
            return JsonConvert.DeserializeObject<List<JsonCreatureTemplateAddon>>(result) ?? new List<JsonCreatureTemplateAddon>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureTemplateAddon>();
        }
    }

    public async Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureEquipmentTemplates"));
            return JsonConvert.DeserializeObject<List<JsonCreatureEquipmentTemplate>>(result) ?? new List<JsonCreatureEquipmentTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureEquipmentTemplate>();
        }
    }

    public async Task<IReadOnlyList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetMangosCreatureEquipmentTemplates"));
            return JsonConvert.DeserializeObject<List<JsonMangosCreatureEquipmentTemplate>>(result) ?? new List<JsonMangosCreatureEquipmentTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonMangosCreatureEquipmentTemplate>();
        }
    }

    public async Task<IReadOnlyList<IGameEventCreature>> GetGameEventCreaturesAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameEventCreaturesAsync"));
            return JsonConvert.DeserializeObject<List<JsonGameEventCreature>>(result) ?? new List<JsonGameEventCreature>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameEventCreature>();
        }
    }

    public async Task<IReadOnlyList<IGameEventGameObject>> GetGameEventGameObjectsAsync()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameEventGameObjectsAsync"));
            return JsonConvert.DeserializeObject<List<JsonGameEventGameObject>>(result) ?? new List<JsonGameEventGameObject>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameEventGameObject>();
        }
    }

    public async Task<IReadOnlyList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameEventCreaturesByGuidAsync", entry.ToString(), guid.ToString()));
            return JsonConvert.DeserializeObject<List<JsonGameEventCreature>>(result) ?? new List<JsonGameEventCreature>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameEventCreature>();
        }
    }

    public async Task<IReadOnlyList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameEventGameObjectsByGuidAsync", entry.ToString(), guid.ToString()));
            return JsonConvert.DeserializeObject<List<JsonGameEventGameObject>>(result) ?? new List<JsonGameEventGameObject>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameEventGameObject>();
        }
    }

    public async Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureEquipmentTemplates", entry.ToString()));
            return JsonConvert.DeserializeObject<List<JsonCreatureEquipmentTemplate>>(result) ?? new List<JsonCreatureEquipmentTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCreatureEquipmentTemplate>();
        }
    }

    public async Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameObjectByGuidAsync", entry.ToString(), guid.ToString()));
            return JsonConvert.DeserializeObject<JsonGameObjectWrath>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreaturesByGuidAsync", entry.ToString(), guid.ToString()));
            return JsonConvert.DeserializeObject<JsonCreatureWrath>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureAddon", entry.ToString(), guid.ToString()));
            return JsonConvert.DeserializeObject<JsonCreatureAddonWrath>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureTemplateAddon", entry.ToString()));
            return JsonConvert.DeserializeObject<JsonCreatureTemplateAddon>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetWaypointData", pathId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonWaypointData>>(result) ?? new List<JsonWaypointData>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonWaypointData>();
        }
    }

    public async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count = 1)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSmartScriptWaypoints", pathId.ToString(), count.ToString()));
            return JsonConvert.DeserializeObject<List<JsonSmartScriptWaypoint>>(result) ?? new List<JsonSmartScriptWaypoint>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSmartScriptWaypoint>();
        }
    }

    public async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetSmartScriptWaypoints", pathId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonSmartScriptWaypoint>>(result) ?? new List<JsonSmartScriptWaypoint>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonSmartScriptWaypoint>();
        }
    }

    public async Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetScriptWaypoints", pathId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonScriptWaypoint>>(result) ?? new List<JsonScriptWaypoint>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonScriptWaypoint>();
        }
    }

    public async Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetMangosWaypoints", pathId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonMangosWaypoint>>(result) ?? new List<JsonMangosWaypoint>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonMangosWaypoint>();
        }
    }

    public async Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetMangosCreatureMovement", guid.ToString()));
            return JsonConvert.DeserializeObject<List<JsonMangosCreatureMovement>>(result) ?? new List<JsonMangosCreatureMovement>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonMangosCreatureMovement>();
        }
    }

    public async Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetMangosCreatureMovementTemplate", entry.ToString(), pathId?.ToString() ?? ""));
            return JsonConvert.DeserializeObject<List<JsonMangosCreatureMovementTemplate>>(result) ?? new List<JsonMangosCreatureMovementTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonMangosCreatureMovementTemplate>();
        }
    }

    public async Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetMangosPathName", pathId.ToString()));
            return JsonConvert.DeserializeObject<JsonMangosWaypointsPathName>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type, uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetLoot", type.ToString(), entry.ToString()));
            return JsonConvert.DeserializeObject<List<JsonLootEntry>>(result) ?? new List<JsonLootEntry>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonLootEntry>();
        }
    }

    public async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetLoot", type.ToString()));
            return JsonConvert.DeserializeObject<List<JsonLootEntry>>(result) ?? new List<JsonLootEntry>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonLootEntry>();
        }
    }

    public async Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, uint entry)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetLootTemplateName", type.ToString(), entry.ToString()));
            return JsonConvert.DeserializeObject<JsonLootTemplateName>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<ILootTemplateName>> GetLootTemplateName(LootSourceType type)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetLootTemplateName", type.ToString()));
            return JsonConvert.DeserializeObject<List<JsonLootTemplateName>>(result) ?? new List<JsonLootTemplateName>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonLootTemplateName>();
        }
    }

    public async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureLootCrossReference(uint lootId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureLootCrossReference", lootId.ToString()));
            return JsonConvert.DeserializeObject<(List<JsonCreatureTemplateWrath>, List<JsonCreatureTemplateDifficulty>)>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return (new List<JsonCreatureTemplateWrath>(), new List<JsonCreatureTemplateDifficulty>());
        }
    }

    public async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureSkinningLootCrossReference(uint lootId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureSkinningLootCrossReference", lootId.ToString()));
            var x = JsonConvert.DeserializeObject<JsonCreatureTemplateAndDifficulty>(result)!;
            return (x.Templates, x.Difficulties);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return (new List<JsonCreatureTemplateWrath>(), new List<JsonCreatureTemplateDifficulty>());
        }
    }

    public async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreaturePickPocketLootCrossReference(uint lootId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreaturePickPocketLootCrossReference", lootId.ToString()));
            var x = JsonConvert.DeserializeObject<JsonCreatureTemplateAndDifficulty>(result)!;
            return (x.Templates, x.Difficulties);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return (new List<JsonCreatureTemplateWrath>(), new List<JsonCreatureTemplateDifficulty>());
        }
    }

    public async Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectLootCrossReference(uint lootId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetGameObjectLootCrossReference", lootId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonGameObjectTemplate>>(result) ?? new List<JsonGameObjectTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonGameObjectTemplate>();
        }
    }

    public async Task<IReadOnlyList<ILootEntry>> GetReferenceLootCrossReference(uint lootId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetReferenceLootCrossReference", lootId.ToString()));
            return JsonConvert.DeserializeObject<List<JsonLootEntry>>(result) ?? new List<JsonLootEntry>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonLootEntry>();
        }
    }

    public async Task<IReadOnlyList<IConversationActor>> GetConversationActors()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetConversationActors"));
            return JsonConvert.DeserializeObject<List<JsonConversationActor>>(result) ?? new List<JsonConversationActor>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonConversationActor>();
        }
    }

    public async Task<IReadOnlyList<IConversationActorTemplate>> GetConversationActorTemplates()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetConversationActorTemplates"));
            return JsonConvert.DeserializeObject<List<JsonConversationActorTemplate>>(result) ?? new List<JsonConversationActorTemplate>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonConversationActorTemplate>();
        }
    }

    public async Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureByGuidAsync", entry.ToString(), guid.ToString()));
            return JsonConvert.DeserializeObject<JsonCreatureWrath>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<ICoreCommandHelp>> GetCommands()
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCommands"));
            return JsonConvert.DeserializeObject<List<JsonCoreCommandHelp>>(result) ?? new List<JsonCoreCommandHelp>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<JsonCoreCommandHelp>();
        }
    }

    public async Task<ICreatureModelInfo?> GetCreatureModelInfo(uint displayId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetCreatureModelInfo", displayId.ToString()));
            return JsonConvert.DeserializeObject<JsonCreatureModelInfo>(result);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return null;
        }
    }

    public async Task<IReadOnlyList<IQuestRelation>> GetQuestStarters(uint questId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestStarters", questId.ToString()));
            return (IReadOnlyList<JsonQuestRelation>?)JsonConvert.DeserializeObject<List<JsonQuestRelation>>(result) ?? (IReadOnlyList<JsonQuestRelation>)Array.Empty<IQuestRelation>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return Array.Empty<IQuestRelation>();
        }
    }

    public async Task<IReadOnlyList<IQuestRelation>> GetQuestEnders(uint questId)
    {
        try
        {
            var result = await client.GetStringAsync(Path.Join(URL, "GetQuestEnders", questId.ToString()));
            return (IReadOnlyList<JsonQuestRelation>?)JsonConvert.DeserializeObject<List<JsonQuestRelation>>(result) ?? (IReadOnlyList<JsonQuestRelation>)Array.Empty<IQuestRelation>();
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return Array.Empty<IQuestRelation>();
        }
    }

    public IList<IPhaseName>? GetPhaseNames()
    {
        return new List<IPhaseName>();
    }

    public void ConnectOrThrow()
    {

    }
}