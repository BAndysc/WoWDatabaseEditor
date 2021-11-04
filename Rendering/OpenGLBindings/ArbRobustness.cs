namespace OpenGLBindings
{
    /// <summary>
    /// Not used directly.
    /// </summary>
    public enum ArbRobustness
    {
        /// <summary>
        /// Original was GL_NO_ERROR = 0
        /// </summary>
        NoError = 0,
        /// <summary>
        /// Original was GL_CONTEXT_FLAG_ROBUST_ACCESS_BIT_ARB = 0x00000004
        /// </summary>
        ContextFlagRobustAccessBitArb = 4,
        /// <summary>
        /// Original was GL_LOSE_CONTEXT_ON_RESET_ARB = 0x8252
        /// </summary>
        LoseContextOnResetArb = 33362,
        /// <summary>
        /// Original was GL_GUILTY_CONTEXT_RESET_ARB = 0x8253
        /// </summary>
        GuiltyContextResetArb = 33363,
        /// <summary>
        /// Original was GL_INNOCENT_CONTEXT_RESET_ARB = 0x8254
        /// </summary>
        InnocentContextResetArb = 33364,
        /// <summary>
        /// Original was GL_UNKNOWN_CONTEXT_RESET_ARB = 0x8255
        /// </summary>
        UnknownContextResetArb = 33365,
        /// <summary>
        /// Original was GL_RESET_NOTIFICATION_STRATEGY_ARB = 0x8256
        /// </summary>
        ResetNotificationStrategyArb = 33366,
        /// <summary>
        /// Original was GL_NO_RESET_NOTIFICATION_ARB = 0x8261
        /// </summary>
        NoResetNotificationArb = 33377
    }
}