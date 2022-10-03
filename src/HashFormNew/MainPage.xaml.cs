// using AndroidX.Activity.Result;
using IG.App.ViewModel;
using IG.Crypto;
using System;
using System.Diagnostics;
//using Windows.ApplicationModel.Background;

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
            var selectedFilePath = result?.FullPath;
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

    /// <summary>Indicates visually on the File Entry control that something is dragged over,
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

    /// <summary>On Drag start, get text from the concerned element and set is as text for drop events.</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void DragStarting_GetText(object sender, DragStartingEventArgs e)
    {
        if (ViewModel != null) ViewModel.DebugMessage = null;
        string senderId = (sender as Element)?.Parent?.GetType().Name;
        string draggedText = null;
        string draggedTextInitial = null;
        // Get dragged text that is currently on e:
        draggedTextInitial = e.Data.Text;
        if (draggedTextInitial == null && e.Data.Properties.ContainsKey("Text"))
        {
            object res1;
            bool success = e.Data.Properties.TryGetValue("Text", out res1);
            if (success) { draggedTextInitial = (string)res1; }
        }
        draggedText = draggedTextInitial;
        if (draggedText == null)
        {

            // Overrides for specific  type of UI element that is parent of sender: 
            if ((sender as Element)?.Parent is Label)
            {
                draggedText = ((sender as Element)?.Parent as Label)?.Text;
            }
            if ((sender as Element)?.Parent is Entry)
            {
                draggedText = ((sender as Element)?.Parent as Entry)?.Text;
            }
            if ((sender as Element)?.Parent is Editor)
            {
                draggedText = ((sender as Element)?.Parent as Editor)?.Text;
            }
            if ((sender as Element)?.Parent is CheckBox)
            {
                draggedText = ((sender as Element)?.Parent as CheckBox)?.IsChecked.ToString();
            }
            if ((sender as Element)?.Parent is RadioButton)
            {
                draggedText = ((sender as Element)?.Parent as RadioButton)?.IsChecked.ToString();
            }
        }

        // Override for specific UI elements: 


        if ((sender as Element)?.Parent == Md5Label)
        {
            // We are dragging an UI elemennt related to MD5 hash value:
            draggedText = ViewModel.HashValueMD5;
        }
        if ((sender as Element)?.Parent == Sha1Label)
        {
            // We are dragging an UI elemennt related to MD5 hash value:
            draggedText = ViewModel.HashValueSHA1;
        }
        if ((sender as Element)?.Parent == Sha256Label)
        {
            // We are dragging an UI elemennt related to MD5 hash value:
            draggedText = ViewModel.HashValueSHA256;
        }
        if ((sender as Element)?.Parent == Sha512Label)
        {
            // We are dragging an UI elemennt related to MD5 hash value:
            draggedText = ViewModel.HashValueSHA512;
        }

        if ((sender as Element)?.Parent?.Parent == VerifyHashValueContainer
            || (sender as Element)?.Parent == VerifyHashValueContainer)
        {
            // We are dragging an UI element that is contained in VerifyHashValueContainer - set dragged data to the verified hash:
            draggedText = ViewModel.VerifiedHashValue;
        }


        if (ViewModel != null)
        {
            // Providee some debug information:
            ViewModel.DebugMessage = $"Drag start on {senderId}: draggedTextInitial={draggedTextInitial} draggedText={draggedText}";
        }
        e.Data.Text = draggedText;
        e.Data.Properties["Text"]= draggedText;
    }





    /// <summary>Indicates visually on the File Entry control that something is dragged over,
    /// by changing background color.
    /// <remarks>This should be implemented via VievModel property binding in the future.</remarks></summary>
    void OnDragOverVerifiedHashValueEntry(object sender, DragEventArgs eventArgs)
    {
        try
        {
            if (ViewModel.NumEntries == 0)
            {
                // Update the DragOver counter:
                ++ViewModel.NumEntries;
                eventArgs.AcceptedOperation = DataPackageOperation.Copy;
                ViewModel.DebugMessage = $"DragOver-VerifiedHash: NumEntries={ViewModel.NumEntries} Text={eventArgs.Data?.Text}";
            }
        }
        catch { }
    }

    void OnDragLeaveVerifiedHashValueEntry(object sender, DragEventArgs eventArgs)
    {
        try
        {
            // reset the DragOver counter:
            ViewModel.NumEntries = 0;
            ViewModel.DebugMessage = $"DragLeave-VerifiedHash: NumEntries={ViewModel.NumEntries} Text={eventArgs.Data?.Text}";
        }
        catch { }
    }

    async void OnDropVerifiedHashValueEntry(object sender, DropEventArgs e)
    {
        try
        {
            // reset the DragOver counter:
            ViewModel.NumEntries = 0;
        }
        catch { }
        try
        {
            string droppedText = null;
            // ViewModel.FilePath
            if (e.Data!=null)
            {
                droppedText = await e.Data.GetTextAsync();
            }
            ViewModel.DebugMessage = $"Drop-VerifiedHash: NumEntries={ViewModel.NumEntries} Text={droppedText}";
            VerifiedHashValueEnty.Text = droppedText;
        }
        catch { }
    }



}

