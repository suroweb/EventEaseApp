namespace EventEaseApp.Services
{
    public class NotificationService
    {
        public event Action<string, NotificationType>? OnShow;
        public event Action? OnHide;

        public void ShowNotification(string message, NotificationType type)
        {
            OnShow?.Invoke(message, type);
        }

        public void ShowSuccess(string message)
        {
            ShowNotification(message, NotificationType.Success);
        }

        public void ShowError(string message)
        {
            ShowNotification(message, NotificationType.Error);
        }

        public void ShowWarning(string message)
        {
            ShowNotification(message, NotificationType.Warning);
        }

        public void ShowInfo(string message)
        {
            ShowNotification(message, NotificationType.Info);
        }

        public void Hide()
        {
            OnHide?.Invoke();
        }
    }

    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info
    }
}