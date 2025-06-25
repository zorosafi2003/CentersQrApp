using CentersBarCode.Services;
using CentersBarCode.ViewModels;
using Microsoft.Extensions.Logging;
using Camera.MAUI;
using Camera.MAUI.ZXingHelper;

namespace CentersBarCode;

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
			})
			 .UseMauiCameraView(); // Register Camera.MAUI

		 // Configure logging
        builder.Services.AddLogging(logging =>
        {
            logging.AddDebug();
        });

		 // Register services
		builder.Services.AddSingleton<MainViewModel>();
		
		// Register GoogleAuthService as both the interface and concrete type
        // This allows injection of either the interface or concrete type
        builder.Services.AddSingleton<GoogleAuthService>();
        builder.Services.AddSingleton<IGoogleAuthService>(sp => sp.GetRequiredService<GoogleAuthService>());
		
		// Register pages
		builder.Services.AddTransient<Views.MainPage>();
		builder.Services.AddTransient<Views.LoginPage>();
		builder.Services.AddTransient<Views.RecordsPage>();
		builder.Services.AddSingleton<Views.SplashScreen>();
		
		// Add essentials for secure storage
		builder.Services.AddSingleton<ISecureStorage>(SecureStorage.Default);

		return builder.Build();
	}
}
