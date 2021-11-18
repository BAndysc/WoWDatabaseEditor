namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.GetnColorTable, GL.Arb.GetnConvolutionFilter and 58 other functions
    /// </summary>
    public enum PixelType
    {
        /// <summary>
        /// Original was GL_BYTE = 0x1400
        /// </summary>
        Byte = 5120,
        /// <summary>
        /// Original was GL_UNSIGNED_BYTE = 0x1401
        /// </summary>
        UnsignedByte = 5121,
        /// <summary>
        /// Original was GL_SHORT = 0x1402
        /// </summary>
        Short = 5122,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT = 0x1403
        /// </summary>
        UnsignedShort = 5123,
        /// <summary>
        /// Original was GL_INT = 0x1404
        /// </summary>
        Int = 5124,
        /// <summary>
        /// Original was GL_UNSIGNED_INT = 0x1405
        /// </summary>
        UnsignedInt = 5125,
        /// <summary>
        /// Original was GL_FLOAT = 0x1406
        /// </summary>
        Float = 5126,
        /// <summary>
        /// Original was GL_HALF_FLOAT = 0x140B
        /// </summary>
        HalfFloat = 5131,
        /// <summary>
        /// Original was GL_UNSIGNED_BYTE_3_3_2 = 0x8032
        /// </summary>
        UnsignedByte332 = 32818,
        /// <summary>
        /// Original was GL_UNSIGNED_BYTE_3_3_2_EXT = 0x8032
        /// </summary>
        UnsignedByte332Ext = 32818,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033
        /// </summary>
        UnsignedShort4444 = 32819,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT_4_4_4_4_EXT = 0x8033
        /// </summary>
        UnsignedShort4444Ext = 32819,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034
        /// </summary>
        UnsignedShort5551 = 32820,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT_5_5_5_1_EXT = 0x8034
        /// </summary>
        UnsignedShort5551Ext = 32820,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_8_8_8_8 = 0x8035
        /// </summary>
        UnsignedInt8888 = 32821,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_8_8_8_8_EXT = 0x8035
        /// </summary>
        UnsignedInt8888Ext = 32821,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_10_10_10_2 = 0x8036
        /// </summary>
        UnsignedInt1010102 = 32822,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_10_10_10_2_EXT = 0x8036
        /// </summary>
        UnsignedInt1010102Ext = 32822,
        /// <summary>
        /// Original was GL_UNSIGNED_BYTE_2_3_3_REVERSED = 0x8362
        /// </summary>
        UnsignedByte233Reversed = 33634,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT_5_6_5 = 0x8363
        /// </summary>
        UnsignedShort565 = 33635,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT_5_6_5_REVERSED = 0x8364
        /// </summary>
        UnsignedShort565Reversed = 33636,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT_4_4_4_4_REVERSED = 0x8365
        /// </summary>
        UnsignedShort4444Reversed = 33637,
        /// <summary>
        /// Original was GL_UNSIGNED_SHORT_1_5_5_5_REVERSED = 0x8366
        /// </summary>
        UnsignedShort1555Reversed = 33638,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_8_8_8_8_REVERSED = 0x8367
        /// </summary>
        UnsignedInt8888Reversed = 33639,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_2_10_10_10_REVERSED = 0x8368
        /// </summary>
        UnsignedInt2101010Reversed = 33640,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_24_8 = 0x84FA
        /// </summary>
        UnsignedInt248 = 34042,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B
        /// </summary>
        UnsignedInt10F11F11FRev = 35899,
        /// <summary>
        /// Original was GL_UNSIGNED_INT_5_9_9_9_REV = 0x8C3E
        /// </summary>
        UnsignedInt5999Rev = 35902,
        /// <summary>
        /// Original was GL_FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD
        /// </summary>
        Float32UnsignedInt248Rev = 36269
    }
}