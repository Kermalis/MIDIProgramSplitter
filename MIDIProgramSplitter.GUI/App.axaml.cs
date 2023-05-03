using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace MIDIProgramSplitter.GUI;

internal sealed partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var mw = new MainWindow();
			mw.HandleArgs(desktop.Args!);
			desktop.MainWindow = mw;
		}

		base.OnFrameworkInitializationCompleted();
	}
}