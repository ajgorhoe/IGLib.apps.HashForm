using IG.App.ViewModel;

namespace IG.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);

		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<MainViewModel>();


        builder.Services.AddTransient<DetailPage>();
        builder.Services.AddTransient<DetailViewModel>();

		// Add some custom services for making UI stuff callable viia ViewModels:
		builder.Services.AddSingleton<IG.App.IClipboardService, IG.App.ClipboardServiceDefault>();
		builder.Services.AddSingleton<IG.App.IAlertService, IG.App.AlertServiceDefault>();

        return builder.Build();
	}
}
