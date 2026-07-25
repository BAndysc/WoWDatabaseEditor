using Hexa.NET.ImGui;
using TheEngine;
using TheEngine.Interfaces;
using TheEngine.Utils.ImGuiHelper;
using TheMaths;
using WDE.MpqReader;

namespace WDE.MapRenderer.Managers
{
    public class NotificationsCenter
    {
        private readonly IUIManager uiManager;
        private float lastNotificationTime;
        private Utf8NativeString lastNotification;
        private SimpleBox notificationBox;

        public const float Padding = 20;

        public NotificationsCenter(Engine engine, IUIManager uiManager)
        {
            this.uiManager = uiManager;
            this.notificationBox = new SimpleBox(engine, BoxPlacement.ScreenCenter);
        }

        public void ShowMessage(Utf8NativeString message, float time = 4000)
        {
            lastNotification = message;
            lastNotificationTime = time;
        }

        public void RenderGUI(float delta)
        {
            if (!lastNotification.IsNull)
            {
                float t = Math.Min(lastNotificationTime / 2500f, 1);

                var io = ImGui.GetIO();

                var bigBoldFont = io.Fonts.Fonts[2];
                ImGui.PushFont(bigBoldFont, 0);
                notificationBox.Alpha = t * 0.8f;
                notificationBox.Draw(lastNotification.AsSpan());
                ImGui.PopFont();

                lastNotificationTime -= delta;

                if (lastNotificationTime < 0)
                {
                    lastNotification = default;
                }
            }
        }
    }
}