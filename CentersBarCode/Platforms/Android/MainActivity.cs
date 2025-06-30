using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Identity.Client;
using CentersBarCode.Platforms.Android;
using CentersBarCode.Services;
using Microsoft.Maui.Hosting;
using System;
using System.Diagnostics;

namespace CentersBarCode;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    // Store the GoogleAuthService as a field to keep a strong reference
    private GoogleAuthService? _authService;
    
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        System.Diagnostics.Debug.WriteLine("MainActivity.OnCreate called");
        
        // Store the current activity for MSAL authentication
        Platform.Init(this, savedInstanceState);
        
        try
        {
            // Initialize GoogleAuthHelper with GoogleAuthService instance
            _authService = IPlatformApplication.Current?.Services?.GetService<GoogleAuthService>();
            if (_authService != null)
            {
                System.Diagnostics.Debug.WriteLine("Initializing GoogleAuthHelper with GoogleAuthService");
                GoogleAuthHelper.Initialize(_authService);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed to get GoogleAuthService from services");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing services: {ex.Message}");
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("MainActivity.OnResume called");
    }

    protected override void OnPause()
    {
        base.OnPause();
        System.Diagnostics.Debug.WriteLine("MainActivity.OnPause called");
    }

    // Handle the redirect from the authentication flow
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        System.Diagnostics.Debug.WriteLine($"OnActivityResult: requestCode={requestCode}, resultCode={resultCode}, data={data != null}");
        
        try
        {
            // Try to process Google Sign-In result first
            bool handled = GoogleAuthHelper.ProcessActivityResult(requestCode, resultCode, data);
            
            if (handled)
            {
                System.Diagnostics.Debug.WriteLine("Activity result was handled by GoogleAuthHelper");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Activity result was not handled by GoogleAuthHelper, passing to MSAL");
                // If not handled by Google, pass to MSAL
                AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing activity result: {ex}");
        }
        
        // Always call base method
        base.OnActivityResult(requestCode, resultCode, data);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        // Only call this on API 23+ (Android 6.0+)
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
