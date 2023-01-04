using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WDE.Common.Factories;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.RuntimeData;

[AutoRegister(Platforms.Browser)]
[SingleInstance]
public class WebRuntimeDataService : IRuntimeDataService
{
    private static string host = "http://192.168.1.103:5000";
    private HttpClient httpClient;

    public WebRuntimeDataService(IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.Factory();
    }
    
    public async Task<string> ReadAllText(string path)
    {
        try
        {
            var response = await httpClient.GetAsync(Path.Combine(host, path));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            throw new DataMissingException(path, e);
        }
    }

    public async Task<bool> Exists(string path)
    {
        var response = await httpClient.GetAsync(Path.Combine(host, path));
        return response.IsSuccessStatusCode;
    }

    public async Task<byte[]> ReadAllBytes(string path)
    {
        try
        {
            var response = await httpClient.GetAsync(Path.Combine(host, path));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception e)
        {
            throw new DataMissingException(path, e);
        }
    }

    private IEnumerable<string> GetHardcodedFilesFromDirectory(string directory)
    { 
        if (directory.StartsWith("DbDefinitions"))
        {
            yield return "DbDefinitions/access_requirement.json";
            yield return "DbDefinitions/access_requirement_master.json";
            yield return "DbDefinitions/achievement_criteria_data.json";
            yield return "DbDefinitions/achievement_dbc.json";
            yield return "DbDefinitions/achievement_reward.json";
            yield return "DbDefinitions/areatrigger_involvedrelation.json";
            yield return "DbDefinitions/areatrigger_scripts.json";
            yield return "DbDefinitions/areatrigger_tavern.json";
            yield return "DbDefinitions/areatrigger_teleport.json";
            yield return "DbDefinitions/areatrigger_teleport_ats.json";
            yield return "DbDefinitions/battlefield_template.json";
            yield return "DbDefinitions/battleground_template.json";
            yield return "DbDefinitions/battlemaster_entry.json";
            yield return "DbDefinitions/broadcast_text.json";
            yield return "DbDefinitions/creature.json";
            yield return "DbDefinitions/creature_classlevelstats.json";
            yield return "DbDefinitions/creature_default_trainer.json";
            yield return "DbDefinitions/creature_emote_set_ats.json";
            yield return "DbDefinitions/creature_equip_template.json";
            yield return "DbDefinitions/creature_model_info.json";
            yield return "DbDefinitions/creature_movement_override.json";
            yield return "DbDefinitions/creature_onkill_reputation.json";
            yield return "DbDefinitions/creature_questender.json";
            yield return "DbDefinitions/creature_questitem.json";
            yield return "DbDefinitions/creature_queststarter.json";
            yield return "DbDefinitions/creature_spell_ats.json";
            yield return "DbDefinitions/creature_spell_group_ats.json";
            yield return "DbDefinitions/creature_summon_groups.json";
            yield return "DbDefinitions/creature_tcpp.json";
            yield return "DbDefinitions/creature_template.json";
            yield return "DbDefinitions/creature_template_ats.json";
            yield return "DbDefinitions/creature_template_resistance.json";
            yield return "DbDefinitions/creature_text.json";
            yield return "DbDefinitions/disables.json";
            yield return "DbDefinitions/event_zone_template.json";
            yield return "DbDefinitions/exploration_basexp.json";
            yield return "DbDefinitions/game_event.json";
            yield return "DbDefinitions/game_event_arena_seasons.json";
            yield return "DbDefinitions/game_event_battleground_holiday.json";
            yield return "DbDefinitions/game_event_condition.json";
            yield return "DbDefinitions/game_event_creature.json";
            yield return "DbDefinitions/game_event_creature_quest.json";
            yield return "DbDefinitions/game_event_gameobject.json";
            yield return "DbDefinitions/game_event_gameobject_quest.json";
            yield return "DbDefinitions/game_event_model_equip.json";
            yield return "DbDefinitions/game_event_npc_vendor.json";
            yield return "DbDefinitions/game_event_npcflag.json";
            yield return "DbDefinitions/game_event_pool.json";
            yield return "DbDefinitions/game_event_prerequisite.json";
            yield return "DbDefinitions/game_event_quest_condition.json";
            yield return "DbDefinitions/game_event_seasonal_questrelation.json";
            yield return "DbDefinitions/game_tele.json";
            yield return "DbDefinitions/game_weather.json";
            yield return "DbDefinitions/gameobject.json";
            yield return "DbDefinitions/gameobject_overrides.json";
            yield return "DbDefinitions/gameobject_questender.json";
            yield return "DbDefinitions/gameobject_questitem.json";
            yield return "DbDefinitions/gameobject_queststarter.json";
            yield return "DbDefinitions/gameobject_tcpp.json";
            yield return "DbDefinitions/gameobject_template.json";
            yield return "DbDefinitions/gameobject_template_ats.json";
            yield return "DbDefinitions/gossip_menu.json";
            yield return "DbDefinitions/gossip_menu_ac.json";
            yield return "DbDefinitions/gossip_menu_master.json";
            yield return "DbDefinitions/gossip_menu_option.json";
            yield return "DbDefinitions/gossip_menu_option_master.json";
            yield return "DbDefinitions/graveyard_zone.json";
            yield return "DbDefinitions/holiday_dates.json";
            yield return "DbDefinitions/instance_encounters.json";
            yield return "DbDefinitions/instance_spawn_groups.json";
            yield return "DbDefinitions/instance_template.json";
            yield return "DbDefinitions/instance_template_ats.json";
            yield return "DbDefinitions/instance_template_cata.json";
            yield return "DbDefinitions/instance_template_master.json";
            yield return "DbDefinitions/item_enchantment_template.json";
            yield return "DbDefinitions/item_set_names.json";
            yield return "DbDefinitions/lfg_dungeon_rewards.json";
            yield return "DbDefinitions/lfg_dungeon_template.json";
            yield return "DbDefinitions/linked_respawn.json";
            yield return "DbDefinitions/loot_template_creature.json";
            yield return "DbDefinitions/loot_template_disenchant.json";
            yield return "DbDefinitions/loot_template_fishing.json";
            yield return "DbDefinitions/loot_template_gameobject.json";
            yield return "DbDefinitions/loot_template_item.json";
            yield return "DbDefinitions/loot_template_mail.json";
            yield return "DbDefinitions/loot_template_milling.json";
            yield return "DbDefinitions/loot_template_pickpocketing.json";
            yield return "DbDefinitions/loot_template_prospecting.json";
            yield return "DbDefinitions/loot_template_reference.json";
            yield return "DbDefinitions/loot_template_skinning.json";
            yield return "DbDefinitions/loot_template_spell.json";
            yield return "DbDefinitions/mail_level_reward.json";
            yield return "DbDefinitions/milling_loot_template.json";
            yield return "DbDefinitions/npc_spellclick_spells.json";
            yield return "DbDefinitions/npc_text.json";
            yield return "DbDefinitions/page_text.json";
            yield return "DbDefinitions/pet_levelstats.json";
            yield return "DbDefinitions/pet_name_generation.json";
            yield return "DbDefinitions/phase_area.json";
            yield return "DbDefinitions/player_classlevelstats.json";
            yield return "DbDefinitions/player_factionchange_achievement.json";
            yield return "DbDefinitions/player_factionchange_items.json";
            yield return "DbDefinitions/player_factionchange_quests.json";
            yield return "DbDefinitions/player_factionchange_reputations.json";
            yield return "DbDefinitions/player_factionchange_spells.json";
            yield return "DbDefinitions/player_factionchange_titles.json";
            yield return "DbDefinitions/player_levelstats.json";
            yield return "DbDefinitions/player_totem_model.json";
            yield return "DbDefinitions/player_xp_for_level.json";
            yield return "DbDefinitions/playercreateinfo.json";
            yield return "DbDefinitions/playercreateinfo_action.json";
            yield return "DbDefinitions/playercreateinfo_cast_spell.json";
            yield return "DbDefinitions/playercreateinfo_item.json";
            yield return "DbDefinitions/playercreateinfo_skills.json";
            yield return "DbDefinitions/playercreateinfo_spell_custom.json";
            yield return "DbDefinitions/poi.json";
            yield return "DbDefinitions/pool_members.json";
            yield return "DbDefinitions/pool_template.json";
            yield return "DbDefinitions/quest_conditions.json";
            yield return "DbDefinitions/quest_details.json";
            yield return "DbDefinitions/quest_greeting.json";
            yield return "DbDefinitions/quest_mail_sender.json";
            yield return "DbDefinitions/quest_offer_reward.json";
            yield return "DbDefinitions/quest_poi.json";
            yield return "DbDefinitions/quest_poi_points.json";
            yield return "DbDefinitions/quest_pool_members.json";
            yield return "DbDefinitions/quest_pool_template.json";
            yield return "DbDefinitions/quest_request_items.json";
            yield return "DbDefinitions/quest_template.json";
            yield return "DbDefinitions/quest_template_ats.json";
            yield return "DbDefinitions/reputation_reward_rate.json";
            yield return "DbDefinitions/reputation_spillover_template.json";
            yield return "DbDefinitions/script_spline_chain_meta.json";
            yield return "DbDefinitions/script_spline_chain_waypoints.json";
            yield return "DbDefinitions/skill_discovery_template.json";
            yield return "DbDefinitions/skill_extra_item_template.json";
            yield return "DbDefinitions/skill_fishing_base_level.json";
            yield return "DbDefinitions/skill_perfect_item_template.json";
            yield return "DbDefinitions/spawn_group.json";
            yield return "DbDefinitions/spawn_group_template.json";
            yield return "DbDefinitions/spell_area.json";
            yield return "DbDefinitions/spell_area_cata.json";
            yield return "DbDefinitions/spell_bonus_data.json";
            yield return "DbDefinitions/spell_conditions.json";
            yield return "DbDefinitions/spell_group.json";
            yield return "DbDefinitions/spell_group_stack_rules.json";
            yield return "DbDefinitions/spell_implicit_target.json";
            yield return "DbDefinitions/spell_learn_spell.json";
            yield return "DbDefinitions/spell_linked_spell.json";
            yield return "DbDefinitions/spell_override_ats.json";
            yield return "DbDefinitions/spell_pet_auras.json";
            yield return "DbDefinitions/spell_proc.json";
            yield return "DbDefinitions/spell_ranks.json";
            yield return "DbDefinitions/spell_required.json";
            yield return "DbDefinitions/spell_script_names.json";
            yield return "DbDefinitions/spell_target_position.json";
            yield return "DbDefinitions/spell_threat.json";
            yield return "DbDefinitions/trainer.json";
            yield return "DbDefinitions/trainer_spell.json";
            yield return "DbDefinitions/transports.json";
            yield return "DbDefinitions/trinity_string.json";
            yield return "DbDefinitions/vehicle_accessory.json";
            yield return "DbDefinitions/vehicle_seat_addon.json";
            yield return "DbDefinitions/vehicle_template_accessory.json";
            yield return "DbDefinitions/waypoint_scripts.json";
            yield return "DbDefinitions/waypoints.json";
        }
    }
    
    public async Task<IReadOnlyList<string>> GetAllFiles(string directory, string searchPattern)
    { 
        var allFiles = GetHardcodedFilesFromDirectory(directory);
        List<string> results = new List<string>();
        foreach (var file in allFiles)
        {
            if (SearchPatternMatch(Path.GetFileName(file), searchPattern))
                results.Add(file);
        }

        return results;
    }

    private bool SearchPatternMatch(string file, string searchPattern)
    {
        var anyPrefix = searchPattern.StartsWith('*');
        var anySuffix = searchPattern.EndsWith('*');
        if (anyPrefix && anySuffix)
            return file.Contains(searchPattern.Substring(1, searchPattern.Length - 2));
        if (anyPrefix)
            return file.EndsWith(searchPattern.Substring(1));
        if (anySuffix)
            return file.StartsWith(searchPattern.Substring(0, searchPattern.Length - 1));
        return file == searchPattern;
    }
}