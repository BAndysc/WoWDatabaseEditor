using TheEngine.Interfaces;
using TheMaths;

namespace WDE.MapRenderer.Managers
{
    public class NotificationsCenter
    {
        private readonly IUIManager uiManager;
        private float lastNotificationTime;
        private string? lastNotification;

        public const float Padding = 20;

        public const string Font = "calibri";
        
        public NotificationsCenter(IUIManager uiManager)
        {
            this.uiManager = uiManager;
        }

        public void ShowMessage(string message, float time = 4000)
        {
            lastNotification = message;
            lastNotificationTime = time;
        }

        public void RenderGUI(float delta)
        {
            if (lastNotification != null)
            {
                float t = Math.Min(lastNotificationTime / 2500f, 1);
                
                using var ui = uiManager.BeginImmediateDrawRel(0.5f, 0.5f, 0.5f, 0.5f);
                
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
    }
}