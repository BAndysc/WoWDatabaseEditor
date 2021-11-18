namespace OpenGLBindings
{
	/// <summary>
	/// Not used directly.
	/// </summary>
	public enum GraphicsResetStatus
	{
		/// <summary>
		/// Original was GL_NO_ERROR = 0
		/// </summary>
		NoError = 0,
		/// <summary>
		/// Original was GL_GUILTY_CONTEXT_RESET = 0x8253
		/// </summary>
		GuiltyContextReset = 33363,
		/// <summary>
		/// Original was GL_INNOCENT_CONTEXT_RESET = 0x8254
		/// </summary>
		InnocentContextReset = 33364,
		/// <summary>
		/// Original was GL_UNKNOWN_CONTEXT_RESET = 0x8255
		/// </summary>
		UnknownContextReset = 33365
	}
}