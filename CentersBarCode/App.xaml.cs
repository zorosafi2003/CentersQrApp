namespace CentersBarCode;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
        
        // Start with SplashScreen
        MainPage = new Views.SplashScreen();
	}
}
