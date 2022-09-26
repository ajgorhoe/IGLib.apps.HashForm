using IG.App.ViewModel;
using IG.Crypto;
using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;

namespace IG.App;

public partial class MainPage : ContentPage
{

    public MainPage(MainViewModel vm) : base()
    {
        InitializeComponent();
        BindingContext = vm;
        ViewModel = vm;
    }


    /// <summary>Referencr to the corresponding ViewModel.</summary>
    MainViewModel ViewModel { get; init; }


    async void OnButtonBrowseClicked(object sender, EventArgs args)
    {
        PickOptions options = new()
        {
            PickerTitle = "Please select the file to be hashed",
            
        };
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            var selectedFilePath = result.FullPath;
            if (!string.IsNullOrEmpty(selectedFilePath))
            {
                ViewModel.FilePath = result.FullPath;
            }
        }
        catch (Exception)
        {
            // ViewModel.FilePath = null;
        }
    }


    Brush _fileEntryInitialBackground = Color.FromRgb(255, 255, 255);

    string _storedFilePath = null;

    /// <summary>Indicates visually on the <see cref="FileEntry"/> control that something is dragged over,
    /// by changing background color.
    /// <remarks>This should be implemented via VievModel property binding in the future.</remarks></summary>
    void OnDragOverFileEntry(object sender, DragEventArgs eventArgs)
    {
        try
        {
            if (ViewModel.NumEntries == 0)
            {
                // Update the DragOver counter:
                ++ViewModel.NumEntries;
                eventArgs.AcceptedOperation = DataPackageOperation.Copy;
                _storedFilePath = ViewModel.FilePath;
                ViewModel.FilePath = "<< Drop here to update file path >>";
                // _fileEntryInitialBackground = this.FileEntry.Background;
                // this.FileEntry.Background = Color.Parse("Orange");
            }
        }
        catch { }
    }

    void OnDragLeaveFileEntry(object sender, DragEventArgs eventArgs)
    {
        try
        {
            // reset the DragOver counter:
            ViewModel.NumEntries = 0;
            if (ViewModel.NumEntries == 0)
            {
                ViewModel.FilePath = _storedFilePath;
                _storedFilePath = null;
                //this.FileEntry.Background = _fileEntryInitialBackground;
            }
        }
        catch { }
    }

    async void OnDropFileEntry(object sender, DropEventArgs e)
    {
        try
        {
            // reset the DragOver counter:
            ViewModel.NumEntries = 0;
            if (ViewModel.NumEntries == 0)
            {
                _storedFilePath = null;
                //this.FileEntry.Background = _fileEntryInitialBackground;
            }
        }
        catch { }
        try
        {
            // ViewModel.FilePath
            ViewModel.DroppedTextValue = await e.Data.GetTextAsync();
        }
        catch { }
    }


    async void ButtonHelp_Clicked(object sender, EventArgs args)
    {
        await DisplayAlert("Help", "Help is not implemented yet", "OK");
    }

    async void ButtonAbout_Clicked(object sender, EventArgs args)
    {
        await DisplayAlert("Info", "New HashForm application, 2022", "OK");
    }

    async void ButtonTest_Clicked(object sender, EventArgs args)
    {
        await DisplayAlert("Info", "New HashForm application, 2022", "OK");
    }


     void ButtonClear_Clicked(object sender, EventArgs args)
    {
        ViewModel.InvalidateHashValues();
    }

    

    async void ButtonCalculate_Clicked(object sender, EventArgs args)
    {
        try
        {
            await ViewModel.CalculateMissingHashesAsync();
            await Task.Delay(50);
            ViewModel.RefreshHashvaluesInUi();  // Once again trigger OnPropertyChanged events
            (this.OuterLayout as IView).InvalidateArrange();  // this should force-refrech the updated controls, but it also does not work

        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", $"Error in computation of hash values: {ex.Message}", "OK");
        }
    }

    const string VirusTotalBaseAddress = "https://www.virustotal.com/gui/file/";

    const string VirusTotalAddressAppendix = "?nocache=1";

    string hashTypeVT { get; } = HashConst.SHA256Hash;

    async void ButtonQueryVirusTotal_Clicked(object sender, EventArgs args)
    {
        string hashValue = null;
        string browserAddress = null;
        try
        {

            hashValue = ViewModel.GetHashValue(hashTypeVT);
            if (string.IsNullOrEmpty(hashValue))
            {
                hashValue = await ViewModel.CalculateHashAsync(hashTypeVT);
            }
            browserAddress = VirusTotalBaseAddress + hashValue + VirusTotalAddressAppendix;

            Uri uri = new Uri(browserAddress);
            await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);

        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", $"Error when browsing to:{Environment.NewLine}  {browserAddress}{Environment.NewLine}"
                + $"Error message: {Environment.NewLine}  {ex.Message}", "OK");
        }
    }


}

