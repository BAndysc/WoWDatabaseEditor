#if USE_OPENTK
using Avalonia.OpenGL.Controls;
using Avalonia.OpenTK;

namespace TheAvaloniaOpenGL
{
    
    public abstract class OpenTKGlControl2 : OpenGlBase2
    {
        public OpenTKGlControl2() : this(new OpenGlControlSettings())
        {
        }
        
        public OpenTKGlControl2(OpenGlControlSettings settings) : base(UpdateSettings(settings))
        {
        }

        private static OpenGlControlSettings UpdateSettings(OpenGlControlSettings settings)
        {
            settings = settings.Clone();
            if (settings.Context == null && settings.ContextFactory == null)
            {
                settings.ContextFactory = () => AvaloniaOpenTKIntegration.CreateCompatibleContext(null);
            }

            return settings;
        }
    }
}
#endif
