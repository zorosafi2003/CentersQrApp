using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

#if ANDROID
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using CentersBarCode.Platforms.Android;
using Android.Net;
#endif

namespace CentersBarCode.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        // Client ID from your Google Developer Console - make sure this matches your OAuth configuration
        private const string WebClientId = "613797922873-vmu2lko85b1mkv0lno9is9d557ggog4o.apps.googleusercontent.com";
        
        #if ANDROID
        // Sign-in client
        private GoogleSignInClient? _signInClient;
        #endif
        
        // Task completion source for the sign-in operation
        private TaskCompletionSource<AuthResult>? _authTcs;
        
        // Flag to track if sign-in is in progress
        private bool _isSigningIn = false;
        
        public GoogleAuthService()
        {
            #if ANDROID
            InitializeGoogleSignIn();
            #endif
        }
        
        #if ANDROID
        /// <summary>
        /// Check if the device is connected to the internet
        /// </summary>
        private bool IsConnectedToInternet()
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
                
                Debug.WriteLine($"Internet connection check: {isConnected}");
                return isConnected;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking internet connection: {ex.Message}");
                return true; // Assume connected if we can't check
            }
        }

        /// <summary>
        /// Initializes the Google Sign-In client with appropriate options
        /// </summary>
        private void InitializeGoogleSignIn()
        {
            try
            {
                // Verify internet connection first
                if (!IsConnectedToInternet())
                {
                    Debug.WriteLine("No internet connection detected during initialization");
                    return;
                }
                
                // Check if Google Play Services is available
                var availability = GoogleApiAvailability.Instance;
                var resultCode = availability.IsGooglePlayServicesAvailable(Platform.CurrentActivity);
                
                if (resultCode != ConnectionResult.Success)
                {
                    bool isResolvable = availability.IsUserResolvableError(resultCode);
                    Debug.WriteLine($"Google Play Services is not available. Result code: {resultCode}, Resolvable: {isResolvable}");
                    
                    if (isResolvable && Platform.CurrentActivity != null)
                    {
                        // Show dialog to help user resolve the issue
                        availability.GetErrorDialog(Platform.CurrentActivity, resultCode, 9000)?.Show();
                    }
                    return;
                }
                
                // Create Google Sign In configuration with correct settings
                var gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                    .RequestEmail()           // Request email first - basic profile
                    .RequestProfile()          // Request profile
                    .RequestId()              // Request ID
                    .RequestIdToken(WebClientId)   // Request ID token
                    .Build();

                // Get the Google client with the options
                if (Platform.CurrentActivity != null)
                {
                    _signInClient = GoogleSignIn.GetClient(Platform.CurrentActivity, gso);
                    Debug.WriteLine("GoogleSignInClient initialized successfully");
                    
                    // Clear any previous sign-in silently - this helps with account switching issues
                    ClearSignInSilently();
                }
                else
                {
                    Debug.WriteLine("CurrentActivity is null during GoogleSignIn initialization");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Google Sign-In: {ex}");
            }
        }
        
        /// <summary>
        /// Clear sign-in state silently (doesn't show UI)
        /// </summary>
        private void ClearSignInSilently()
        {
            try
            {
                if (_signInClient != null)
                {
                    _signInClient.SignOutAsync().ContinueWith(task => 
                    {
                        if (task.IsCompletedSuccessfully)
                        {
                            Debug.WriteLine("Silent sign-out completed successfully");
                        }
                        else if (task.Exception != null)
                        {
                            Debug.WriteLine($"Silent sign-out error: {task.Exception.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ClearSignInSilently: {ex.Message}");
            }
        }
        #endif
        
        /// <summary>
        /// Initiates the Google Sign-In flow
        /// </summary>
        /// <returns>Authentication result</returns>
        public async Task<AuthResult> SignInWithGoogleAsync()
        {
            // Prevent multiple simultaneous sign-in attempts
            if (_isSigningIn)
            {
                Debug.WriteLine("Sign-in already in progress, returning early");
                return new AuthResult
                {
                    IsSuccessful = false,
                    ErrorMessage = "A sign-in operation is already in progress"
                };
            }
            
            try
            {
                _isSigningIn = true;
                Debug.WriteLine("Starting SignInWithGoogleAsync");
                
                #if ANDROID
                // Verify internet connection first
                if (!IsConnectedToInternet())
                {
                    Debug.WriteLine("No internet connection detected");
                    return new AuthResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = "No internet connection. Please check your network settings and try again."
                    };
                }
                
                // Check if Google Play Services is available
                if (Platform.CurrentActivity != null)
                {
                    var availability = GoogleApiAvailability.Instance;
                    var resultCode = availability.IsGooglePlayServicesAvailable(Platform.CurrentActivity);
                    
                    if (resultCode != ConnectionResult.Success)
                    {
                        bool isResolvable = availability.IsUserResolvableError(resultCode);
                        Debug.WriteLine($"Google Play Services is not available. Result code: {resultCode}, Resolvable: {isResolvable}");
                        
                        if (isResolvable)
                        {
                            // Show dialog to help user resolve the issue
                            availability.GetErrorDialog(Platform.CurrentActivity, resultCode, 9000)?.Show();
                        }
                        
                        return new AuthResult
                        {
                            IsSuccessful = false,
                            ErrorMessage = "Google Play Services is not available on this device."
                        };
                    }
                }
                
                // Reinitialize client if needed
                if (_signInClient == null && Platform.CurrentActivity != null)
                {
                    Debug.WriteLine("Reinitializing Google Sign-In client");
                    InitializeGoogleSignIn();
                    
                    if (_signInClient == null)
                    {
                        return new AuthResult
                        {
                            IsSuccessful = false,
                            ErrorMessage = "Could not initialize Google Sign-In. Please try again."
                        };
                    }
                }
                #endif
                
                // Create a new task completion source before any async operations
                _authTcs = new TaskCompletionSource<AuthResult>();
                
                #if ANDROID
                // Start the sign-in intent
                if (_signInClient != null && Platform.CurrentActivity != null)
                {
                    try
                    {
                        Debug.WriteLine("Starting Google Sign-In intent");
                        var signInIntent = _signInClient.SignInIntent;
                        
                        // Force a network check before starting the intent
                        if (!IsConnectedToInternet())
                        {
                            Debug.WriteLine("Network connection lost before starting intent");
                            _authTcs.TrySetResult(new AuthResult
                            {
                                IsSuccessful = false,
                                ErrorMessage = "Network connection lost. Please check your internet connection."
                            });
                        }
                        else
                        {
                            Platform.CurrentActivity.StartActivityForResult(signInIntent, GoogleAuthHelper.RC_SIGN_IN);
                            Debug.WriteLine("Google Sign-In intent started successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error starting sign-in intent: {ex}");
                        _authTcs.TrySetResult(new AuthResult 
                        { 
                            IsSuccessful = false, 
                            ErrorMessage = $"Error starting Google Sign-In: {ex.Message}" 
                        });
                    }
                }
                else
                {
                    string errorMessage = _signInClient == null 
                        ? "Google Sign-In client is null" 
                        : "Current activity is null";
                        
                    Debug.WriteLine(errorMessage);
                    _authTcs.TrySetResult(new AuthResult 
                    { 
                        IsSuccessful = false, 
                        ErrorMessage = $"Google Sign-In is not properly initialized: {errorMessage}" 
                    });
                }
                #elif IOS || MACCATALYST
                // iOS implementation would go here
                Debug.WriteLine("iOS Google Sign-In not implemented");
                _authTcs.TrySetResult(new AuthResult 
                { 
                    IsSuccessful = false, 
                    ErrorMessage = "Google Sign-In not implemented for iOS yet" 
                });
                #elif WINDOWS
                // Windows implementation would go here
                Debug.WriteLine("Windows Google Sign-In not implemented");
                _authTcs.TrySetResult(new AuthResult 
                { 
                    IsSuccessful = false, 
                    ErrorMessage = "Google Sign-In not implemented for Windows yet" 
                });
                #endif
                
                // Wait for the result with a timeout
                Debug.WriteLine("Waiting for authentication result with timeout");
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2)); // Reduced timeout to 2 minutes
                var completedTask = await Task.WhenAny(_authTcs.Task, timeoutTask);
                
                if (completedTask == timeoutTask)
                {
                    Debug.WriteLine("Google Sign-In timed out");
                    return new AuthResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = "Google Sign-In timed out. Please try again."
                    };
                }
                
                // Get the actual result
                var result = await _authTcs.Task;
                Debug.WriteLine($"Google Sign-In completed: {result.IsSuccessful}, Email: {result.UserEmail ?? "null"}");
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during Google Sign-In: {ex}");
                return new AuthResult
                {
                    IsSuccessful = false,
                    ErrorMessage = $"Error during sign-in: {ex.Message}"
                };
            }
            finally
            {
                _isSigningIn = false;
                Debug.WriteLine("Sign-in operation finished, reset _isSigningIn flag");
            }
        }
        
        /// <summary>
        /// Signs the user out of Google
        /// </summary>
        public async Task SignOutAsync()
        {
            #if ANDROID
            try
            {
                if (_signInClient != null)
                {
                    Debug.WriteLine("Signing out from Google");
                    await _signInClient.SignOutAsync();
                    Debug.WriteLine("Google Sign-Out completed successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Google Sign-Out error: {ex}");
            }
            #endif
            
            await Task.CompletedTask;
        }
        
        #if ANDROID
        /// <summary>
        /// Callback method for successful Google Sign-In
        /// </summary>
        /// <param name="account">The GoogleSignInAccount</param>
        /// <param name="isSuccessful">Whether the sign-in was successful</param>
        public void OnGoogleSignInSuccess(GoogleSignInAccount account, bool isSuccessful)
        {
            if (_authTcs == null)
            {
                Debug.WriteLine("Warning: OnGoogleSignInSuccess called but _authTcs is null");
                return;
            }
            
            if (isSuccessful && account != null)
            {
                Debug.WriteLine($"Google Sign-In successful for: {account.Email}");
                Debug.WriteLine($"ID Token: {(string.IsNullOrEmpty(account.IdToken) ? "null" : "present")}");
                Debug.WriteLine($"Server Auth Code: {(string.IsNullOrEmpty(account.ServerAuthCode) ? "null" : "present")}");
                
                // Create a successful auth result
                var authResult = new AuthResult
                {
                    IsSuccessful = true,
                    UserEmail = account.Email,
                    IdToken = account.IdToken,
                    ServerAuthCode = account.ServerAuthCode
                };
                
                // Complete the task
                _authTcs.TrySetResult(authResult);
            }
            else
            {
                string errorMessage = account == null 
                    ? "Account information could not be retrieved" 
                    : "Sign-in was not successful";
                    
                Debug.WriteLine($"Google Sign-In error: {errorMessage}");
                
                // Handle failure
                var result = new AuthResult
                {
                    IsSuccessful = false,
                    ErrorMessage = errorMessage
                };
                
                _authTcs.TrySetResult(result);
            }
        }
        #endif
        
        /// <summary>
        /// Callback method for Google Sign-In errors
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        public void OnGoogleSignInError(string errorMessage)
        {
            Debug.WriteLine($"Google Sign-In error: {errorMessage}");
            
            if (_authTcs == null)
            {
                Debug.WriteLine("Warning: OnGoogleSignInError called but _authTcs is null");
                return;
            }
            
            var result = new AuthResult
            {
                IsSuccessful = false,
                ErrorMessage = errorMessage ?? "Unknown error occurred"
            };
            
            _authTcs.TrySetResult(result);
        }
    }
}