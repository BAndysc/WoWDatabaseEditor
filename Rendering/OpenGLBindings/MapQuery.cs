namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.GetnMap, GL.GetnMap
    /// </summary>
    public enum MapQuery
    {
        /// <summary>
        /// Original was GL_COEFF = 0x0A00
        /// </summary>
        Coeff = 2560,
        /// <summary>
        /// Original was GL_ORDER = 0x0A01
        /// </summary>
        Order,
        /// <summary>
        /// Original was GL_DOMAIN = 0x0A02
        /// </summary>
        Domain
    }
}