namespace WDE.SmartScriptEditor.Models
{
    // this is bad we use hardcoded constants here :/
    public static class SmartConstants
    {
        public const int SourceNone = 0;
        public const int TargetNone = 0;
        public const int ActionNone = 0;
        public const int ActionComment = 9998;
        public const int EventTriggerTimed = 59;
        public const int EventLink = 61;
        public const int ActionTriggerTimed = 67;
        public const int ActionWait = 9999;
        public const string CommentWait = "-meta_wait";
        public const string CommentComment = "-meta_comment";
        public const int ConditionSourceSmartScript = 22;
        public const int ConditionOr = -1;
    }
}