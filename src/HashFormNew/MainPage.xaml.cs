using IG.App.ViewModel;
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


    async void OnButtonHelpClicked(object sender, EventArgs args)
    {
        await DisplayAlert("Help", "Help is not implemented yet", "OK");
    }

    async void OnButtonAboutClicked(object sender, EventArgs args)
    {
        await DisplayAlert("Info", "New HashForm application, 2022", "OK");
    }

    async void OnButtonTestClicked(object sender, EventArgs args)
    {
        await DisplayAlert("Info", "New HashForm application, 2022", "OK");
    }


     void OnButtonClearClicked(object sender, EventArgs args)
    {
        ViewModel.InvalidateHashValues();
    }

    

    async void OnButtonCalculateClicked(object sender, EventArgs args)
    {
        try
        {
            ViewModel.CalculateMissingHashesAsync();
            await Task.Delay(50);
            ViewModel.RefreshHashvaluesInUi();  // Once again trigger OnPropertyChanged events
            (this.OuterLayout as IView).InvalidateArrange();  // this should force-refrech the updated controls, but it also does not work

        }
        catch (Exception ex)
        {
            await DisplayAlert("ERROR", $"Error in computation of hash values: {ex.Message}", "OK");
        }
    }


}

