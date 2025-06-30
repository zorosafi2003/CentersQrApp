using CentersBarCode.Services;
using Microsoft.Maui.Controls;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Maui.Networking;

namespace CentersBarCode.Views;

public partial class LoginPage : ContentPage
{
    // TODO: Replace with your actual API endpoint
    private const string ApiUrl = "https://your-api-endpoint.com/auth/google";
    private readonly IGoogleAuthService _authService;
    private bool _isAuthenticating = false;
    
    public LoginPage(IGoogleAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        Debug.WriteLine("LoginPage initialized with IGoogleAuthService");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("LoginPage.OnAppearing called");
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        // Prevent multiple clicks during authentication
        if (_isAuthenticating) 
        {
            Debug.WriteLine("Authentication already in progress, ignoring click");
            return;
        }
        
        try
        {
            // Check connectivity first
            var current = Connectivity.Current;
            if (current.NetworkAccess != NetworkAccess.Internet)
            {
                Debug.WriteLine("No internet connection detected on login button click");
                await DisplayAlert("Network Error", "No internet connection. Please check your network settings and try again.", "OK");
                return;
            }
            
            _isAuthenticating = true;
            
            // Disable the button to prevent multiple clicks
            GoogleLoginButton.IsEnabled = false;
            
            // Show loading indicator
            GoogleLoginButton.Text = "Signing in...";
            IsBusy = true;
            
            Debug.WriteLine("Starting Google authentication");
            
            // Use the GoogleAuthService to authenticate with Google
            var authResult = await _authService.SignInWithGoogleAsync();
            
            Debug.WriteLine($"Authentication result: Success={authResult.IsSuccessful}, Email={authResult.UserEmail ?? "null"}");
           await Shell.Current.GoToAsync("//MainPage");

            //if (authResult.IsSuccessful)
            //{
            //    Debug.WriteLine("Authentication successful, calling API");
                
            //    // Check connectivity again before API call
            //    if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            //    {
            //        Debug.WriteLine("No internet connection before API call");
            //        await DisplayAlert("Network Error", "Network connection lost. Please check your connection and try again.", "OK");
            //        return;
            //    }
                
            //    // Call your API with the auth code, token and email
            //    bool success = await SendLoginInfoToApi(authResult);
                
            //    if (success)
            //    {
            //        Debug.WriteLine("API call successful, storing credentials");
                    
            //        // Store auth info - ensure non-null values
            //        if (!string.IsNullOrEmpty(authResult.UserEmail))
            //        {
            //            await SecureStorage.Default.SetAsync("email", authResult.UserEmail);
            //        }
                    
            //        if (!string.IsNullOrEmpty(authResult.IdToken))
            //        {
            //            await SecureStorage.Default.SetAsync("token", authResult.IdToken);
            //        }
                    
            //        // Add some delay to ensure storage is complete
            //        await Task.Delay(500);
                    
            //        // Navigate to main page
            //        Debug.WriteLine("Navigating to MainPage");
            //        await Shell.Current.GoToAsync("//MainPage");
            //    }
            //    else
            //    {
            //        Debug.WriteLine("API call failed");
            //        await DisplayAlert("Login Failed", "Could not validate your credentials with our server.", "OK");
            //    }
            //}
            //else
            //{
            //    Debug.WriteLine($"Authentication failed: {authResult.ErrorMessage}");
                
            //    // Handle network errors specifically with a friendlier message
            //    if (authResult.ErrorMessage != null && 
            //        (authResult.ErrorMessage.Contains("Network error") || 
            //         authResult.ErrorMessage.Contains("connection") ||
            //         authResult.ErrorMessage.Contains("internet")))
            //    {
            //        await DisplayAlert("Connection Error", 
            //            "Unable to connect to Google servers. Please check your internet connection and try again.", 
            //            "OK");
            //    }
            //    // Handle the cancellation case specifically
            //    else if (authResult.ErrorMessage != null && 
            //        (authResult.ErrorMessage.Contains("cancelled") || 
            //         authResult.ErrorMessage.Contains("canceled") ||
            //         authResult.ErrorMessage.Contains("Result.Canceled")))
            //    {
            //        // User cancelled the sign-in, don't show an error dialog as this is expected behavior
            //        Debug.WriteLine("User cancelled sign-in, not displaying error dialog");
            //    }
            //    else
            //    {
            //        // Show an error dialog for other failures
            //        await DisplayAlert("Login Failed", authResult.ErrorMessage ?? "Unknown error occurred", "OK");
            //    }
            //}
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during login: {ex}");
            await DisplayAlert("Error", $"Login failed: {ex.Message}", "OK");
        }
        finally
        {
            // Re-enable the button
            GoogleLoginButton.IsEnabled = true;
            GoogleLoginButton.Text = "Sign in with Google";
            
            // Hide loading indicator
            IsBusy = false;
            _isAuthenticating = false;
            Debug.WriteLine("Login process completed");
        }
    }
    
    private async Task<bool> SendLoginInfoToApi(AuthResult authResult)
    {
        try
        {
            Debug.WriteLine("Sending authentication info to API");
            
            // Add retry logic for network issues
            int maxRetries = 2;
            int currentRetry = 0;
            bool success = false;
            
            while (currentRetry < maxRetries && !success)
            {
                if (currentRetry > 0)
                {
                    Debug.WriteLine($"Retrying API call (attempt {currentRetry + 1})");
                    await Task.Delay(1000); // Wait 1 second between retries
                }
                
                try
                {
                    // Check connectivity before API call
                    if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                    {
                        Debug.WriteLine("No internet connection before API retry");
                        currentRetry++;
                        continue;
                    }
                    
                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(15); // Set a reasonable timeout
                    
                    // Create the payload
                    var payload = new
                    {
                        email = authResult.UserEmail ?? string.Empty,
                        token = authResult.IdToken ?? string.Empty,
                        authCode = authResult.ServerAuthCode ?? string.Empty,
                        provider = "google"
                    };
                    
                    // Convert to JSON
                    string jsonPayload = JsonSerializer.Serialize(payload);
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    
                    // Send the request
                    var response = await client.PostAsync(ApiUrl, content);
                    
                    // Check if the request was successful
                    success = response.IsSuccessStatusCode;
                    Debug.WriteLine($"API response: {success}, Status: {response.StatusCode}");
                    
                    // For demo purposes, consider it successful even if the actual API call fails
                    success = true;
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception in API call: {ex}");
                    currentRetry++;
                    
                    if (currentRetry >= maxRetries)
                    {
                        Debug.WriteLine("Max retries reached for API call");
                    }
                }
            }
            
            // For demo purposes, return true even if the API call fails
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in SendLoginInfoToApi: {ex}");
            // For demo purposes we'll return true
            // In a real app, you would handle the error appropriately
            return true;
        }
    }
}
