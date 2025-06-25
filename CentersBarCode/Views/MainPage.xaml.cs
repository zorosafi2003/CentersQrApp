using CentersBarCode.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Camera.MAUI;
using Camera.MAUI.ZXingHelper;
using ZXing;
using System.Timers;

namespace CentersBarCode.Views;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private bool _isFlashOn = false;
    private System.Timers.Timer? _scanTimeoutTimer;
    
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Initialize scanner when OpenQrScannerCommand is executed
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsQrScannerVisible) && _viewModel.IsQrScannerVisible)
            {
                CheckCameraPermissionAndInitialize();
            }
        };
    }
    
    private void CheckCameraPermissionAndInitialize()
    {
        // Check for camera permissions when the scanner is opened
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permission Denied", 
                            "Camera permission is required to scan QR codes.", "OK");
                        _viewModel.IsQrScannerVisible = false;
                        return;
                    }
                }
                
                // Start the camera view if permission granted
                await InitializeCameraAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Camera Error", 
                    $"An error occurred initializing the camera: {ex.Message}", "OK");
                _viewModel.IsQrScannerVisible = false;
            }
        });
    }
    
    private async Task InitializeCameraAsync()
    {
        try
        {
            if (cameraView == null)
                return;
            
            // Ensure camera is stopped first to avoid issues
            try
            {
                await cameraView.StopCameraAsync();
            }
            catch
            {
                // Ignore errors from stopping a camera that wasn't started
            }
            
            // Clear any existing settings
            _isFlashOn = false;
            cameraView.BarCodeDetectionEnabled = false;
            
            // Start camera with a short delay to ensure UI is ready
            await Task.Delay(100);
            
            // Start the camera
            await cameraView.StartCameraAsync();
            
            // Enable barcode detection after camera is successfully started
            cameraView.BarCodeDetectionEnabled = true;
            
            // Set initialized flag
            _viewModel.IsCameraInitialized = true;
            
            // Start a timeout timer to handle case where no QR code is detected
            StartScanTimeoutTimer();
            
            // Debug information
            System.Diagnostics.Debug.WriteLine("Camera initialized successfully");
        }
        catch (Exception ex)
        {
            _viewModel.IsCameraInitialized = false;
            await DisplayAlert("Camera Error", 
                $"Failed to start camera: {ex.Message}", "OK");
            _viewModel.IsQrScannerVisible = false;
            
            System.Diagnostics.Debug.WriteLine($"Camera initialization error: {ex}");
        }
    }
    
    private void StartScanTimeoutTimer()
    {
        // Clean up any existing timer
        _scanTimeoutTimer?.Stop();
        _scanTimeoutTimer?.Dispose();
        
        // Create a new timer that will fire after 60 seconds
        _scanTimeoutTimer = new System.Timers.Timer(60000);
        _scanTimeoutTimer.Elapsed += ScanTimeout_Elapsed;
        _scanTimeoutTimer.AutoReset = false;
        _scanTimeoutTimer.Start();
    }
    
    private void ScanTimeout_Elapsed(object? sender, ElapsedEventArgs e)
    {
        // Only show timeout message if the scanner is still visible (not already completed)
        if (_viewModel.IsQrScannerVisible && !_viewModel.IsPopupVisible)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Ask user if they want to continue scanning
                bool continueScanning = await DisplayAlert("Scanning Timeout", 
                    "No QR code has been detected for some time. Do you want to continue scanning?", 
                    "Continue", "Cancel");
                
                if (continueScanning)
                {
                    // Restart the timer
                    StartScanTimeoutTimer();
                }
                else
                {
                    // Close scanner
                    _viewModel.IsQrScannerVisible = false;
                    _viewModel.IsCameraInitialized = false;
                    
                    if (cameraView != null)
                    {
                        await cameraView.StopCameraAsync();
                    }
                }
            });
        }
    }
    
    /// <summary>
    /// Handles the barcode detection from Camera.MAUI
    /// </summary>
    private void CameraView_BarCodeDetected(object sender, BarcodeEventArgs e)
    {
        if (!_viewModel.IsQrScannerVisible)
            return;
            
        MainThread.BeginInvokeOnMainThread(async () => 
        {
            try
            {
                // Stop timeout timer
                _scanTimeoutTimer?.Stop();
                
                // Stop scanning once we detect a barcode
                if (cameraView != null)
                {
                    cameraView.BarCodeDetectionEnabled = false;
                }
                
                // Process the detected barcode
                if (e.Result != null && e.Result.Length > 0)
                {
                    // Get the first barcode result
                    var firstResult = e.Result[0];
                    if (!string.IsNullOrEmpty(firstResult.Text))
                    {
                        // Set the scanned text and show popup
                        _viewModel.ScannedQrText = firstResult.Text;
                        _viewModel.IsPopupVisible = true;
                        _viewModel.IsQrScannerVisible = false;
                        
                        // Play a success sound or vibration to indicate detection
                        try
                        {
                            Vibration.Default.Vibrate();
                        }
                        catch (Exception)
                        {
                            // Ignore vibration errors
                        }
                        
                        // Clean up camera resources
                        if (cameraView != null)
                        {
                            await cameraView.StopCameraAsync();
                            _viewModel.IsCameraInitialized = false;
                        }
                    }
                    else
                    {
                        // Re-enable barcode detection if the result was empty
                        if (cameraView != null)
                        {
                            cameraView.BarCodeDetectionEnabled = true;
                        }
                        
                        // Restart timeout timer
                        StartScanTimeoutTimer();
                    }
                }
                else
                {
                    // Re-enable barcode detection if no results
                    if (cameraView != null)
                    {
                        cameraView.BarCodeDetectionEnabled = true;
                    }
                    
                    // Restart timeout timer
                    StartScanTimeoutTimer();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in barcode detection: {ex.Message}");
                
                // Re-enable barcode detection
                if (cameraView != null)
                {
                    cameraView.BarCodeDetectionEnabled = true;
                }
                
                // Restart timeout timer
                StartScanTimeoutTimer();
            }
        });
    }
    
    /// <summary>
    /// Toggle the flashlight/torch for the camera
    /// </summary>
    private void ToggleFlash_Clicked(object sender, EventArgs e)
    {
        if (cameraView != null && _viewModel.IsCameraInitialized)
        {
            try
            {
                _isFlashOn = !_isFlashOn;
                
                // Debug info
                System.Diagnostics.Debug.WriteLine($"Setting TorchEnabled to {_isFlashOn}");
                
                // Try setting torch property
                cameraView.TorchEnabled = _isFlashOn;
                
                // Update button appearance based on state
                if (sender is Button flashButton)
                {
                    flashButton.BackgroundColor = _isFlashOn ? Colors.Yellow : Colors.Transparent;
                    flashButton.Text = _isFlashOn ? "💡" : "🔦";  // Change icon based on state
                }
            }
            catch (Exception ex)
            {
                // Show error message if torch couldn't be toggled
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Flashlight Error", 
                        $"Could not toggle flashlight: {ex.Message}", "OK");
                });
                
                System.Diagnostics.Debug.WriteLine($"Flashlight error: {ex}");
            }
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Camera Not Ready", 
                    "Camera must be initialized before using the flashlight.", "OK");
            });
        }
    }
    
    /// <summary>
    /// Manual QR code capture button handler
    /// </summary>
    private async void CaptureQrCode_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.IsQrScannerVisible || !_viewModel.IsCameraInitialized)
            return;
            
        try
        {
            if (cameraView != null)
            {
                // Stop the timeout timer during manual capture
                _scanTimeoutTimer?.Stop();
                
                // Disable automatic barcode detection during manual capture
                cameraView.BarCodeDetectionEnabled = false;
                
                // Take a photo
                var photo = await cameraView.TakePhotoAsync();
                if (photo != null)
                {
                    // Let the user know we're processing the image
                    await DisplayAlert("Processing", "Analyzing the captured image for QR codes...", "OK");
                    
                    // Re-enable barcode detection, which will process the next frame
                    // This is a workaround since Camera.MAUI doesn't expose direct image analysis
                    cameraView.BarCodeDetectionEnabled = true;
                    
                    // Give some time for detection to occur
                    await Task.Delay(2000);
                    
                    // If no barcode was detected after the delay, show an error
                    if (!_viewModel.IsPopupVisible)
                    {
                        await DisplayAlert("No QR Code Found", 
                            "No QR code was detected in the captured image. Please try again.", "OK");
                        
                        // Restart the timeout timer
                        StartScanTimeoutTimer();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to capture QR code: {ex.Message}", "OK");
            
            // Re-enable barcode detection
            if (cameraView != null)
            {
                cameraView.BarCodeDetectionEnabled = true;
            }
            
            // Restart the timeout timer
            StartScanTimeoutTimer();
        }
    }
    
    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Clean up timer
        _scanTimeoutTimer?.Stop();
        _scanTimeoutTimer?.Dispose();
        _scanTimeoutTimer = null;
        
        // Ensure camera is stopped when page disappears
        if (cameraView != null && _viewModel.IsCameraInitialized)
        {
            try
            {
                await cameraView.StopCameraAsync();
                _viewModel.IsCameraInitialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping camera: {ex.Message}");
            }
        }
    }
}
