using Microsoft.Maui.Controls;
using System.Diagnostics;

namespace CentersBarCode.Views;

public partial class SplashScreen : ContentPage
{
    public SplashScreen()
    {
        InitializeComponent();
        
        // Navigate to Login page after 5 seconds
        Dispatcher.DispatchAsync(async () => 
        {
            try
            {
                await Task.Delay(5000); // 5 seconds
                
                // Set the main page to AppShell
                if (Application.Current != null)
                {
                    Application.Current.MainPage = new AppShell();
                    
                    // Navigate to login page
                    await Shell.Current.GoToAsync("//LoginPage");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during splash navigation: {ex.Message}");
            }
        });
    }
}