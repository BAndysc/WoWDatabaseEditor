using TheMaths;

namespace WDE.MapRenderer.Managers
{
    public class NotificationsCenter
    {
        private readonly IGameContext gameContext;

        private float lastNotificationTime;
        private string? lastNotification;

        private Vector2 boxSize;

        public const float Padding = 20;

        public const string Font = "calibri";
        
        public NotificationsCenter(IGameContext gameContext)
        {
            this.gameContext = gameContext;
        }

        public void ShowMessage(string message, float time = 4000)
        {
            lastNotification = message;
            lastNotificationTime = time;
            boxSize = gameContext.Engine.Ui.MeasureText(Font, lastNotification, 18) + Vector2.One * Padding;
        }

        public void RenderGUI(float delta)
        {
            if (lastNotification != null)
            {
                float t = Math.Min(lastNotificationTime / 2500f, 1);
                
                float x = gameContext.Engine.WindowHost.WindowWidth / 2 - boxSize.X / 2;
                float y = gameContext.Engine.WindowHost.WindowHeight / 2 - boxSize.Y / 2;
                using var ui = gameContext.Engine.Ui.BeginImmediateDraw(x, y);
                
                ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.5f).WithW(0.4f * t), Padding / 2);
                ui.BeginVerticalBox(new Vector4(0, 0, 0, 0.7f).WithW(0.6f * t), Padding / 2);
                ui.Text(Font, lastNotification, 18, Vector4.One.WithW(t));
                ui.EndBox();

                lastNotificationTime -= delta;
                if (lastNotificationTime < 0)
                {
                    lastNotification = null;
                }
            }
        }

        public void Dispose()
        {
            
        }
    }
}