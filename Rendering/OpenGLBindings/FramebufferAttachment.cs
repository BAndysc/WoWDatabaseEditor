namespace OpenGLBindings
{
    /// <summary>
    /// Used in GL.Arb.FramebufferTexture, GL.Arb.FramebufferTextureFace and 25 other functions
    /// </summary>
    public enum FramebufferAttachment
    {
        /// <summary>
        /// Original was GL_FRONT_LEFT = 0x0400
        /// </summary>
        FrontLeft = 0x400,
        /// <summary>
        /// Original was GL_FRONT_RIGHT = 0x0401
        /// </summary>
        FrontRight = 1025,
        /// <summary>
        /// Original was GL_BACK_LEFT = 0x0402
        /// </summary>
        BackLeft = 1026,
        /// <summary>
        /// Original was GL_BACK_RIGHT = 0x0403
        /// </summary>
        BackRight = 1027,
        /// <summary>
        /// Original was GL_AUX0 = 0x0409
        /// </summary>
        Aux0 = 1033,
        /// <summary>
        /// Original was GL_AUX1 = 0x040A
        /// </summary>
        Aux1 = 1034,
        /// <summary>
        /// Original was GL_AUX2 = 0x040B
        /// </summary>
        Aux2 = 1035,
        /// <summary>
        /// Original was GL_AUX3 = 0x040C
        /// </summary>
        Aux3 = 1036,
        /// <summary>
        /// Original was GL_COLOR = 0x1800
        /// </summary>
        Color = 6144,
        /// <summary>
        /// Original was GL_DEPTH = 0x1801
        /// </summary>
        Depth = 6145,
        /// <summary>
        /// Original was GL_STENCIL = 0x1802
        /// </summary>
        Stencil = 6146,
        /// <summary>
        /// Original was GL_DEPTH_STENCIL_ATTACHMENT = 0x821A
        /// </summary>
        DepthStencilAttachment = 33306,
        /// <summary>
        /// Original was GL_MAX_COLOR_ATTACHMENTS = 0x8CDF
        /// </summary>
        MaxColorAttachments = 36063,
        /// <summary>
        /// Original was GL_MAX_COLOR_ATTACHMENTS_EXT = 0x8CDF
        /// </summary>
        MaxColorAttachmentsExt = 36063,
        /// <summary>
        /// Original was GL_MAX_COLOR_ATTACHMENTS_NV = 0x8CDF
        /// </summary>
        MaxColorAttachmentsNv = 36063,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT0 = 0x8CE0
        /// </summary>
        ColorAttachment0 = 36064,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT0_EXT = 0x8CE0
        /// </summary>
        ColorAttachment0Ext = 36064,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT0_NV = 0x8CE0
        /// </summary>
        ColorAttachment0Nv = 36064,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT0_OES = 0x8CE0
        /// </summary>
        ColorAttachment0Oes = 36064,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT1 = 0x8CE1
        /// </summary>
        ColorAttachment1 = 36065,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT1_EXT = 0x8CE1
        /// </summary>
        ColorAttachment1Ext = 36065,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT1_NV = 0x8CE1
        /// </summary>
        ColorAttachment1Nv = 36065,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT2 = 0x8CE2
        /// </summary>
        ColorAttachment2 = 36066,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT2_EXT = 0x8CE2
        /// </summary>
        ColorAttachment2Ext = 36066,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT2_NV = 0x8CE2
        /// </summary>
        ColorAttachment2Nv = 36066,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT3 = 0x8CE3
        /// </summary>
        ColorAttachment3 = 36067,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT3_EXT = 0x8CE3
        /// </summary>
        ColorAttachment3Ext = 36067,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT3_NV = 0x8CE3
        /// </summary>
        ColorAttachment3Nv = 36067,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT4 = 0x8CE4
        /// </summary>
        ColorAttachment4 = 36068,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT4_EXT = 0x8CE4
        /// </summary>
        ColorAttachment4Ext = 36068,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT4_NV = 0x8CE4
        /// </summary>
        ColorAttachment4Nv = 36068,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT5 = 0x8CE5
        /// </summary>
        ColorAttachment5 = 36069,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT5_EXT = 0x8CE5
        /// </summary>
        ColorAttachment5Ext = 36069,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT5_NV = 0x8CE5
        /// </summary>
        ColorAttachment5Nv = 36069,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT6 = 0x8CE6
        /// </summary>
        ColorAttachment6 = 36070,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT6_EXT = 0x8CE6
        /// </summary>
        ColorAttachment6Ext = 36070,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT6_NV = 0x8CE6
        /// </summary>
        ColorAttachment6Nv = 36070,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT7 = 0x8CE7
        /// </summary>
        ColorAttachment7 = 36071,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT7_EXT = 0x8CE7
        /// </summary>
        ColorAttachment7Ext = 36071,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT7_NV = 0x8CE7
        /// </summary>
        ColorAttachment7Nv = 36071,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT8 = 0x8CE8
        /// </summary>
        ColorAttachment8 = 36072,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT8_EXT = 0x8CE8
        /// </summary>
        ColorAttachment8Ext = 36072,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT8_NV = 0x8CE8
        /// </summary>
        ColorAttachment8Nv = 36072,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT9 = 0x8CE9
        /// </summary>
        ColorAttachment9 = 36073,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT9_EXT = 0x8CE9
        /// </summary>
        ColorAttachment9Ext = 36073,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT9_NV = 0x8CE9
        /// </summary>
        ColorAttachment9Nv = 36073,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT10 = 0x8CEA
        /// </summary>
        ColorAttachment10 = 36074,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT10_EXT = 0x8CEA
        /// </summary>
        ColorAttachment10Ext = 36074,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT10_NV = 0x8CEA
        /// </summary>
        ColorAttachment10Nv = 36074,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT11 = 0x8CEB
        /// </summary>
        ColorAttachment11 = 36075,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT11_EXT = 0x8CEB
        /// </summary>
        ColorAttachment11Ext = 36075,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT11_NV = 0x8CEB
        /// </summary>
        ColorAttachment11Nv = 36075,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT12 = 0x8CEC
        /// </summary>
        ColorAttachment12 = 36076,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT12_EXT = 0x8CEC
        /// </summary>
        ColorAttachment12Ext = 36076,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT12_NV = 0x8CEC
        /// </summary>
        ColorAttachment12Nv = 36076,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT13 = 0x8CED
        /// </summary>
        ColorAttachment13 = 36077,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT13_EXT = 0x8CED
        /// </summary>
        ColorAttachment13Ext = 36077,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT13_NV = 0x8CED
        /// </summary>
        ColorAttachment13Nv = 36077,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT14 = 0x8CEE
        /// </summary>
        ColorAttachment14 = 36078,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT14_EXT = 0x8CEE
        /// </summary>
        ColorAttachment14Ext = 36078,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT14_NV = 0x8CEE
        /// </summary>
        ColorAttachment14Nv = 36078,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT15 = 0x8CEF
        /// </summary>
        ColorAttachment15 = 36079,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT15_EXT = 0x8CEF
        /// </summary>
        ColorAttachment15Ext = 36079,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT15_NV = 0x8CEF
        /// </summary>
        ColorAttachment15Nv = 36079,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT16 = 0x8CF0
        /// </summary>
        ColorAttachment16 = 36080,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT17 = 0x8CF1
        /// </summary>
        ColorAttachment17 = 36081,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT18 = 0x8CF2
        /// </summary>
        ColorAttachment18 = 36082,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT19 = 0x8CF3
        /// </summary>
        ColorAttachment19 = 36083,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT20 = 0x8CF4
        /// </summary>
        ColorAttachment20 = 36084,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT21 = 0x8CF5
        /// </summary>
        ColorAttachment21 = 36085,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT22 = 0x8CF6
        /// </summary>
        ColorAttachment22 = 36086,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT23 = 0x8CF7
        /// </summary>
        ColorAttachment23 = 36087,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT24 = 0x8CF8
        /// </summary>
        ColorAttachment24 = 36088,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT25 = 0x8CF9
        /// </summary>
        ColorAttachment25 = 36089,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT26 = 0x8CFA
        /// </summary>
        ColorAttachment26 = 36090,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT27 = 0x8CFB
        /// </summary>
        ColorAttachment27 = 36091,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT28 = 0x8CFC
        /// </summary>
        ColorAttachment28 = 36092,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT29 = 0x8CFD
        /// </summary>
        ColorAttachment29 = 36093,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT30 = 0x8CFE
        /// </summary>
        ColorAttachment30 = 36094,
        /// <summary>
        /// Original was GL_COLOR_ATTACHMENT31 = 0x8CFF
        /// </summary>
        ColorAttachment31 = 36095,
        /// <summary>
        /// Original was GL_DEPTH_ATTACHMENT = 0x8D00
        /// </summary>
        DepthAttachment = 36096,
        /// <summary>
        /// Original was GL_DEPTH_ATTACHMENT_EXT = 0x8D00
        /// </summary>
        DepthAttachmentExt = 36096,
        /// <summary>
        /// Original was GL_DEPTH_ATTACHMENT_OES = 0x8D00
        /// </summary>
        DepthAttachmentOes = 36096,
        /// <summary>
        /// Original was GL_STENCIL_ATTACHMENT = 0x8D20
        /// </summary>
        StencilAttachment = 36128,
        /// <summary>
        /// Original was GL_STENCIL_ATTACHMENT_EXT = 0x8D20
        /// </summary>
        StencilAttachmentExt = 36128
    }
}