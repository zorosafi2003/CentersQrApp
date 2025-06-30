using CentersBarCode.ViewModels;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Media;
using System.Timers;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

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

        RequestCameraPermissions();
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

    private async void RequestCameraPermissions()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permission Denied", "Camera access is required.", "OK");
            _viewModel.IsQrScannerVisible = false;
        }
    }

    private void CheckCameraPermissionAndInitialize()
    {
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

                // Initialize the camera view
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
            {
                System.Diagnostics.Debug.WriteLine("Camera view is null");
                return;
            }

            // Configure barcode reader options
            cameraView.Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormat.Ean13 | BarcodeFormat.Ean8 | BarcodeFormat.Code128 | BarcodeFormat.Code39,
                AutoRotate = true,
                TryHarder = false, // Enable for better detection
                Multiple = false,
            };

            cameraView.CameraLocation = CameraLocation.Rear;
            cameraView.IsTorchOn = false;
            _isFlashOn = false;

            // Enable barcode detection
            cameraView.IsDetecting = true;

            // Set initialized flag
            _viewModel.IsCameraInitialized = true;

            // Start a timeout timer
            StartScanTimeoutTimer();

            System.Diagnostics.Debug.WriteLine("Camera initialized successfully");
        }
        catch (Exception ex)
        {
            _viewModel.IsCameraInitialized = false;
            await DisplayAlert("Camera Error",
                $"Failed to initialize camera: {ex.Message}", "OK");
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
        if (_viewModel.IsQrScannerVisible && !_viewModel.IsPopupVisible)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool continueScanning = await DisplayAlert("Scanning Timeout",
                    "No QR code has been detected for some time. Do you want to continue scanning?",
                    "Continue", "Cancel");

                if (continueScanning)
                {
                    StartScanTimeoutTimer();
                }
                else
                {
                    _viewModel.IsQrScannerVisible = false;
                    _viewModel.IsCameraInitialized = false;
                    if (cameraView != null)
                    {
                        cameraView.IsDetecting = false;
                    }
                }
            });
        }
    }

    private void CameraView_BarCodeDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (!_viewModel.IsQrScannerVisible)
        {
            System.Diagnostics.Debug.WriteLine("Barcode detection skipped: Scanner not visible");
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Barcode detected: {e.Results?.Length} results");

                // Stop timeout timer
                _scanTimeoutTimer?.Stop();

                // Stop scanning
                if (cameraView != null)
                {
                    cameraView.IsDetecting = false;
                }

                // Process the detected barcode
                if (e.Results != null && e.Results.Length > 0)
                {
                    var firstResult = e.Results[0];
                    var resultText = firstResult.ToString();
                    System.Diagnostics.Debug.WriteLine($"Detected QR code: {resultText}");

                    if (!string.IsNullOrEmpty(resultText))
                    {
                        _viewModel.ScannedQrText = resultText;
                        _viewModel.IsPopupVisible = true;
                        _viewModel.IsQrScannerVisible = false;

                        try
                        {
                            Vibration.Default.Vibrate();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Vibration error: {ex.Message}");
                        }

                        if (cameraView != null)
                        {
                            cameraView.IsDetecting = false;
                            _viewModel.IsCameraInitialized = false;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Empty QR code result");
                        if (cameraView != null)
                        {
                            cameraView.IsDetecting = true;
                        }
                        StartScanTimeoutTimer();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No QR code results detected");
                    if (cameraView != null)
                    {
                        cameraView.IsDetecting = true;
                    }
                    StartScanTimeoutTimer();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in barcode detection: {ex.Message}");
                if (cameraView != null)
                {
                    cameraView.IsDetecting = true;
                }
                StartScanTimeoutTimer();
            }
        });
    }

    private void ToggleFlash_Clicked(object sender, EventArgs e)
    {
        if (cameraView != null && _viewModel.IsCameraInitialized)
        {
            try
            {
                _isFlashOn = !_isFlashOn;
                System.Diagnostics.Debug.WriteLine($"Setting TorchEnabled to {_isFlashOn}");
                cameraView.IsTorchOn = _isFlashOn;

                if (sender is Button flashButton)
                {
                    flashButton.BackgroundColor = _isFlashOn ? Colors.Yellow : Colors.Transparent;
                    flashButton.Text = _isFlashOn ? "💡" : "🔦";
                }
            }
            catch (Exception ex)
            {
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

    private async void CaptureQrCode_Clicked(object sender, EventArgs e)
    {
        if (!_viewModel.IsQrScannerVisible || !_viewModel.IsCameraInitialized)
        {
            await DisplayAlert("Error", "Camera is not initialized.", "OK");
            return;
        }

        try
        {
            _scanTimeoutTimer?.Stop();
            if (cameraView != null)
            {
                cameraView.IsDetecting = true;
            }
            await DisplayAlert("Scanning", "Please position the QR code in the frame.", "OK");
            StartScanTimeoutTimer();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to process QR code: {ex.Message}", "OK");
            if (cameraView != null)
            {
                cameraView.IsDetecting = true;
            }
            StartScanTimeoutTimer();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _scanTimeoutTimer?.Stop();
        _scanTimeoutTimer?.Dispose();
        _scanTimeoutTimer = null;

        if (cameraView != null && _viewModel.IsCameraInitialized)
        {
            try
            {
                cameraView.IsDetecting = false;
                _viewModel.IsCameraInitialized = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping camera: {ex.Message}");
            }
        }
    }
}
