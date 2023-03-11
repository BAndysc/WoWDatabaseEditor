using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Common.Services;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.DbcStore.Spells;

[AutoRegister]
[SingleInstance]
public class SpellFindAnywhereSource : IFindAnywhereSource
{
    private readonly Lazy<IDbcSpellService> spellServiceLazy;
    private readonly Lazy<ISpellStore> spellStoreLazy;
    private readonly Lazy<IMessageBoxService> messageBoxService;
    private readonly Lazy<IMainThread> mainThreadLazy;

    public int Order => int.MaxValue;
    
    public FindAnywhereSourceType SourceType => FindAnywhereSourceType.Dbc;

    public SpellFindAnywhereSource(Lazy<IDbcSpellService> spellService,
        Lazy<ISpellStore> spellStore,
        Lazy<IMessageBoxService> messageBoxService,
        Lazy<IMainThread> mainThreadLazy)
    {
        this.spellServiceLazy = spellService;
        this.spellStoreLazy = spellStore;
        this.messageBoxService = messageBoxService;
        this.mainThreadLazy = mainThreadLazy;
    }
    
    public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType, IReadOnlyList<string> parameterNames, long parameterValue, CancellationToken cancellationToken)
    {
        var command = new AsyncAutoCommand(() => messageBoxService.Value.ShowDialog(new MessageBoxFactory<bool>()
            .SetTitle("DBC entry")
            .SetMainInstruction("This is a DBC spell")
            .SetContent("You cannot open it, it is just an info for you that such spell exists")
            .WithButton("Understood!", true, true, true)
            .Build()));
        
        var spellService = spellServiceLazy.Value;
        var spellStore = spellStoreLazy.Value;
        var mainThread = mainThreadLazy.Value;

        void AddThing(uint spell, string name, string description, ImageUri icon)
        {
            mainThread.Dispatch(() =>
            {
                resultContext.AddResult(new FindAnywhereResult(icon,
                    spell,
                    name,
                    description,
                    null,
                    command));
            });
        }
        
        if (parameterNames.IndexOf("SpellParameter") != -1)
        {
            if (spellStore.HasSpell((uint)parameterValue))
            {
                AddThing((uint)parameterValue, spellStore.GetName((uint)parameterValue) ?? "(unknown)", spellService.GetDescription((uint)parameterValue) ?? "Spell in DBC", new ImageUri("Icons/document_instance_template_big.png"));
            }
        }

        await Task.Run(() =>
        {
            for (int j = 0, count = spellService.SpellCount; j < count; ++j)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var spell = spellService.GetSpellId(j);
                var name = spellService.GetName(spell);
                
                for (int i = 0; i < spellService.GetSpellEffectsCount(spell); ++i)
                {
                    var type = spellService.GetSpellEffectType(spell, i);
                    if (type == SpellEffectType.SendEvent)
                    {
                        if (parameterNames.IndexOf("EventScriptParameter") != -1)
                        {
                            var @event = spellService.GetSpellEffectMiscValueA(spell, i);
                            if (@event == parameterValue)
                            {
                                AddThing(spell, name, $"Effect {i}: SPELL_EFFECT_SEND_EVENT with event {@event}", new ImageUri("Icons/document_instance_template_big.png"));
                            }   
                        }
                    }
                    else if (type == SpellEffectType.Summon)
                    {
                        if (parameterNames.IndexOf("CreatureParameter") != -1)
                        {
                            var entry = spellService.GetSpellEffectMiscValueA(spell, i);
                            if (entry == parameterValue)
                            {
                                AddThing(spell, name, $"Effect {i}: SPELL_EFFECT_SUMMON with npc {entry}", new ImageUri("Icons/document_instance_template_big.png"));
                            }   
                        }
                    }

                    var triggerSpell = spellService.GetSpellEffectTriggerSpell(spell, i);
                    if (triggerSpell == parameterValue && parameterNames.IndexOf("SpellParameter") != -1)
                        AddThing(spell, name, $"Effect {i}: Trigger spell {triggerSpell}", new ImageUri("Icons/document_spell_linked_big.png"));
                }
            }
        }, cancellationToken);
    }
}