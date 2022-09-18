using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IG.Crypto;
using Microsoft.Maui.Storage;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Reflection;
using Windows.UI.WebUI;
// using static CommunityToolkit.Mvvm.ComponentModel.__Internals.__TaskExtensions.TaskAwaitableWithoutEndValidation;

namespace IG.App.ViewModel;

public partial class MainViewModel :
    ObservableObject
//,
//INotifyPropertyChanged
{


    // Defined in ObservableObject:
    // public event PropertyChangedEventHandler PropertyChanged;
    // public void OnPropertyChanged(striing)


    // event PropertyChangedEventHandler PropertyChanged;

    public MainViewModel(IServiceProvider serviceProvider, HashCalculatorBase hashCalculator = null)
    {
        HashCalculator = hashCalculator;
        ServiceProvider = serviceProvider;
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

#if DEBUG
        IsDebugMode = true;
#else
        IsDebugMode = false;
#endif

        LaunchInfoDialogCommand = new Command(
            execute: () =>
            {
                // Do the stuff...
                // ToDo: implement command body!
                // 
                ((Command)LaunchInfoDialogCommand).ChangeCanExecute();
            },
            // ToDo: replace criterion below.
            canExecute: () => true
        );

    }

    private IServiceProvider _serviceProvider;

    public IServiceProvider ServiceProvider
    {
        get {
            if (_serviceProvider == null)
            {

            }
            return _serviceProvider;
        }
        init
        {
            if (value != _serviceProvider)
                _serviceProvider = value;
        }
    }


    private HashCalculatorBase _hashCalculator = null;


    // ToDo: replace type with interface!
    public HashCalculatorBase HashCalculator
    {
        get
        {
            if (_hashCalculator == null)
                _hashCalculator = new HashCalculator();
            return _hashCalculator;
        }
        init
        {
            if (value != _hashCalculator)
                _hashCalculator = value;
        }
    }

    public ICommand AboutCommand => new Command(
        execute: () =>
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "About", AppNameAndVersionString + Environment.NewLine
                + Environment.NewLine + "Calculates cryptographic hashes of files and text.");
        });

    public ICommand HelpCommand => new Command(
        execute: () =>
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "Help", "Help is not yet implemented." + Environment.NewLine + "See also: " +
                Environment.NewLine + "http://www2.arnes.si/~ljc3m2/igor/software/IGLibShellApp/HashForm.html");
        });

    public ICommand CopyHashToClipboardCommand => new Command<string>(
            execute: (string param) =>
            {
                ServiceProvider?.GetService<IG.App.IClipboardService>()?.SetText(param);

                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                        "Hash Copied",
                        "Hash copied to clipboard: "
                        + Environment.NewLine + param, "OK");

                //Task.Run(async () => { 

                //    // await Task.Delay(800);

                //    ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlertWithTimeout(
                //        "Clipboard",
                //        "Hash copied to clipboard: " 
                //        + Environment.NewLine + param, "OK", 2000);
                //});

            },
            canExecute: (string param) => { return true; }
        );

    public ICommand PasteVerifiedHashFromClipboardCommand => new Command(
            execute: () =>
            {
                ServiceProvider?.GetService<IG.App.IClipboardService>()?.GetText(
                    (valueFromClippboard) => { VerifiedHashValue = valueFromClippboard; }
                );
            }
        );


    string _appNameAndVersionString = null;

    string AppNameAndVersionString {
        get {
            if (_appNameAndVersionString == null)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string appVersion = assembly.GetName().Version.ToString();
                string appName = assembly.GetName().Name;
                _appNameAndVersionString = appName + " v. " + appVersion;
            }
            return _appNameAndVersionString;
        } }



    /// <summary>Calculate hash function of specific type on the current input from this class.</summary>
    /// <param name="hashType">Typ of the hash function applied.</param>
    /// <returns></returns>
    protected async Task<string> CalculateHashAsync(string hashType)
    {
        if (IsTextHashing)
        {
            if (string.IsNullOrEmpty(this.TextToHash))
                throw new InvalidOperationException("Hash computation: Text to be hashed is empty.");
            var hashText = async () =>
            {
                await Task.FromResult(true); // dummy await; ToDo: replace with async method
                try
                {
                    return await HashCalculator.CalculateTextHashStringAsync(hashType, TextToHash);
                }
                catch
                {
                    return null;
                }

            };
            return await Task.Run(hashText);
        } else if (IsFileHashing)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                throw new ArgumentException("Hash computation: Path to file to be hashed is not specified.");
            }
            if (!File.Exists(FilePath))
            {
                throw new InvalidOperationException("Hash computation: File to be hashed does not exist.");
            }
            var hashFile = async () => {
                await Task.FromResult(true); // dummy await; ToDo: replace with async method
                try
                {
                    return await HashCalculator.CalculateFileHashStringAsync(hashType, FilePath);
                }
                catch
                {
                    return null;
                }
            };
            return await Task.Run(hashFile);
        } else
        {
            throw new InvalidOperationException("Input for calculation of hash function is not specified.");
        }
    }

    public async void VerifyHashAsync()
    {
        if (string.IsNullOrEmpty(VerifiedHashValue))
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "Warning",  "Hash value to be verified is not specified.");
            return;
        }
        VerifiedHashValue = IsUpperCaseHashes? VerifiedHashValue.ToUpper(): VerifiedHashValue.ToLower();
        string matchedHashType = null;
        if (string.IsNullOrEmpty(matchedHashType))
        {
            if (string.IsNullOrEmpty(HashValueMD5))
            {
                HashValueMD5 = await CalculateHashAsync(HashConst.MD5Hash);
            }
            if (HashValueMD5 == VerifiedHashValue)
            {
                matchedHashType = HashConst.MD5Hash;
            }
        }
        if (string.IsNullOrEmpty(matchedHashType))
        {
            if (string.IsNullOrEmpty(HashValueSHA1))
            {
                HashValueSHA1 = await CalculateHashAsync(HashConst.SHA1Hash);
            }
            if (HashValueSHA1 == VerifiedHashValue)
            {
                matchedHashType = HashConst.SHA1Hash;
            }
        }

        if (string.IsNullOrEmpty(matchedHashType))
        {
            if (string.IsNullOrEmpty(HashValueSHA256))
            {
                HashValueSHA256 = await CalculateHashAsync(HashConst.SHA256Hash);
            }
            if (HashValueSHA256 == VerifiedHashValue)
            {
                matchedHashType = HashConst.SHA256Hash;
            }
        }
        if (string.IsNullOrEmpty(matchedHashType))
        {
            if (string.IsNullOrEmpty(HashValueSHA512))
            {
                HashValueSHA512 = await CalculateHashAsync(HashConst.SHA512Hash);
            }
            if (HashValueSHA512 == VerifiedHashValue)
            {
                matchedHashType = HashConst.SHA512Hash;
            }
        }



        if (string.IsNullOrEmpty(matchedHashType))
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "WARNING",  "The specified hash value: " + Environment.NewLine
                + $"  {VerifiedHashValue}" + Environment.NewLine
                + "does NOT correspond to any type of hashes considered by this application.");
        }
        else
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "Info",  "The specified hash value: " + Environment.NewLine
                + $"  {VerifiedHashValue}" + Environment.NewLine
                + $"corresponds to the {matchedHashType} hash of the specified {(IsFileHashing? "file": "text")}.");

        }
    }


    public ICommand VerifyHashCommand => new Command(
            execute: () =>
            {
                VerifyHashAsync();
            }
        );


    public async void CalculateMissingHashesAsync()
    {
        int numHashesCAlculated = 0;
        // ToDo: fix IsHashesOutdated, then remove "|| true"" 
        if (IsHashesOutdated)
        {
            if (IsInputDataSufficient)
            {
                try
                {
                    IsCalculating = true;
                    List<Task<string>> hashingTasks = new List<Task<string>>();
                    Task<string> hashTaskMD5 = null;
                    Task<string> hashTaskSHA1 = null;
                    Task<string> hashTaskSHA256 = null;
                    Task<string> hashTaskSHA512 = null;
                    if (CalculateMD5 && string.IsNullOrEmpty(HashValueMD5))
                    {
                        hashingTasks.Add(hashTaskMD5 = CalculateHashAsync(HashConst.MD5Hash));
                        //string hashValue = await CalculateHashAsync(HashConst.MD5Hash);
                        //HashValueMD5 = hashValue;
                    }
                    if (CalculateSHA1 && string.IsNullOrEmpty(HashValueSHA1))
                    {
                        hashingTasks.Add(hashTaskSHA1 = CalculateHashAsync(HashConst.SHA1Hash));
                        //string hashValue = await CalculateHashAsync(HashConst.SHA1Hash);
                        //HashValueSHA1 = hashValue;
                    }
                    if (CalculateSHA256 && string.IsNullOrEmpty(HashValueSHA256))
                    {
                        hashingTasks.Add(hashTaskSHA256 = CalculateHashAsync(HashConst.SHA256Hash));
                        //string hashValue = await CalculateHashAsync(HashConst.SHA256Hash);
                        //HashValueSHA256 = hashValue;
                    }
                    if (CalculateSHA512 && string.IsNullOrEmpty(HashValueSHA512))
                    {
                        hashingTasks.Add(hashTaskSHA512 = CalculateHashAsync(HashConst.SHA512Hash));
                        //string hashValue = await CalculateHashAsync(HashConst.SHA512Hash);
                        //HashValueSHA512 = hashValue;
                    }

                    //if (hashTaskMD5 != null) { HashValueMD5 = await hashTaskMD5; ++numHashesCAlculated; }
                    //if (hashTaskSHA1 != null) { HashValueSHA1 = await hashTaskSHA1; ++numHashesCAlculated; }
                    //if (hashTaskSHA256 != null) { HashValueSHA256 = await hashTaskSHA256; ++numHashesCAlculated; }
                    //if (hashTaskSHA512 != null) { HashValueSHA512 = await hashTaskSHA512; ++numHashesCAlculated; }
                    
                    var x = await Task.WhenAny(hashingTasks);

                    while (hashingTasks.Count > 0)
                    {

                        Task<string> completedTask = await Task.WhenAny(hashingTasks);
                        hashingTasks.Remove(completedTask);

                        //Task<string>[] awaitedTasks = hashingTasks.ToArray();
                        //int completedTaskIndex = Task.WaitAny(awaitedTasks);
                        //Task<string> completedTask = awaitedTasks[completedTaskIndex];
                        //hashingTasks.RemoveAt(completedTaskIndex);

                        if (completedTask == hashTaskMD5)
                        {
                            HashValueMD5 = await completedTask;
                            ++numHashesCAlculated;
                        }
                        else if (completedTask == hashTaskSHA1)
                        {
                            HashValueSHA1 = await completedTask;
                            ++numHashesCAlculated;
                        }
                        else if (completedTask == hashTaskSHA256)
                        {
                            HashValueSHA256 = await completedTask;
                            ++numHashesCAlculated;
                        }
                        else if (completedTask == hashTaskSHA512)
                        {
                            HashValueSHA512 = await completedTask;
                            ++numHashesCAlculated;
                        }
                    }


                        
                    //while (hashingTasks.Count > 0)
                    //{
                    //    Task<string>[] awaitedTasks = hashingTasks.ToArray();
                    //    int completedTaskIndex = Task.WaitAny(awaitedTasks);
                    //    Task<string> completedTask = awaitedTasks[completedTaskIndex];
                    //    hashingTasks.RemoveAt(completedTaskIndex);
                    //    if (completedTask == hashTaskMD5)
                    //    {
                    //        HashValueMD5 = await completedTask;
                    //        ++numHashesCAlculated;
                    //    }
                    //    else if (completedTask == hashTaskSHA1)
                    //    {
                    //        HashValueSHA1 = await completedTask;
                    //        ++numHashesCAlculated;
                    //    }
                    //    else if (completedTask == hashTaskSHA256)
                    //    {
                    //        HashValueSHA256 = await completedTask;
                    //        ++numHashesCAlculated;
                    //    }
                    //    else if (completedTask == hashTaskSHA512)
                    //    {
                    //        HashValueSHA512 = await completedTask;
                    //        ++numHashesCAlculated;
                    //    }
                    //}


                }
                catch
                {
                    throw;
                }
                finally
                {
                    IsCalculating = false;
                    RefreshIsHashesOutdated(false);
                }
            }
            else
            {
                throw new InvalidOperationException("Input data for calculation of hashes is not ready.");
            }
        }
        if (IsSaveFileHashesEligible)
        {
            SaveHashesToFileAsync();
        }
    }


    string HashFileExtension => ".chk";


    bool IsSaveFileHashesEligible => IsSaveFileHashesToFile && IsFileHashing
        && !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

    public virtual async void SaveHashesToFileAsync()
    {
        if (!IsSaveFileHashesEligible)
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "Hashes Not Saved",
                "Conditions to save hashes to a file are not met. " + Environment.NewLine
                + (IsSaveFileHashesToFile ? "" : "  Flag for saving file hashes to files is not set." + Environment.NewLine)
                + (IsFileHashing ? "" : "  Hashes are calclated for text rather than for a file." + Environment.NewLine)
                + (!string.IsNullOrEmpty(FilePath) ? "" : "  File to be hashed is not specified." + Environment.NewLine)
                );
            return;
        }

        string hashFilePath = FilePath + HashFileExtension;
        bool doSave = true;
        if (File.Exists(hashFilePath))
        {
            doSave = false;
            /*
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "File aleady exists", "File already exists: " + Environment.NewLine + FilePath
                + Environment.NewLine + "You will be asked for confirmation to overwrite the file.");
            */
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowConfirmation(
                "Overwrite confirmation", "The file to save calculated hashes already exists: " + Environment.NewLine + FilePath
                + Environment.NewLine + Environment.NewLine + "Owerwrite the file?",
                (doOverwrite) => { doSave = doOverwrite; }, "Yes", "No");
        }
        bool saved = false;
        if (doSave)
        {
            using (TextWriter writer = new StreamWriter(hashFilePath))
            {
                if (writer == null)
                {
                    ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                        "ERROR when Saving File Hashes",
                        "Could not write to the following file: " + Environment.NewLine + Environment.NewLine
                        + hashFilePath + Environment.NewLine + Environment.NewLine
                        + "Before trying again, please check file permissions and " + Environment.NewLine
                        + "and make sure that file is not used by another process.");
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    long fileLength = new FileInfo(FilePath).Length;
                    sb.AppendLine(Environment.NewLine
                        + "File:   " + Path.GetFileName(FilePath) + Environment.NewLine
                        + "Length: " + fileLength + Environment.NewLine + Environment.NewLine
                        + "Hash values: " + Environment.NewLine);
                    if (!string.IsNullOrEmpty(HashValueMD5))
                        sb.AppendLine("  MD5:    " + Environment.NewLine + HashValueMD5);
                    if (!string.IsNullOrEmpty(HashValueSHA1))
                        sb.AppendLine("  SHA1:    " + Environment.NewLine + HashValueSHA1);
                    if (!string.IsNullOrEmpty(HashValueSHA256))
                        sb.AppendLine("  SHA256:    " + Environment.NewLine + HashValueSHA256);
                    if (!string.IsNullOrEmpty(HashValueSHA512))
                        sb.AppendLine("  SHA512:    " + Environment.NewLine + HashValueSHA512);
                    sb.AppendLine("  ");
                    await writer.WriteAsync(sb.ToString());
                    saved = true;
                }
            }
            if (saved)
            {
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                    "File Hashes Saved",
                    "Calculated file hashes were saved to the fillowing file: " + Environment.NewLine + Environment.NewLine
                    + hashFilePath + Environment.NewLine);
            }
        }

    }


    public void UpdateHashCase()
    {
        if (!string.IsNullOrEmpty(HashValueMD5))
            HashValueMD5 = IsUpperCaseHashes ? HashValueMD5.ToUpper() : HashValueMD5.ToLower();
        if (!string.IsNullOrEmpty(HashValueSHA1))
            HashValueSHA1 = IsUpperCaseHashes ? HashValueSHA1.ToUpper() : HashValueSHA1.ToLower();
        if (!string.IsNullOrEmpty(HashValueSHA256))
            HashValueSHA256 = IsUpperCaseHashes ? HashValueSHA256.ToUpper() : HashValueSHA256.ToLower();
        if (!string.IsNullOrEmpty(HashValueSHA512))
            HashValueSHA512 = IsUpperCaseHashes ? HashValueSHA512.ToUpper() : HashValueSHA512.ToLower();
    }


    private string _droppedTextValue = null;

    public string DroppedTextValue
    {
        get => _droppedTextValue;
        set
        {
            if (value != _droppedTextValue)
            {
                _droppedTextValue = value;
                OnPropertyChanged(nameof(DroppedTextValue));
            }
        }
    }

    private int _numEtries = 0;

    /// <summary>Counts total number of DragEnter / DragLeave events, for debug purposes.</summary>
    public int NumEntries
    {
        get => _numEtries;
        set
        {
            if (value != _numEtries)
            {
                _numEtries = value;
                OnPropertyChanged(nameof(NumEntries));
            }
        }
    }

    public bool IsDebugMode { get; init; } = false;

    private bool _isDebugInfoVisible = false;

    /// <summary>If true then debug information will be visible on the view.
    /// This is prevented when not in Debug mode.</summary>
    public bool IsDebugInfoVisible
    {
        get => _isDebugInfoVisible;
        set {
            if (value != _isDebugInfoVisible)
            {
                if (IsDebugMode)
                {
                    _isDebugInfoVisible = value;
                    OnPropertyChanged(nameof(IsDebugInfoVisible));
                }
                else
                {
                    _isDebugInfoVisible = false;
                }
            }
        }
    }

    public ICommand LaunchInfoDialogCommand { get; private set; }


    private string _filePath;
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
                OnPropertyChanged(nameof(DirectoryPath));
                if (IsFileHashing)
                {
                    InvalidateHashValues();
                    TextToHash = GetFilePreview(_filePath);
                }
                RefreshInputDataSufficient();
                RefreshIsHashesOutdated();
            }
        }
    }

    public string DirectoryPath
    {
        get => string.IsNullOrEmpty(FilePath) ? null : Path.GetDirectoryName(FilePath);
    }

    private bool _isFileHashing = true;

    public bool IsFileHashing
    {
        get => _isFileHashing;
        set
        {
            if (value != _isFileHashing)
            {
                _isFileHashing = value;
                OnPropertyChanged(nameof(IsFileHashing));
                OnPropertyChanged(nameof(IsTextHashing));
                OnPropertyChanged(nameof(TextEntryLabelText));
                if (_isFileHashing)
                {
                    TextToHash = GetFilePreview(FilePath);
                } else
                {
                    TextToHash = null; // LastTextToHashWhenTextHashing;
                }
                InvalidateHashValues();
                RefreshInputDataSufficient();
                RefreshIsHashesOutdated();
            }
        }
    }


    private int _maxLinesFilePreview = 100;
    private int _maxCharactersFilePreview = 10000;
    protected string GetFilePreview(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }
        if (!File.Exists(filePath))
        {
            return null;
        }
        bool isProbablyTextFile = false;
        // Read part of the file and sew whether the file could be a text file;
        // This is difficult to establish out in general, but we will base our estimation on the number of newlines:
        int numChecked = _maxCharactersFilePreview;

        using (FileStream fileStream = File.OpenRead(filePath))
        {
            var rawData = new byte[numChecked];
            var rawLength = fileStream.Read(rawData, 0, rawData.Length);
            // Count newlines: 
            int numNewLines = 0;
            for (int i = 0; i < rawLength; ++i)
            {
                if (rawData[i] == '\n')
                {
                    ++numNewLines;
                }
            }
            if (numNewLines > rawLength / 300)
            {
                isProbablyTextFile |= true;
            }
            if (!isProbablyTextFile)
            {
                string str = Encoding.Default.GetString(rawData, 0, rawLength);
                if (rawLength < _maxLinesFilePreview)
                {
                    return str;
                }
                else
                {
                    return str + Environment.NewLine + "...";
                }

            }
        }
        // Probably a string file, try to read a certain number of lines: 
        StringBuilder sb = new StringBuilder();
        int numLinesRead = 0;
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null && numLinesRead < _maxLinesFilePreview)
            {
                ++numLinesRead;
                sb.AppendLine(line);
            }
        }
        if (numLinesRead >= _maxLinesFilePreview)
        {
            sb.AppendLine(Environment.NewLine + "...");
        }
        return sb.ToString();
    }

    public bool IsTextHashing
    {
        get => !IsFileHashing;
        set { IsFileHashing = !value; }
    }

    private bool _calculateHashesAutomatically = false;

    public bool CalculateHashesAutomatically
    {
        get => _calculateHashesAutomatically;
        set
        {
            if (value != _calculateHashesAutomatically)
            {
                _calculateHashesAutomatically = value;
                OnPropertyChanged(nameof(CalculateHashesAutomatically));
                RefreshIsHashesOutdated();  // to eventually trigger automatic calculation of hashes
            }
        }
    }

    private bool _isSaveFileHashesToFile = false;

    public bool IsSaveFileHashesToFile
    {
        get => _isSaveFileHashesToFile;
        set
        {
            if (value != _isSaveFileHashesToFile)
            {
                _isSaveFileHashesToFile = value;
                if (value && IsTextHashing)
                {
                    ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                        "Warning", "Only file hashes will be saved. Switch from text to files for this setting to take effect.");
                }
                if (IsSaveFileHashesEligible)
                {
                    SaveHashesToFileAsync();
                }
            }
        }
    }

    private bool _isUpperCaseHashes = false;

    public bool IsUpperCaseHashes
    {
        get => _isUpperCaseHashes;
        set
        {
            if (value != _isUpperCaseHashes)
            {
                _isUpperCaseHashes = value;
                UpdateHashCase();
            }
        }
    }

    private bool _calculateMD5 = true;

    public bool CalculateMD5
    {
        get => _calculateMD5;
        set
        {
            if (value != _calculateMD5)
            {
                _calculateMD5 = value;
                OnPropertyChanged(nameof(CalculateMD5));
                RefreshIsHashesOutdated();
            }
        }
    }



    private bool _calculateSHA1 = true;

    public bool CalculateSHA1
    {
        get => _calculateSHA1;
        set
        {
            if (value != _calculateSHA1)
            {
                _calculateSHA1 = value;
                OnPropertyChanged(nameof(CalculateSHA1));
                RefreshIsHashesOutdated();
            }
        }
    }

    private bool _calculateSHA256 = true;

    public bool CalculateSHA256
    {
        get => _calculateSHA256;
        set
        {
            if (value != _calculateSHA256)
            {
                _calculateSHA256 = value;
                OnPropertyChanged(nameof(CalculateSHA256));
                RefreshIsHashesOutdated();
            }
        }
    }

    private bool _calculateSHA512 = false;

    public bool CalculateSHA512
    {
        get => _calculateSHA512;
        set
        {
            if (value != _calculateSHA512)
            {
                _calculateSHA512 = value;
                OnPropertyChanged(nameof(CalculateSHA512));
                RefreshIsHashesOutdated();
            }
        }
    }





    /// <summary>Recalculates the <see cref="IsHashesOutdated"/> property, and performs any automatic 
    /// tasks dependent on this property.</summary>
    /// <param name="performAutomaticCalculations">If false then automatic calculations ae not performed.
    /// Default is true.</param>
    public bool RefreshIsHashesOutdated(bool performAutomaticCalculations =  true)
    {
        bool isOutdated = GetHashesOutdated();  // this will also call OnPropertyChanged(nameof(IsHashesOutdated));
        if (isOutdated)
        {
            if (CalculateHashesAutomatically && performAutomaticCalculations && !IsCalculating)
            {
                try
                {
                    CalculateMissingHashesAsync();
                }
                catch
                { }
                finally
                {
                    isOutdated = GetHashesOutdated();
                }
            }
        }
        return isOutdated;
    }


    /// <summary>Computes the true value for <see cref="IsHashesOutdated"/>.</summary>
    /// <returns></returns>
    protected bool GetHashesOutdated()
    {
        bool isOutdated = false;
        if (IsInputDataSufficient)
        {
            if (CalculateMD5 && string.IsNullOrEmpty(HashValueMD5))
            {
                isOutdated = true;
            }
            else if (CalculateSHA1 && string.IsNullOrEmpty(HashValueSHA1))
            {
                isOutdated = true;
            }
            else if (CalculateSHA256 && string.IsNullOrEmpty(HashValueSHA256))
            {
                isOutdated = true;
            }
            else if (CalculateSHA512 && string.IsNullOrEmpty(HashValueSHA512))
            {
                isOutdated = true;
            }
        }
        if (isOutdated != _isHashesOutdated)
        {
            _isHashesOutdated = isOutdated;
            OnPropertyChanged(nameof(IsHashesOutdated));
        }
        return isOutdated;
    }

    private bool _isHashesOutdated = false;

    /// <summary>Whether the currently kept hash values are outdated and need recalculation.</summary>
    public bool IsHashesOutdated
    {
        get
        {
            return GetHashesOutdated();
        }
    }

    private bool _isCalculating = false;

    /// <summary>Wheen changed to true, this also triggers actual calculation of hash values!</summary>
    public bool IsCalculating
    {
        get => _isCalculating;
        set
        {
            if (value != _isCalculating)
            {
                if (value == true && !IsHashesOutdated)
                {
                    // Hashes are not outdated, no need to do calculation!
                    return;
                }
                _isCalculating = value;
                OnPropertyChanged(nameof(IsCalculating));
                // ToDo: trigger calculation!
            }
        }
    }

    /// <summary>This method was added in order to alleviate the issue with labels in UI bot refreshing on 
    /// updating values of <see cref="HashValueMD5"/>, <see cref="HashValueSHA1"/>, etc. Calling this method
    /// did not help (a possible bug?), but it helped when labels were changed to read-only text entries.</summary>
    public void RefreshHashvaluesInUi()
    {
        OnPropertyChanged(nameof(HashValueMD5));
        OnPropertyChanged(nameof(HashValueSHA1));
        OnPropertyChanged(nameof(HashValueSHA256));
        OnPropertyChanged(nameof(HashValueSHA512));
    }

    private string _hashValueMD5 = null;

    public string HashValueMD5
    {
        get => _hashValueMD5;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = IsUpperCaseHashes? value.ToUpper() : value.ToLower();
            }
            if (value != _hashValueMD5)
            {
                _hashValueMD5 = value;
            }
            OnPropertyChanged(nameof(HashValueMD5));
        }
    }

    private string _hashValueSHA1 = null;

    public string HashValueSHA1
    {
        get => _hashValueSHA1;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = IsUpperCaseHashes? value.ToUpper() : value.ToLower();
            }
            if (value != _hashValueSHA1)
            {
                _hashValueSHA1 = value;
            }
            OnPropertyChanged(nameof(HashValueSHA1));
        }
    }


    private string _hashValueSHA256 = null;

    public string HashValueSHA256
    {
        get => _hashValueSHA256;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = IsUpperCaseHashes? value.ToUpper() : value.ToLower();
            }
            if (value != _hashValueSHA256)
            {
                _hashValueSHA256 = value;
            }
            OnPropertyChanged(nameof(HashValueSHA256));
        }
    }

    private string _hashValueSHA512 = null;

    public string HashValueSHA512
    {
        get => _hashValueSHA512;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = IsUpperCaseHashes? value.ToUpper() : value.ToLower();
            }
            if (value != _hashValueSHA512)
            {
                _hashValueSHA512 = value;
            }
            OnPropertyChanged(nameof(HashValueSHA512));
        }
    }

    private string _verifiedHashValue = null;

    public string VerifiedHashValue
    {
        get => _verifiedHashValue;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                value = IsUpperCaseHashes? value.ToUpper() : value.ToLower();
            }
            if (value != _verifiedHashValue)
            {
                _verifiedHashValue = value;
            }
            OnPropertyChanged(nameof(VerifiedHashValue));

        }
    }


    public virtual void InvalidateHashValues()
    {
        HashValueMD5 = null;
        HashValueSHA1 = null;
        HashValueSHA256 = null;
        HashValueSHA512 = null;
        RefreshIsHashesOutdated();
    }

    public bool RefreshInputDataSufficient()
    {
        return GetIsInputDataSufficient();
    }

    protected bool GetIsInputDataSufficient()
    {
        bool isSufficient = false;
        if (IsFileHashing)
        {

            isSufficient = !string.IsNullOrEmpty(FilePath)
                && File.Exists(FilePath);
            if (isSufficient) 
            { 
                FileInfo fi = new FileInfo(_filePath);
                isSufficient = fi.Length > 0;
            }
        } else if (IsTextHashing)
        {
            isSufficient = !string.IsNullOrEmpty(TextToHash);
        }
        if (isSufficient!=_isInputDataSufficient)
        {
            _isInputDataSufficient = isSufficient;
            OnPropertyChanged(nameof(IsInputDataSufficient));
            RefreshIsHashesOutdated();
        }
        return isSufficient;
    }

    private bool _isInputDataSufficient = false;

    public bool IsInputDataSufficient
    {
        get => GetIsInputDataSufficient();
    }

    
    public bool IsCalculationButtonsEnabled
    {
        get => IsInputDataSufficient && IsHashesOutdated && !IsCalculating;
    }


    public string TextEntryLabelText
    {
        get => IsFileHashing ? "File Content Preview:" : "Enter Hashed Text Below:";
    }

    private string _textToHash = null;


    public string TextToHash
    {
        get => _textToHash;
        set
        {
            if (value != _textToHash)
            {
                _textToHash = value;
                OnPropertyChanged(nameof(TextToHash));
                if (IsTextHashing)
                {
                    InvalidateHashValues();
                    LastTextToHashWhenTextHashing = _textToHash;
                }
                RefreshInputDataSufficient();
                RefreshIsHashesOutdated();
            }
        }
    }

    protected string LastTextToHashWhenFileHashing { get; set; } = null;

    protected string LastTextToHashWhenTextHashing { get; set; } = null;




}
