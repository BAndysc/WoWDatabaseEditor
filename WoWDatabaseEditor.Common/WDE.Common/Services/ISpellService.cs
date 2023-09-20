using System;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [Flags]
    public enum SpellAttr0 : uint
    {
        ProcFailureBurnsCharge = 0x1,
        UsesRangedSlot = 0x2,
        OnNextSwingNoDamage = 0x4,
        DoNotLogImmuneMisses = 0x8,
        IsAbility = 0x10,
        IsTradeSkill = 0x20,
        Passive = 0x40,
        DoNotDisplaySpellBookAuraIconCombatLog = 0x80,
        DoNotLog = 0x100,
        HeldItemOnly = 0x200,
        OnNextSwing = 0x400,
        WearerCastsProcTrigger = 0x800,
        ServerOnly = 0x1000,
        AllowItemSpellInPvP = 0x2000,
        OnlyIndoors = 0x4000,
        OnlyOutdoors = 0x8000,
        NotShapeShifted = 0x10000,
        OnlyStealthed = 0x20000,
        DoNotSheath = 0x40000,
        ScalesWithOrCreatureLevel = 0x80000,
        CancelsAutoAttackCombat = 0x100000,
        NoActiveDefense = 0x200000,
        TrackTargetInCastPlayerOnly = 0x400000,
        AllowCastWhileDead = 0x800000,
        AllowWhileMounted = 0x1000000,
        CooldownOnEvent = 0x2000000,
        AuraIsDeBuff = 0x4000000,
        AllowWhileSitting = 0x8000000,
        NotInCombatOnlyPeaceful = 0x10000000,
        NoImmunities = 0x20000000,
        HeartbeatResist = 0x40000000,
        NoAuraCancel = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr1 : uint
    {
        DismissPetFirst = 0x1,
        UseAllMana = 0x2,
        IsChannelled = 0x4,
        NoRedirection = 0x8,
        NoSkillIncrease = 0x10,
        AllowWhileStealthed = 0x20,
        IsSelfChannelled = 0x40,
        NoReflection = 0x80,
        OnlyPeacefulTargets = 0x100,
        InitiatesCombatEnablesAutoAttack = 0x200,
        NoThreat = 0x400,
        AuraUnique = 0x800,
        FailureBreaksStealth = 0x1000,
        ToggleFarSight = 0x2000,
        TrackTargetInChannel = 0x4000,
        ImmunityPurgesEffect = 0x8000,
        ImmunityToHostileAndFriendlyEffects = 0x10000,
        NoAutoCastAi = 0x20000,
        PreventsAnim = 0x40000,
        ExcludeCaster = 0x80000,
        FinishingMoveDamage = 0x100000,
        ThreatOnlyOnMiss = 0x200000,
        FinishingMoveDuration = 0x400000,
        IgnoreOwnersDeath = 0x800000,
        SpecialSkillUp = 0x1000000,
        AuraStaysAfterCombat = 0x2000000,
        RequireAllTargets = 0x4000000,
        DiscountPowerOnMiss = 0x8000000,
        NoAuraIcon = 0x10000000,
        NameInChannelBar = 0x20000000,
        ComboOnBlockMainlineDispelAllStacks = 0x40000000,
        CastWhenLearned = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr2 : uint
    {
        AllowDeadTarget = 0x1,
        NoShapeShiftUi = 0x2,
        IgnoreLineOfSight = 0x4,
        AllowLowLevelBuff = 0x8,
        UseShapeShiftBar = 0x10,
        AutoRepeat = 0x20,
        CannotCastOnTapped = 0x40,
        DoNotReportSpellFailure = 0x80,
        IncludeInAdvancedCombatLog = 0x100,
        AlwaysCastAsUnit = 0x200,
        SpecialTamingFlag = 0x400,
        NoTargetPerSecondCosts = 0x800,
        ChainFromCaster = 0x1000,
        EnchantOwnItemOnly = 0x2000,
        AllowWhileInvisible = 0x4000,
        DoNotConsumeIfGainedDuringCast = 0x8000,
        NoActivePets = 0x10000,
        DoNotResetCombatTimers = 0x20000,
        NoJumpWhileCastPending = 0x40000,
        AllowWhileNotShapeShiftedCasterForm = 0x80000,
        InitiateCombatPostCastEnablesAutoAttack = 0x100000,
        FailOnAllTargetsImmune = 0x200000,
        NoInitialThreat = 0x400000,
        ProcCooldownOnFailure = 0x800000,
        ItemCastWithOwnerSkill = 0x1000000,
        DontBlockManaRegen = 0x2000000,
        NoSchoolImmunities = 0x4000000,
        IgnoreWeaponsKill = 0x8000000,
        NotAnAction = 0x10000000,
        CantCrit = 0x20000000,
        ActiveThreat = 0x40000000,
        RetainItemCast = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr3 : uint
    {
        PvpEnabling = 0x1,
        NoProcEquipRequirement = 0x2,
        NoCastingBarText = 0x4,
        CompletelyBlocked = 0x8,
        NoResTimer = 0x10,
        NoDurabilityLoss = 0x20,
        NoAvoidance = 0x40,
        DotStackingRule = 0x80,
        OnlyOnPlayer = 0x100,
        NotAProc = 0x200,
        RequiresMainHandWeapon = 0x400,
        OnlyBattlegrounds = 0x800,
        OnlyOnGhosts = 0x1000,
        HideChannelBar = 0x2000,
        HideInRaidFilter = 0x4000,
        NormalRangedAttack = 0x8000,
        SuppressCasterProcs = 0x10000,
        SuppressTargetProcs = 0x20000,
        AlwaysHit = 0x40000,
        InstantTargetProcs = 0x80000,
        AllowAuraWhileDead = 0x100000,
        OnlyProcOutdoors = 0x200000,
        CastingCancelsAutoRepeatMainlineDoNotTriggerTargetStand = 0x400000,
        NoDamageHistory = 0x800000,
        RequiresOffHandWeapon = 0x1000000,
        TreatAsPeriodic = 0x2000000,
        CanProcFromProcs = 0x4000000,
        OnlyProcOnCaster = 0x8000000,
        IgnoreCasterAndTargetRestrictions = 0x10000000,
        IgnoreCasterModifiers = 0x20000000,
        DoNotDisplayRange = 0x40000000,
        NotOnAoeImmune = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr4 : uint
    {
        NoCastLog = 0x1,
        ClassTriggerOnlyOnTarget = 0x2,
        AuraExpiresOffline = 0x4,
        NoHelpfulThreat = 0x8,
        NoHarmfulThreat = 0x10,
        AllowClientTargeting = 0x20,
        CannotBeStolen = 0x40,
        AllowCastWhileCasting = 0x80,
        IgnoreDamageTakenModifiers = 0x100,
        CombatFeedbackWhenUsable = 0x200,
        WeaponSpeedCostScaling = 0x400,
        NoPartialImmunity = 0x800,
        AuraIsBuff = 0x1000,
        DoNotLogCaster = 0x2000,
        ReactiveDamageProc = 0x4000,
        NotInSpellBook = 0x8000,
        NotInArenaOrRatedBattleground = 0x10000,
        IgnoreDefaultArenaRestrictions = 0x20000,
        BouncyChainMissiles = 0x40000,
        AllowProcWhileSitting = 0x80000,
        AuraNeverBounces = 0x100000,
        AllowEnteringArena = 0x200000,
        ProcSuppressSwingAnim = 0x400000,
        SuppressWeaponProcs = 0x800000,
        AutoRangedCombat = 0x1000000,
        OwnerPowerScaling = 0x2000000,
        OnlyFlyingAreas = 0x4000000,
        ForceDisplayCastBar = 0x8000000,
        IgnoreCombatTimer = 0x10000000,
        AuraBounceFailsSpell = 0x20000000,
        Obsolete = 0x40000000,
        UseFacingFromSpell = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr5 : uint
    {
        AllowActionsDuringChannel = 0x1,
        NoReagentCostWithAura = 0x2,
        RemoveEnteringArena = 0x4,
        AllowWhileStunned = 0x8,
        TriggersChanneling = 0x10,
        LimitN = 0x20,
        IgnoreAreaEffectPvpCheck = 0x40,
        NotOnPlayer = 0x80,
        NotOnPlayerControlledNpc = 0x100,
        ExtraInitialPeriod = 0x200,
        DoNotDisplayDuration = 0x400,
        ImpliedTargeting = 0x800,
        MeleeChainTargeting = 0x1000,
        SpellHasteAffectsPeriodic = 0x2000,
        NotAvailableWhileCharmed = 0x4000,
        TreatAsAreaEffect = 0x8000,
        AuraAffectsNotJustReqEquippedItem = 0x10000,
        AllowWhileFleeing = 0x20000,
        AllowWhileConfused = 0x40000,
        AiDoesNotFaceTarget = 0x80000,
        DoNotAttemptAPetReSummonWhenDismounting = 0x100000,
        IgnoreTargetRequirements = 0x200000,
        NotOnTrivial = 0x400000,
        NoPartialResists = 0x800000,
        IgnoreCasterRequirements = 0x1000000,
        AlwaysLineOfSight = 0x2000000,
        AlwaysAoeLineOfSight = 0x4000000,
        NoCasterAuraIcon = 0x8000000,
        NoTargetAuraIcon = 0x10000000,
        AuraUniquePerCaster = 0x20000000,
        AlwaysShowGroundTexture = 0x40000000,
        AddMeleeHitRating = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr6 : uint
    {
        NoCooldownOnTooltip = 0x1,
        DoNotResetCooldownInArena = 0x2,
        NotAnAttack = 0x4,
        CanAssistImmunePc = 0x8,
        IgnoreForModTimeRate = 0x10,
        DoNotConsumeResources = 0x20,
        FloatingCombatTextOnCast = 0x40,
        AuraIsWeaponProc = 0x80,
        DoNotChainToCrowdControlledTargets = 0x100,
        AllowOnCharmedTargets = 0x200,
        NoAuraLog = 0x400,
        NotInRaidInstances = 0x800,
        AllowWhileRidingVehicle = 0x1000,
        IgnorePhaseShift = 0x2000,
        AiPrimaryRangedAttack = 0x4000,
        NoPushback = 0x8000,
        NoJumpPathing = 0x10000,
        AllowEquipWhileCasting = 0x20000,
        OriginateFromController = 0x40000,
        DelayCombatTimerDuringCast = 0x80000,
        AuraIconOnlyForCasterLimit10 = 0x100000,
        ShowMechanicAsCombatText = 0x200000,
        AbsorbCannotBeIgnore = 0x400000,
        TapsImmediately = 0x800000,
        CanTargetUnTargetable = 0x1000000,
        DoesNotResetSwingTimerIfInstant = 0x2000000,
        VehicleImmunityCategory = 0x4000000,
        IgnoreHealingModifiers = 0x8000000,
        DoNotAutoSelectTargetWithInitiatesCombat = 0x10000000,
        IgnoreCasterDamageModifiers = 0x20000000,
        DisableTiedEffectPoints = 0x40000000,
        NoCategoryCooldownMods = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr7 : uint
    {
        AllowSpellReflection = 0x1,
        NoTargetDurationMod = 0x2,
        DisableAuraWhileDead = 0x4,
        DebugSpell = 0x8,
        TreatAsRaidBuff = 0x10,
        CanBeMultiCast = 0x20,
        DonTCauseSpellPushback = 0x40,
        PrepareForVehicleControlEnd = 0x80,
        HordeSpecificSpell = 0x100,
        AllianceSpecificSpell = 0x200,
        DispelRemovesCharges = 0x400,
        CanCauseInterrupt = 0x800,
        CanCauseSilence = 0x1000,
        NoUiNotInterruptible = 0x2000,
        RecastOnReSummon = 0x4000,
        ResetSwingTimerAtSpellStart = 0x8000,
        OnlyInSpellBookUntilLearned = 0x10000,
        DoNotLogPvpKill = 0x20000,
        AttackOnChargeToUnit = 0x40000,
        ReportSpellFailureToUnitTarget = 0x80000,
        NoClientFailWhileStunnedFleeingConfused = 0x100000,
        RetainCooldownThroughLoad = 0x200000,
        IgnoresColdWeatherFlyingRequirement = 0x400000,
        NoAttackDodge = 0x800000,
        NoAttackParry = 0x1000000,
        NoAttackMiss = 0x2000000,
        TreatAsNpcAoe = 0x4000000,
        BypassNoResurrectAura = 0x8000000,
        DoNotCountForPvpScoreboard = 0x10000000,
        ReflectionOnlyDefends = 0x20000000,
        CanProcFromSuppressedTargetProcs = 0x40000000,
        AlwaysCastLog = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr8 : uint
    {
        NoAttackBlock = 0x1,
        IgnoreDynamicObjectCaster = 0x2,
        RemoveOutsideDungeonsAndRaids = 0x4,
        OnlyTargetIfSameCreator = 0x8,
        CanHitAoeUntargetable = 0x10,
        AllowWhileCharmed = 0x20,
        AuraRequiredByClient = 0x40,
        IgnoreSanctuary = 0x80,
        UseTargetSLevelForSpellScaling = 0x100,
        PeriodicCanCrit = 0x200,
        MirrorCreatureName = 0x400,
        OnlyPlayersCanCastThisSpell = 0x800,
        AuraPointsOnClient = 0x1000,
        NotInSpellBookUntilLearned = 0x2000,
        TargetProcsOnCaster = 0x4000,
        RequiresLocationToBeOnLiquidSurface = 0x8000,
        OnlyTargetOwnSummons = 0x10000,
        HasteAffectsDuration = 0x20000,
        IgnoreSpellCastOverrideCost = 0x40000,
        AllowTargetsHiddenBySpawnTracking = 0x80000,
        RequiresEquippedInvTypes = 0x100000,
        NoSummonPlusDestFromClientTargetingPathingRequirement = 0x200000,
        MeleeHasteAffectsPeriodic = 0x400000,
        EnforceInCombatRessurectionLimit = 0x800000,
        HealPrediction = 0x1000000,
        NoLevelUpToast = 0x2000000,
        SkipIsKnownCheck = 0x4000000,
        AiFaceTarget = 0x8000000,
        NotInBattleground = 0x10000000,
        MasteryAffectsPoints = 0x20000000,
        DisplayLargeAuraIconOnUnitFramesBossAura = 0x40000000,
        CanAttackImmunePc = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr9 : uint
    {
        ForceDestLocation = 0x1,
        ModInvisIncludesParty = 0x2,
        OnlyWhenIllegallyMounted = 0x4,
        DoNotLogAuraRefresh = 0x8,
        MissileSpeedIsDelayInSec = 0x10,
        IgnoreTotemRequirementsForCasting = 0x20,
        ItemCastGrantsSkillGain = 0x40,
        DoNotAddToUnlearnList = 0x80,
        CooldownIgnoresRangedWeapon = 0x100,
        NotInArena = 0x200,
        TargetMustBeGrounded = 0x400,
        AllowWhileBanishedAuraState = 0x800,
        FaceUnitTargetUponCompletionOfJumpCharge = 0x1000,
        HasteAffectsMeleeAbilityCastTime = 0x2000,
        IgnoreDefaultRatedBattlegroundRestrictions = 0x4000,
        DoNotDisplayPowerCost = 0x8000,
        NextModalSpellRequiresSameUnitTarget = 0x10000,
        AutoCastOffByDefault = 0x20000,
        IgnoreSchoolLockout = 0x40000,
        AllowDarkSimulacrum = 0x80000,
        AllowCastWhileChanneling = 0x100000,
        SuppressVisualKitErrors = 0x200000,
        SpellCastOverrideInSpellBook = 0x400000,
        JumpChargeNoFacingControl = 0x800000,
        IgnoreCasterHealingModifiers = 0x1000000,
        ProgrammerOnlyDonTConsumeChargeIfItemDeleted = 0x2000000,
        ItemPassiveOnClient = 0x4000000,
        ForceCorpseTarget = 0x8000000,
        CannotKillTarget = 0x10000000,
        LogPassive = 0x20000000,
        NoMovementRadiusBonus = 0x40000000,
        ChannelPersistsOnPetFollow = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr10 : uint
    {
        BypassVisibilityCheck = 0x1,
        IgnorePositiveDamageTakenModifiers = 0x2,
        UsesRangedSlotCosmeticOnly = 0x4,
        DoNotLogFullOverHeal = 0x8,
        NpcKnockBackIgnoreDoors = 0x10,
        ForceNonBinaryResistance = 0x20,
        NoSummonLog = 0x40,
        IgnoreInstanceLockAndFarmLimitOnTeleport = 0x80,
        AreaEffectsUseTargetRadius = 0x100,
        ChargeOrJumpChargeUseAbsoluteSpeed = 0x200,
        ProcCooldownOnAPerTargetBasis = 0x400,
        LockChestAtPrecast = 0x800,
        UseSpellBaseLevelForScaling = 0x1000,
        ResetCooldownUponEndingAnEncounter = 0x2000,
        RollingPeriodic = 0x4000,
        SpellBookHiddenUntilOverridden = 0x8000,
        DefendAgainstFriendlyCast = 0x10000,
        AllowDefenseWhileCasting = 0x20000,
        AllowDefenseWhileChanneling = 0x40000,
        AllowFatalDuelDamage = 0x80000,
        MultiClickGroundTargeting = 0x100000,
        AoeCanHitSummonedInvis = 0x200000,
        AllowWhileStunnedByHorrorMechanic = 0x400000,
        VisibleOnlyToCasterConversationsOnly = 0x800000,
        UpdatePassivesOnApplyOrRemove = 0x1000000,
        NormalMeleeAttack = 0x2000000,
        IgnoreFeignDeath = 0x4000000,
        CasterDeathCancelsPersistentAreaAuras = 0x8000000,
        DoNotLogAbsorb = 0x10000000,
        ThisMountIsNotAtTheAccountLevel = 0x20000000,
        PreventClientCastCancel = 0x40000000,
        EnforceFacingOnPrimaryTargetOnly = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr11 : uint
    {
        LockCasterMovementAndFacingWhileCasting = 0x1,
        DonTCancelWhenAllEffectsAreDisabled = 0x2,
        ScalesWithCastingItemSLevel = 0x4,
        DoNotLogOnLearn = 0x8,
        HideShapeShiftRequirements = 0x10,
        AbsorbFallingDamage = 0x20,
        UnbreakableChannel = 0x40,
        IgnoreCasterSSpellLevel = 0x80,
        TransferMountSpell = 0x100,
        IgnoreSpellCastOverrideShapeShiftRequirements = 0x200,
        NewestExclusiveComplete = 0x400,
        NotInInstances = 0x800,
        Obsolete = 0x1000,
        IgnorePvpPower = 0x2000,
        CanAssistUninteractible = 0x4000,
        CastWhenInitialLoggingIn = 0x8000,
        NotInMythicPlusModeChallengeMode = 0x10000,
        CheaperNpcKnockBack = 0x20000,
        IgnoreCasterAbsorbModifiers = 0x40000,
        IgnoreTargetAbsorbModifiers = 0x80000,
        HideLossOfControlUi = 0x100000,
        AllowHarmfulOnFriendly = 0x200000,
        CheapMissileAoi = 0x400000,
        ExpensiveMissileAoi = 0x800000,
        NoClientFailOnNoPet = 0x1000000,
        AiAttemptCastOnImmunePlayer = 0x2000000,
        AllowWhileStunnedByStunMechanic = 0x4000000,
        DonTCloseLootWindow = 0x8000000,
        HideDamageAbsorbUi = 0x10000000,
        DoNotTreatAsAreaEffect = 0x20000000,
        CheckRequiredTargetAuraByCaster = 0x40000000,
        ApplyZoneAuraSpellToPets = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr12 : uint
    {
        EnableProcsFromSuppressedCasterProcs = 0x1,
        CanProcFromSuppressedCasterProcs = 0x2,
        ShowCooldownAsChargeUp = 0x4,
        NoPvpBattleFatigue = 0x8,
        TreatSelfCastAsReflect = 0x10,
        DoNotCancelAreaAuraOnSpecSwitch = 0x20,
        CooldownOnAuraCancelUntilCombatEnds = 0x40,
        DoNotReApplyAreaAuraIfItPersistsThroughUpdate = 0x80,
        DisplayToastMessage = 0x100,
        ActivePassive = 0x200,
        IgnoreDamageCancelsAuraInterrupt = 0x400,
        FaceDestination = 0x800,
        ImmunityPurgesSpell = 0x1000,
        DoNotLogSpellMiss = 0x2000,
        IgnoreDistanceCheckOnChargeOrJumpChargeDoneTriggerSpell = 0x4000,
        DisableKnownSpellsWhileCharmed = 0x8000,
        IgnoreDamageAbsorb = 0x10000,
        NotInProvingGrounds = 0x20000,
        OverrideDefaultSpellClickRange = 0x40000,
        IsInGameStoreEffect = 0x80000,
        AllowDuringSpellOverride = 0x100000,
        UseFloatValuesForScalingAmounts = 0x200000,
        SuppressToastsOnItemPush = 0x400000,
        TriggerCooldownOnSpellStart = 0x800000,
        NeverLearn = 0x1000000,
        NoDeflect = 0x2000000,
        DeprecatedUseStartOfCastLocationForSpellDest = 0x4000000,
        RecomputeAuraOnMercenaryMode = 0x8000000,
        UseWeightedRandomForFlexMaxTargets = 0x10000000,
        IgnoreResilience = 0x20000000,
        ApplyResilienceToSelfDamage = 0x40000000,
        OnlyProcFromClassAbilities = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr13 : uint
    {
        AllowClassAbilityProcs = 0x1,
        AllowWhileFearedByFearMechanic = 0x2,
        CooldownSharedWithAiGroup = 0x4,
        InterruptsCurrentCast = 0x8,
        PeriodicScriptRunsLate = 0x10,
        RecipeHiddenUntilKnown = 0x20,
        CanProcFromLifeSteal = 0x40,
        NameplatePersonalBuffsOrDeBuffs = 0x80,
        CannotLifeStealOrLeech = 0x100,
        GlobalAura = 0x200,
        NameplateEnemyDeBuffs = 0x400,
        AlwaysAllowPvpFlaggedTarget = 0x800,
        DoNotConsumeAuraStackOnProc = 0x1000,
        DoNotPvpFlagCaster = 0x2000,
        AlwaysRequirePvpTargetMatch = 0x4000,
        DoNotFailIfNoTarget = 0x8000,
        DisplayedOutsideOfSpellBook = 0x10000,
        CheckPhaseOnStringIdResults = 0x20000,
        DoNotEnforceShapeShiftRequirements = 0x40000,
        AuraPersistsThroughTamePet = 0x80000,
        PeriodicRefreshExtendsDuration = 0x100000,
        UseSkillRankAsSpellLevel = 0x200000,
        AuraAlwaysShown = 0x400000,
        UseSpellLevelForItemSquishCompensation = 0x800000,
        ChainByMostHit = 0x1000000,
        DoNotDisplayCastTime = 0x2000000,
        AlwaysAllowNegativeHealingPercentModifiers = 0x4000000,
        DoNotAllowDisableMovementInterrupt = 0x8000000,
        AllowAuraOnLevelScale = 0x10000000,
        RemoveAuraOnLevelScale = 0x20000000,
        RecomputeAuraOnLevelScale = 0x40000000,
        UpdateFallSpeedAfterAuraRemoval = 0x80000000,
    }
    
    [Flags]
    public enum SpellAttr14 : uint
    {
        PreventJumpingDuringPrecast = 0x1,
    }

    [Flags]
    public enum SpellTargetFlags : uint
    {
        None            = 0x00000000,
        Unused1        = 0x00000001,               // not used
        Unit            = 0x00000002,               // pguid
        UnitRaid       = 0x00000004,               // not sent, used to validate target (if raid member)
        UnitParty      = 0x00000008,               // not sent, used to validate target (if party member)
        Item            = 0x00000010,               // pguid
        SourceLocation = 0x00000020,               // pguid, 3 float
        DestLocation   = 0x00000040,               // pguid, 3 float
        UnitEnemy      = 0x00000080,               // not sent, used to validate target (if enemy)
        UnitAlly       = 0x00000100,               // not sent, used to validate target (if ally) - Used by teaching spells
        CorpseEnemy    = 0x00000200,               // pguid
        UnitDead       = 0x00000400,               // not sent, used to validate target (if dead creature)
        Gameobject      = 0x00000800,               // pguid, used with TARGET_GAMEOBJECT_TARGET
        TradeItem      = 0x00001000,               // pguid
        String          = 0x00002000,               // string
        GameobjectItem = 0x00004000,               // not sent, used with TARGET_GAMEOBJECT_ITEM_TARGET
        CorpseAlly     = 0x00008000,               // pguid
        UnitMinipet    = 0x00010000,               // pguid, used to validate target (if non combat pet)
        GlyphSlot      = 0x00020000,               // used in glyph spells
        DestTarget     = 0x00040000,               // sometimes appears with DEST_TARGET spells (may appear or not for a given spell)
        Unused20        = 0x00080000,               // uint32 counter, loop { vec3 - screen position (?), guid }, not used so far
        UnitPassenger  = 0x00100000,               // guessed, used to validate target (if vehicle passenger)
    }
    
    public enum SpellTarget
    {
        NoTarget                                   = 0,
        UnitCaster                          = 1,
        UnitNearbyEnemy                    = 2,
        UnitNearbyParty                    = 3,
        UnitNearbyAlly                     = 4,
        UnitPet                             = 5,
        UnitTargetEnemy                    = 6,
        UnitSrcAreaEntry                  = 7,
        UnitDestAreaEntry                 = 8,
        DestHome                            = 9,
        Unk10                               = 10,
        UnitSrcAreaUnk11                 = 11,
        Unk12                               = 12,
        Unk13                               = 13,
        Unk14                               = 14,
        UnitSrcAreaEnemy                  = 15,
        UnitDestAreaEnemy                 = 16,
        DestDb                              = 17,
        DestCaster                          = 18,
        UnitCasterAreaParty               = 20,
        UnitTargetAlly                     = 21,
        SrcCaster                           = 22,
        GameobjectTarget                    = 23,
        UnitConeEnemy24                   = 24,
        UnitTargetAny                      = 25,
        GameobjectItemTarget               = 26,
        UnitMaster                          = 27,
        DestDynobjEnemy                    = 28,
        DestDynobjAlly                     = 29,
        UnitSrcAreaAlly                   = 30,
        UnitDestAreaAlly                  = 31,
        DestCasterSummon                   = 32, // front left, doesn't use radius
        UnitSrcAreaParty                  = 33,
        UnitDestAreaParty                 = 34,
        UnitTargetParty                    = 35,
        DestCasterUnk36                   = 36,
        UnitLasttargetAreaParty           = 37,
        UnitNearbyEntry                    = 38,
        DestCasterFishing                  = 39,
        GameobjectNearbyEntry              = 40,
        DestCasterFrontRight              = 41,
        DestCasterBackRight               = 42,
        DestCasterBackLeft                = 43,
        DestCasterFrontLeft               = 44,
        UnitTargetChainhealAlly           = 45,
        DestNearbyEntry                    = 46,
        DestCasterFront                    = 47,
        DestCasterBack                     = 48,
        DestCasterRight                    = 49,
        DestCasterLeft                     = 50,
        GameobjectSrcArea                  = 51,
        GameobjectDestArea                 = 52,
        DestTargetEnemy                    = 53,
        UnitConeEnemy54                   = 54,
        DestCasterFrontLeap               = 55, // for a leap spell
        UnitCasterAreaRaid                = 56,
        UnitTargetRaid                     = 57,
        UnitNearbyRaid                     = 58,
        UnitConeAlly                       = 59,
        UnitConeEntry                      = 60,
        UnitTargetAreaRaidClass          = 61,
        Unk62                               = 62,
        DestTargetAny                      = 63,
        DestTargetFront                    = 64,
        DestTargetBack                     = 65,
        DestTargetRight                    = 66,
        DestTargetLeft                     = 67,
        DestTargetFrontRight              = 68,
        DestTargetBackRight               = 69,
        DestTargetBackLeft                = 70,
        DestTargetFrontLeft               = 71,
        DestCasterRandom                   = 72,
        DestCasterRadius                   = 73,
        DestTargetRandom                   = 74,
        DestTargetRadius                   = 75,
        DestChannelTarget                  = 76,
        UnitChannelTarget                  = 77,
        DestDestFront                      = 78,
        DestDestBack                       = 79,
        DestDestRight                      = 80,
        DestDestLeft                       = 81,
        DestDestFrontRight                = 82,
        DestDestBackRight                 = 83,
        DestDestBackLeft                  = 84,
        DestDestFrontLeft                 = 85,
        DestDestRandom                     = 86,
        DestDest                            = 87,
        DestDynobjNone                     = 88,
        DestTraj                            = 89,
        UnitTargetMinipet                  = 90,
        DestDestRadius                     = 91,
        UnitSummoner                        = 92,
        CorpseSrcAreaEnemy                = 93, // NYI
        UnitVehicle                         = 94,
        UnitTargetPassenger                = 95,
        UnitPassenger0                     = 96,
        UnitPassenger1                     = 97,
        UnitPassenger2                     = 98,
        UnitPassenger3                     = 99,
        UnitPassenger4                     = 100,
        UnitPassenger5                     = 101,
        UnitPassenger6                     = 102,
        UnitPassenger7                     = 103,
        UnitConeEnemy104                  = 104,
        UnitUnk105                         = 105, // 1 spell
        DestRandomEntry                  = 106,
        DestAreaEntryExtra                = 107, // not enough info - only generic spells avalible
        GameobjectCone108                  = 108,
        GameobjectCone109                  = 109,
        UnitConeEntry110                  = 110,
        Unk111                              = 111,
        Unk112                              = 112,
        Unk113                              = 113,
        Unk114                              = 114,
        Unk115                              = 115,
        Unk116                              = 116,
        Unk117                              = 117,
        UnitTargetAllyOrRaid             = 118, // If target is in your party or raid, all party and raid members will be affected
        CorpseSrcAreaRaid                 = 119,
        UnitCasterAndSummons              = 120,
        Unk121                              = 121,
        UnitAreaThreatList                = 122, // any unit on threat list
        UnitAreaTapList                   = 123,
        Unk124                              = 124,
        DestCasterGround                   = 125,
        Unk126                              = 126,
        Unk127                              = 127,
        Unk128                              = 128,
        UnitConeEntry129                  = 129,
        Unk130                              = 130,
        DestSummoner                        = 131,
        DestTargetAlly                     = 132,
        UnitLineCasterToDestAlly        = 133,
        UnitLineCasterToDestEnemy       = 134,
        UnitLineCasterToDest             = 135,
        Unk136                              = 136,
        Unk137                              = 137,
        Unk138                              = 138,
        Unk139                              = 139,
        Unk140                              = 140,
        Unk141                              = 141,
        DestNearbyEntryOrSelfPos            = 142,
        Unk143                              = 143,
        Unk144                              = 144,
        DestNearbyGO                        = 145,
        Unk146                              = 146,
        Unk147                              = 147,
        Unk148                              = 148,
        Unk149                              = 149,
        UnitOwnCritter                     = 150, // own battle pet from UNIT_FIELD_CRITTER
        Unk151                              = 151,
        TotalSpellTargets
    }
        
    public enum SpellEffectType
    {
        None = 0,
        InstaKill                          = 1,
        SchoolDamage                      = 2,
        Dummy                              = 3,
        PortalTeleport                    = 4,
        TeleportUnits                     = 5,
        ApplyAura                         = 6,
        EnvironmentalDamage               = 7,
        PowerDrain                        = 8,
        HealthLeech                       = 9,
        Heal                               = 10,
        Bind                               = 11,
        Portal                             = 12,
        RitualBase                        = 13,
        RitualSpecialize                  = 14,
        RitualActivatePortal             = 15,
        QuestComplete                     = 16,
        WeaponDamageNoschool             = 17,
        Resurrect                          = 18,
        AddExtraAttacks                  = 19,
        Dodge                              = 20,
        Evade                              = 21,
        Parry                              = 22,
        Block                              = 23,
        CreateItem                        = 24,
        Weapon                             = 25,
        Defense                            = 26,
        PersistentAreaAura               = 27,
        Summon                             = 28,
        Leap                               = 29,
        Energize                           = 30,
        WeaponPercentDamage              = 31,
        TriggerMissile                    = 32,
        OpenLock                          = 33,
        SummonChangeItem                 = 34,
        ApplyAreaAuraParty              = 35,
        LearnSpell                        = 36,
        SpellDefense                      = 37,
        Dispel                             = 38,
        Language                           = 39,
        DualWield                         = 40,
        Jump                               = 41,
        JumpDest                          = 42,
        TeleportUnitsFaceCaster         = 43,
        SkillStep                         = 44,
        AddHonor                          = 45,
        Spawn                              = 46,
        TradeSkill                        = 47,
        Stealth                            = 48,
        Detect                             = 49,
        TransDoor                         = 50,
        ForceCriticalHit                 = 51,
        GuaranteeHit                      = 52,
        EnchantItem                       = 53,
        EnchantItemTemporary             = 54,
        Tamecreature                       = 55,
        SummonPet                         = 56,
        LearnPetSpell                    = 57,
        WeaponDamage                      = 58,
        CreateRandomItem                 = 59,
        Proficiency                        = 60,
        SendEvent                         = 61,
        PowerBurn                         = 62,
        Threat                             = 63,
        TriggerSpell                      = 64,
        ApplyAreaAuraRaid               = 65,
        CreateManaGem                    = 66,
        HealMaxHealth                    = 67,
        InterruptCast                     = 68,
        Distract                           = 69,
        Pull                               = 70,
        Pickpocket                         = 71,
        AddFarsight                       = 72,
        UntrainTalents                    = 73,
        ApplyGlyph                        = 74,
        HealMechanical                    = 75,
        SummonObjectWild                 = 76,
        ScriptEffect                      = 77,
        Attack                             = 78,
        Sanctuary                          = 79,
        AddComboPoints                   = 80,
        CreateHouse                       = 81,
        BindSight                         = 82,
        Duel                               = 83,
        Stuck                              = 84,
        SummonPlayer                      = 85,
        ActivateObject                    = 86,
        GameobjectDamage                  = 87,
        GameobjectRepair                  = 88,
        GameobjectSetDestructionState   = 89,
        KillCredit                        = 90,
        ThreatAll                         = 91,
        EnchantHeldItem                  = 92,
        ForceDeselect                     = 93,
        SelfResurrect                     = 94,
        Skinning                           = 95,
        Charge                             = 96,
        CastButton                        = 97,
        KnockBack                         = 98,
        Disenchant                         = 99,
        Inebriate                          = 100,
        FeedPet                           = 101,
        DismissPet                        = 102,
        Reputation                         = 103,
        SummonObjectSlot1                = 104,
        SummonObjectSlot2                = 105,
        SummonObjectSlot3                = 106,
        SummonObjectSlot4                = 107,
        DispelMechanic                    = 108,
        ResurrectPet                      = 109,
        DestroyAllTotems                 = 110,
        DurabilityDamage                  = 111,
        ResurrectNew                      = 113,
        AttackMe                          = 114,
        DurabilityDamagePct              = 115,
        SkinPlayerCorpse                 = 116,
        SpiritHeal                        = 117,
        Skill                              = 118,
        ApplyAreaAuraPet                = 119,
        TeleportGraveyard                 = 120,
        NormalizedWeaponDmg              = 121,
        SendTaxi                          = 123,
        PullTowards                       = 124,
        ModifyThreatPercent              = 125,
        StealBeneficialBuff              = 126,
        Prospecting                        = 127,
        ApplyAreaAuraFriend             = 128,
        ApplyAreaAuraEnemy              = 129,
        RedirectThreat                    = 130,
        PlaySound                         = 131,
        PlayMusic                         = 132,
        UnlearnSpecialization             = 133,
        KillCredit2                       = 134,
        CallPet                           = 135,
        HealPct                           = 136,
        EnergizePct                       = 137,
        LeapBack                          = 138,
        ClearQuest                        = 139,
        ForceCast                         = 140,
        ForceCastWithValue              = 141,
        TriggerSpellWithValue           = 142,
        ApplyAreaAuraOwner              = 143,
        KnockBackDest                    = 144,
        PullTowardsDest                  = 145,
        ActivateRune                      = 146,
        QuestFail                         = 147,
        TriggerMissileSpellWithValue   = 148,
        ChargeDest                        = 149,
        QuestStart                        = 150,
        TriggerSpell2                    = 151,
        SummonRafFriend                  = 152,
        CreateTamedPet                   = 153,
        DiscoverTaxi                      = 154,
        TitanGrip                         = 155,
        EnchantItemPrismatic             = 156,
        CreateItem2                      = 157,
        Milling                            = 158,
        AllowRenamePet                   = 159,
        ForceCast2                       = 160,
        TalentSpecCount                  = 161,
        TalentSpecSelect                 = 162,
        RemoveAura                        = 164,
    }

    public interface ISpellService
    {
        bool Exists(uint spellId);
        int SpellCount { get; }
        uint GetSpellId(int index);
        string GetName(uint spellId);
        int GetSpellEffectsCount(uint spellId);
        SpellAuraType GetSpellAuraType(uint spellId, int effectIndex);
        SpellEffectType GetSpellEffectType(uint spellId, int index);
        SpellTargetFlags GetSpellTargetFlags(uint spellId);
        (SpellTarget a, SpellTarget b) GetSpellEffectTargetType(uint spellId, int index);
        event Action<ISpellService>? Changed;
    }
    
    [UniqueProvider]
    public interface IDbcSpellService : ISpellService
    {
        T GetAttributes<T>(uint spellId) where T : unmanaged, Enum;
        uint? GetSkillLine(uint spellId);
        uint? GetSpellFocus(uint spellId);
        TimeSpan? GetSpellCastingTime(uint spellId);
        TimeSpan? GetSpellDuration(uint spellId);
        TimeSpan? GetSpellCategoryRecoveryTime(uint spellId);
        string? GetDescription(uint spellId);
        uint GetSpellEffectMiscValueA(uint spellId, int index);
        uint GetSpellEffectTriggerSpell(uint spellId, int index);
    }

    [UniqueProvider]
    public interface IDatabaseSpellService : ISpellService
    {
    }

    public static class Extensions
    {
        public static int GetAttributeIndex<T>(this T e) where T : Enum
        {
            if (typeof(T) == typeof(SpellAttr0))
                return 0;
            
            if (typeof(T) == typeof(SpellAttr1))
                return 1;

            if (typeof(T) == typeof(SpellAttr2))
                return 2;

            if (typeof(T) == typeof(SpellAttr3))
                return 3;

            if (typeof(T) == typeof(SpellAttr4))
                return 4;

            if (typeof(T) == typeof(SpellAttr5))
                return 5;

            if (typeof(T) == typeof(SpellAttr6))
                return 6;

            if (typeof(T) == typeof(SpellAttr7))
                return 7;

            if (typeof(T) == typeof(SpellAttr8))
                return 8;

            if (typeof(T) == typeof(SpellAttr9))
                return 9;

            if (typeof(T) == typeof(SpellAttr10))
                return 10;

            if (typeof(T) == typeof(SpellAttr11))
                return 11;

            if (typeof(T) == typeof(SpellAttr12))
                return 12;

            if (typeof(T) == typeof(SpellAttr13))
                return 13;

            if (typeof(T) == typeof(SpellAttr14))
                return 14;

            throw new Exception("Not anW spell attribute enum");
        }
    }
}