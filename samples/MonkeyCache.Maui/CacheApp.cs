using Microsoft.Maui;
using Microsoft.Maui.Controls;
using MonkeyCache.FileStore;

namespace MonkeyCache.Samples.Maui;

public sealed class CacheApp : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var results = new Label { Text = "Run the example to add, read and expire cached data." };
        var run = new Button { Text = "Run cache example" };
        run.Clicked += (_, _) => results.Text = string.Join(Environment.NewLine, CacheWalkthrough.Run(Barrel.Current));
        return new Window(new ContentPage
        {
            Title = "Monkey Cache",
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24),
                Spacing = 12,
                Children = { run, results }
            }
        });
    }
}
