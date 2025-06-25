using System.Collections.ObjectModel;

namespace CentersBarCode.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<string> _centers;

    [ObservableProperty]
    private string? _selectedCenter;

    [ObservableProperty]
    private bool _isQrScannerVisible;

    [ObservableProperty]
    private bool _isPopupVisible;

    [ObservableProperty]
    private string _scannedQrText;

    [ObservableProperty]
    private bool _isCameraInitialized;

    public MainViewModel()
    {
        // Initialize centers list
        Centers = new ObservableCollection<string>
        {
            "Center1",
            "Center2",
            "Center3"
        };

        // Initialize other properties
        IsQrScannerVisible = false;
        IsPopupVisible = false;
        ScannedQrText = string.Empty;
        IsCameraInitialized = false;
    }

    // Command to open QR scanner when a center is selected
    [RelayCommand]
    private void OpenQrScanner()
    {
        // Reset camera state before showing scanner
        IsCameraInitialized = false;
        
        // Show scanner UI
        IsQrScannerVisible = true;
        
        // Camera initialization happens in the code-behind
        System.Diagnostics.Debug.WriteLine("QR Scanner opened");
    }

    // Command to save the scanned QR code
    [RelayCommand]
    private async Task SaveQrCode()
    {
        try
        {
            // Here you can implement the logic to save the QR code data
            // For example, save to a database or file
            
            if (Application.Current?.MainPage != null)
            {
                // For demonstration, just show a success message
                await Application.Current.MainPage.DisplayAlert("Success", 
                    $"QR Code for {SelectedCenter} saved successfully", "OK");
            }
            
            // Close the popup
            IsPopupVisible = false;
            ScannedQrText = string.Empty;
        }
        catch (Exception ex)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Error", 
                    $"Failed to save QR code: {ex.Message}", "OK");
            }
        }
    }

    // Command to cancel and close the popup
    [RelayCommand]
    private void CancelQrCode()
    {
        IsPopupVisible = false;
        ScannedQrText = string.Empty;
    }

    // Command to close the QR scanner view
    [RelayCommand]
    private void CloseQrScanner()
    {
        IsQrScannerVisible = false;
        IsCameraInitialized = false;
        System.Diagnostics.Debug.WriteLine("QR Scanner closed");
    }
    
    // Property to determine if the scan button should be enabled
    public bool CanScan => !string.IsNullOrEmpty(SelectedCenter);
    
    // Update command bindings when SelectedCenter changes
    partial void OnSelectedCenterChanged(string? value)
    {
        OnPropertyChanged(nameof(CanScan));
    }
    
    // Handle camera initialization state change
    partial void OnIsCameraInitializedChanged(bool value)
    {
        System.Diagnostics.Debug.WriteLine($"Camera initialized: {value}");
    }
}
