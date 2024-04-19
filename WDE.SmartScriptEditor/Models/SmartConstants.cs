namespace WDE.SmartScriptEditor.Models
{
    // this is bad we use hardcoded constants here :/
    public static class SmartConstants
    {
        public const int EventFlagActionListWaits = 0x20;
        public const int SourceNone = 0;
        public const int SourceSelf = 1;
        public const int SourceStoredObject = 12;
        public const int TargetNone = 0;
        public const int TargetSelf = 1;
        public const int TargetVictim = 2;
        public const int TargetActionInvoker = 7;
        public const int TargetBattlePet = 70;
        public const int ActionNone = 0;
        public const int ActionTalk = 1;
        public const int ActionCast = 11;
        public const int ActionCallTimedActionList = 80;
        public const int ActionCallRandomTimedActionList = 87;
        public const int ActionCallRandomRangeTimedActionList = 88;
        public const int ActionComment = 9998;
        public const int EventTriggerTimed = 59;
        public const int EventMovementInform = 34;
        public const int EventAiInitialize = 37;
        public const int EventUpdateInCombat = 0;
        public const int EventUpdateOutOfCombat = 1;
        public const int EventWaypointsEnded = 58;
        public const int EventLink = 61;
        public const int ActionStartWaypointsPath = 53;
        public const int ActionCreateTimed = 67;
        public const int ActionMovePoint = 69;
        public const int ActionTriggerTimed = 73;
        public const int ActionRemoveTimed = 74;
        public const int ActionTriggerRandomTimed = 125;
        public const int ActionWait = 9999;
        public const int ActionBeginInlineActionList = 9997;
        public const int ActionAfter = 9996;
        public const int ActionAfterMovement = 9995;
        public const int ActionRepeatTimedActionList = 9994;
        public const int ActionLink = 9993;
        public const int ActionAwaitTimedList = 9992;
        public const string CommentInlineActionList = "-inline";
        public const string CommentInlineMovementActionList = "-inline_wp";
        public const string CommentInlineRepeatActionList = "-inline_repeat";
        public const string CommentWait = "-meta_wait";
        public const string CommentComment = "-meta_comment";
        public const int ConditionSourceSmartScript = 22;
        public const int ConditionOr = -1;
        public const int MovementTypePointMotionType = 8;
        public const int EventFlagNotRepeatable = 1;
        public const int EventGroupBegin = -2;
        public const int EventGroupEnd = -3;
        public const string BeginGroupSeparator = " -//- ";
        public const string BeginGroupText = "#group ";
        public const string EndGroupText = "#endgroup";
    }
}