namespace CentersBarCode;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute("MainPage", typeof(Views.MainPage));
        Routing.RegisterRoute("LoginPage", typeof(Views.LoginPage));
        Routing.RegisterRoute("RecordsPage", typeof(Views.RecordsPage));
	}
}
