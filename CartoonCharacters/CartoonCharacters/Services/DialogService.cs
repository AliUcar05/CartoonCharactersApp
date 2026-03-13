using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace CartoonCharacters.Services;

public static class DialogService
{
    public static async Task ShowMessage(string title, string message)
    {
        if (App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new StackPanel
                {
                    Margin = new Avalonia.Thickness(20),
                    Spacing = 20,
                    Children =
                    {
                        new TextBlock 
                        { 
                            Text = message, 
                            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                            FontSize = 14 
                        },
                        new Button 
                        { 
                            Content = "OK", 
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                            Padding = new Avalonia.Thickness(20, 10)
                        }
                    }
                }
            };

            var okButton = (Button)((StackPanel)dialog.Content).Children[1];
            okButton.Click += (s, e) => dialog.Close();

            await dialog.ShowDialog(desktop.MainWindow);
        }
    }
}