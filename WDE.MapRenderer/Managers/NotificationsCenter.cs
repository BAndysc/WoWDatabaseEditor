using ImGuiNET;
using TheEngine.Interfaces;
using TheEngine.Utils.ImGuiHelper;
using TheMaths;

namespace WDE.MapRenderer.Managers
{
    public class NotificationsCenter
    {
        private readonly IUIManager uiManager;
        private float lastNotificationTime;
        private string? lastNotification;
        private SimpleBox notificationBox;

        public const float Padding = 20;

        public NotificationsCenter(IUIManager uiManager)
        {
            this.uiManager = uiManager;
            this.notificationBox = new SimpleBox(BoxPlacement.ScreenCenter);
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

                var io = ImGui.GetIO();

                var bigBoldFont = io.Fonts.Fonts[2];
                ImGui.PushFont(bigBoldFont);
                notificationBox.Alpha = t * 0.8f;
                notificationBox.Draw(lastNotification);
                ImGui.PopFont();

                lastNotificationTime -= delta;

                if (lastNotificationTime < 0)
                {
                    lastNotification = null;
                }
            }
        }
    }
}