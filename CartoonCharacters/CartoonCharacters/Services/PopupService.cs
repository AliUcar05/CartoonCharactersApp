using System;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;

namespace CartoonCharacters.Services;

public static class PopupService
{
    private static WindowNotificationManager? _manager;

    public static void Initialize(Window hostWindow)
    {
        _manager = new WindowNotificationManager(hostWindow)
        {
            Position = NotificationPosition.TopRight,
            MaxItems = 3
        };
    }

    public static void Info(string title, string message, int seconds = 4)
    {
        _manager?.Show(new Notification(
            title,
            message,
            NotificationType.Information,
            TimeSpan.FromSeconds(seconds)));
    }

    public static void Success(string title, string message, int seconds = 4)
    {
        _manager?.Show(new Notification(
            title,
            message,
            NotificationType.Success,
            TimeSpan.FromSeconds(seconds)));
    }

    public static void Warning(string title, string message, int seconds = 4)
    {
        _manager?.Show(new Notification(
            title,
            message,
            NotificationType.Warning,
            TimeSpan.FromSeconds(seconds)));
    }

    public static void Error(string title, string message, int seconds = 5)
    {
        _manager?.Show(new Notification(
            title,
            message,
            NotificationType.Error,
            TimeSpan.FromSeconds(seconds)));
    }
}