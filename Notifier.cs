using System;

namespace BankFrontEnd
{
    public enum NotificationType
    {
        Success,
        Error,
        Info
    }

    public sealed class NotificationMessage
    {
        public NotificationMessage(string message, NotificationType type)
        {
            Message = message;
            Type = type;
        }

        public string Message { get; }
        public NotificationType Type { get; }
    }

    public static class Notifier
    {
        public static event Action<NotificationMessage>? MessageRequested;

        public static void Success(string message) => Raise(message, NotificationType.Success);
        public static void Error(string message) => Raise(message, NotificationType.Error);
        public static void Info(string message) => Raise(message, NotificationType.Info);

        private static void Raise(string message, NotificationType type)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            MessageRequested?.Invoke(new NotificationMessage(message, type));
        }
    }
}
