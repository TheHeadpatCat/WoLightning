using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WoLightning.Util;

namespace WoLightning.WoL_Plugin.Util
{
    public class NotificationHandler
    {
        private Plugin Plugin;
        private readonly List<Notification> QueuedNotifications = new();
        private readonly static int MaxQueuedNotifications = 7;

        private readonly static TimeSpan Duration = new(0, 0, 7);
        private readonly static String Title = "Warrior of Lightning";
        private readonly static NotificationType Type = NotificationType.Warning;

        private readonly static TimerPlus RateLimiter = new();
        private readonly static short RateLimitMax = 3;
        private short RateLimit = 0;
        

        public NotificationHandler(Plugin plugin)
        {   
            Plugin = plugin;
            RateLimiter.Interval = 3000;
            RateLimiter.AutoReset = true;
            RateLimiter.Elapsed += LowerLimit;
            RateLimiter.Start();
        }

        ~NotificationHandler() { RateLimiter.Stop(); RateLimiter.Elapsed -= LowerLimit; RateLimiter.Dispose(); }

        
        public void send(string content, string? title, NotificationType? type, TimeSpan? duration)
        {
            if(QueuedNotifications.Count < MaxQueuedNotifications) QueuedNotifications.Add(createTemplate(content, title, type, duration));
            Update();
        }
        public void send(string content) { send(content, null, null, null); }
        public void send(string content, NotificationType? type) { send(content, null, type, null); }
        public void send(string content, string? title) { send(content, title, null, null); }
        public void send(string content, TimeSpan duration) { send(content, null, null, duration); }



        private Notification createTemplate(string content, string? title, NotificationType? type, TimeSpan? duration)
        {
            if (title == null) title = Title;
            if (type == null) type = Type;
            if (duration == null) duration = Duration;
            Notification result = new()
            {
                Content = content,
                Title = title,
                Type = (NotificationType)type,
                InitialDuration = (TimeSpan)duration,
            };
            return result;
        }
        private Notification createTemplate(string content) { return createTemplate(content, null, null, null); }
        private Notification createTemplate(string content, string? title) { return createTemplate(content, title, null, null); }
        private Notification createTemplate(string content, string? title, NotificationType? type) { return createTemplate(content, title, type, null); }



        private void Update()
        {
            if (RateLimit >= RateLimitMax || QueuedNotifications.Count == 0) return;

            Notification notif = QueuedNotifications[0];
            Plugin.NotificationManager.AddNotification(notif);
            QueuedNotifications.RemoveAt(0);
            RateLimit++;
            Update();
        }

        private void LowerLimit(object? sender, ElapsedEventArgs e)
        {
            if (RateLimit > 0) RateLimit--;
            Update();
        }
    }
}
