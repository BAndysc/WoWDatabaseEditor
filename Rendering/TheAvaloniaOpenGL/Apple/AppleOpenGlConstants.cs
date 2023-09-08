namespace TheAvaloniaOpenGL.Apple;

static class AppleOpenGlConstants
{
    //# /System/Library/Frameworks/AppKit.framework/Headers/NSOpenGL.h
    public static uint NSOpenGLPFAAllRenderers = 1; //   # choose from all available renderers
    public static uint NSOpenGLPFADoubleBuffer = 5; //   # choose a double buffered pixel format
    public static uint NSOpenGLPFAStereo = 6; //   # stereo buffering supported
    public static uint NSOpenGLPFAAuxBuffers = 7; //   # number of aux buffers
    public static uint NSOpenGLPFAColorSize = 8; //   # number of color buffer bits
    public static uint NSOpenGLPFAAlphaSize = 11; //   # number of alpha component bits
    public static uint NSOpenGLPFADepthSize = 12; //   # number of depth buffer bits
    public static uint NSOpenGLPFAStencilSize = 13; //   # number of stencil buffer bits
    public static uint NSOpenGLPFAAccumSize = 14; //   # number of accum buffer bits
    public static uint NSOpenGLPFAMinimumPolicy = 51; //   # never choose smaller buffers than requested
    public static uint NSOpenGLPFAMaximumPolicy = 52; //   # choose largest buffers of type requested
    public static uint NSOpenGLPFAOffScreen = 53; //   # choose an off-screen capable renderer
    public static uint NSOpenGLPFAFullScreen = 54; //   # choose a full-screen capable renderer
    public static uint NSOpenGLPFASampleBuffers = 55; //   # number of multi sample buffers
    public static uint NSOpenGLPFASamples = 56; //   # number of samples per multi sample buffer
    public static uint NSOpenGLPFAAuxDepthStencil = 57; //   # each aux buffer has its own depth stencil
    public static uint NSOpenGLPFAColorFloat = 58; //   # color buffers store floating point pixels
    public static uint NSOpenGLPFAMultisample = 59; //   # choose multisampling
    public static uint NSOpenGLPFASupersample = 60; //   # choose supersampling
    public static uint NSOpenGLPFASampleAlpha = 61; //   # request alpha filtering
    public static uint NSOpenGLPFARendererID = 70; //   # request renderer by ID
    public static uint NSOpenGLPFASingleRenderer = 71; //   # choose a single renderer for all screens
    public static uint NSOpenGLPFANoRecovery = 72; //   # disable all failure recovery systems
    public static uint NSOpenGLPFAAccelerated = 73; //   # choose a hardware accelerated renderer
    public static uint NSOpenGLPFAClosestPolicy = 74; //   # choose the closest color buffer to request
    public static uint NSOpenGLPFARobust = 75; //   # renderer does not need failure recovery
    public static uint NSOpenGLPFABackingStore = 76; //   # back buffer contents are valid after swap
    public static uint NSOpenGLPFAMPSafe = 78; //   # renderer is multi-processor safe
    public static uint NSOpenGLPFAWindow = 80; //   # can be used to render to an onscreen window
    public static uint NSOpenGLPFAMultiScreen = 81; //   # single window can span multiple screens
    public static uint NSOpenGLPFACompliant = 83; //   # renderer is opengl compliant
    public static uint NSOpenGLPFAScreenMask = 84; //   # bit mask of supported physical screens
    public static uint NSOpenGLPFAPixelBuffer = 90; //   # can be used to render to a pbuffer
    public static uint NSOpenGLPFARemotePixelBuffer = 91; //   # can be used to render offline to a pbuffer
    public static uint NSOpenGLPFAAllowOfflineRenderers = 96; // # allow use of offline renderers
    public static uint NSOpenGLPFAAcceleratedCompute = 97; //   # choose a hardware accelerated compute device
    public static uint NSOpenGLPFAOpenGLProfile = 99; //   # specify an OpenGL Profile to use
    public static uint NSOpenGLPFAVirtualScreenCount = 128; //   # number of virtual screens in this format

    public static uint NSOpenGLProfileVersionLegacy = 0x1000; //    # choose a Legacy/Pre-OpenGL 3.0 Implementation
    public static uint NSOpenGLProfileVersion3_2Core = 0x3200; //    # choose an OpenGL 3.2 Core Implementation
    public static uint NSOpenGLProfileVersion4_1Core = 0x4100; //    # choose an OpenGL 4.1 Core Implementation

    public static uint NSOpenGLCPSwapInterval = 222; //

}