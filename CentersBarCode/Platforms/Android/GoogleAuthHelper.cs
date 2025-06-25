using System.Threading.Tasks;
using CentersBarCode.Services;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;
using Android.Net;

namespace CentersBarCode.Platforms.Android
{
    public class GoogleAuthHelper
    {
        // Request code used for the sign-in intent
        public const int RC_SIGN_IN = 9001;
        
        // Static reference to the GoogleAuthService
        private static GoogleAuthService? _authService;
        
        /// <summary>
        /// Initializes the GoogleAuthHelper with the GoogleAuthService instance
        /// </summary>
        /// <param name="authService">The GoogleAuthService instance</param>
        public static void Initialize(GoogleAuthService authService)
        {
            _authService = authService;
            Debug.WriteLine("GoogleAuthHelper initialized with GoogleAuthService");
        }
        
        /// <summary>
        /// Check if the device is connected to the internet
        /// </summary>
        private static bool IsConnectedToInternet()
        {
            try
            {
                if (Platform.CurrentActivity == null)
                    return false;
                
                ConnectivityManager? connectivityManager = Platform.CurrentActivity.GetSystemService(global::Android.Content.Context.ConnectivityService) as ConnectivityManager;
                if (connectivityManager == null)
                    return false;
                
                // For Android 6.0+
                NetworkInfo? activeNetwork = connectivityManager.ActiveNetworkInfo;
                bool isConnected = activeNetwork != null && activeNetwork.IsConnected;
                
                Debug.WriteLine($"Internet connection check in GoogleAuthHelper: {isConnected}");
                return isConnected;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error checking internet connection in GoogleAuthHelper: {ex.Message}");
                return true; // Assume connected if we can't check
            }
        }
        
        /// <summary>
        /// Processes the activity result from the sign-in intent
        /// </summary>
        /// <param name="requestCode">The request code</param>
        /// <param name="resultCode">The result code</param>
        /// <param name="data">The intent data</param>
        /// <returns>True if the result was handled, false otherwise</returns>
        public static bool ProcessActivityResult(int requestCode, global::Android.App.Result resultCode, global::Android.Content.Intent? data)
        {
            Debug.WriteLine($"ProcessActivityResult: requestCode={requestCode}, resultCode={resultCode}");
            
            // Only handle the Google Sign-In request code
            if (requestCode == RC_SIGN_IN)
            {
                Debug.WriteLine("Handling Google Sign-In activity result");
                
                // Check if the auth service is available
                if (_authService == null)
                {
                    Debug.WriteLine("ERROR: GoogleAuthService is null, cannot process sign-in result");
                    return true; // We still handled it, even though we couldn't process it properly
                }
                
                // Verify internet connection
                if (!IsConnectedToInternet())
                {
                    Debug.WriteLine("No internet connection detected when processing activity result");
                    _authService.OnGoogleSignInError("Network error. Please check your internet connection and try again.");
                    return true;
                }
                
                // Handle the Google Sign-In response even if cancelled
                try
                {
                    // Check if we have valid data
                    if (data != null)
                    {
                        Debug.WriteLine("Getting sign-in account from intent");
                        
                        try
                        {
                            // Try to get the sign-in result directly to check for API exceptions
                            var task = GoogleSignIn.GetSignedInAccountFromIntentAsync(data);
                            
                            // Process task on main thread
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                try
                                {
                                    // Verify internet connection again right before awaiting the task
                                    if (!IsConnectedToInternet())
                                    {
                                        Debug.WriteLine("Network connection lost while processing sign-in");
                                        _authService.OnGoogleSignInError("Network connection lost during sign-in. Please check your connection and try again.");
                                        return;
                                    }
                                    
                                    // Wait for the task to complete with a timeout
                                    var timeoutTask = Task.Delay(15000); // 15 second timeout for account retrieval
                                    var completedTask = await Task.WhenAny(task, timeoutTask);
                                    
                                    if (completedTask == timeoutTask)
                                    {
                                        Debug.WriteLine("Account retrieval timed out");
                                        _authService.OnGoogleSignInError("Account retrieval timed out. Please try again.");
                                        return;
                                    }
                                    
                                    // Get the account
                                    var account = await task;
                                    
                                    if (account != null && !string.IsNullOrEmpty(account.Email))
                                    {
                                        // If we got a valid account, consider it a success
                                        Debug.WriteLine($"Successfully got account: {account.Email}");
                                        _authService.OnGoogleSignInSuccess(account, true);
                                    }
                                    else if (resultCode == global::Android.App.Result.Canceled)
                                    {
                                        // Only report cancellation if we couldn't get an account
                                        Debug.WriteLine("User cancelled the sign-in process");
                                        _authService.OnGoogleSignInError("Sign-in was cancelled. Please try again.");
                                    }
                                    else
                                    {
                                        // Report error if account is null
                                        Debug.WriteLine("Account is null but result code is not Canceled");
                                        _authService.OnGoogleSignInError("Failed to get account information");
                                    }
                                }
                                catch (ApiException apiEx)
                                {
                                    // Handle network errors specifically
                                    if (apiEx.StatusCode == 10)
                                    {
                                        Debug.WriteLine($"Network error during sign-in: {apiEx.Message}");
                                        _authService.OnGoogleSignInError("Network error connecting to Google servers. Please ensure you have a stable internet connection and try again.");
                                    }
                                    else
                                    {
                                        // Handle other API exceptions
                                        Debug.WriteLine($"Google API error: {apiEx.StatusCode} - {apiEx.Message}");
                                        
                                        // Provide a more user-friendly message for common API errors
                                        string errorMessage = apiEx.StatusCode switch
                                        {
                                            // Common status codes and their user-friendly messages
                                            12500 => "Google Play Services is not available on this device.",
                                            12501 => "User cancelled the sign-in.",
                                            _ => $"Google sign-in error: {apiEx.Message}"
                                        };
                                        
                                        _authService.OnGoogleSignInError(errorMessage);
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    // Handle general exceptions
                                    Debug.WriteLine($"Error processing sign-in: {ex}");
                                    _authService.OnGoogleSignInError($"Error processing sign-in: {ex.Message}");
                                }
                            });
                        }
                        catch (ApiException apiEx)
                        {
                            // Handle API exceptions that occur immediately when getting the task
                            if (apiEx.StatusCode == 10)
                            {
                                Debug.WriteLine($"Immediate network error: {apiEx.Message}");
                                _authService.OnGoogleSignInError("Network error connecting to Google. Please check your internet connection and try again.");
                            }
                            else
                            {
                                Debug.WriteLine($"Immediate API error: {apiEx.StatusCode} - {apiEx.Message}");
                                _authService.OnGoogleSignInError($"Google Sign-In error: {apiEx.Message}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            // Handle any other immediate exceptions
                            Debug.WriteLine($"Immediate exception: {ex}");
                            _authService.OnGoogleSignInError($"Error during sign-in: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Report error if data is null
                        Debug.WriteLine("Sign-in intent data was null");
                        _authService.OnGoogleSignInError("Error completing sign-in. Please try again.");
                    }
                }
                catch (System.Exception ex)
                {
                    // Report any exception during processing
                    Debug.WriteLine($"Exception during sign-in processing: {ex}");
                    _authService.OnGoogleSignInError($"Error during sign-in: {ex.Message}");
                }
                
                // We handled the sign-in result
                return true;
            }
            
            // We didn't handle this activity result
            Debug.WriteLine($"Request code {requestCode} doesn't match Google Sign-In code {RC_SIGN_IN}");
            return false;
        }
    }
}