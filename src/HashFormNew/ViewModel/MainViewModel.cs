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
//using Windows.UI.WebUI;
using System.Threading;
using System.Reflection.Metadata.Ecma335;
using System.Linq.Expressions;
using System;
using System.Net.WebSockets;
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

        InitHashMapping();

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


    // Mapping between hash values and UI:

    public class HashMappingElement
    {
        public string HashType { get; init; }

        public Func<string> GetValue { get; init; }

        public Action<string> SetValue { get; init; }

        public Func<bool> IsCalculationRequired { get; init; }
    }

    protected Dictionary<string, HashMappingElement> HashMapping { get; } = new Dictionary<string, HashMappingElement>();

    /// <summary>Returns collection of all available hash types for which hashes can be calculated.</summary>
    public virtual IList<string> HashTypes => HashMapping.Keys.ToList();

    /// <summary>Returns collection of hash types for which calculation of hashes has been required but 
    /// hash valueshasn't yet been calculated.</summary>
    public virtual IList<string> HashTypesMissing =>
        (
            from hashType in HashTypes
            where HashMapping[hashType].IsCalculationRequired() 
                && string.IsNullOrEmpty(HashMapping[hashType].GetValue())
            select hashType
        ).ToList();

    /// <summary>Returns collection of hash types for which hashes are already calcullated.</summary>
    public virtual IList<string> HashTypesCalculated =>
        (
            from hashType in HashTypes
            where !string.IsNullOrEmpty(HashMapping[hashType].GetValue())
            select hashType
        ).ToList();

    /// <summary>Returns collection of hash types for which hashes are already calcullated.</summary>
    public virtual IList<string> HashTypesNotCalculated =>
        (
            from hashType in HashTypes
            where string.IsNullOrEmpty(HashMapping[hashType].GetValue())
            select hashType
        ).ToList();



    protected virtual void InitHashMapping()
    {
        HashMapping[HashConst.MD5Hash] = new HashMappingElement
        {
            HashType = HashConst.MD5Hash,
            GetValue = () => { return HashValueMD5; },
            SetValue = (value) => { HashValueMD5 = value; },
            IsCalculationRequired = () => { return CalculateMD5; }
        };
        HashMapping[HashConst.SHA1Hash] = new HashMappingElement
        {
            HashType = HashConst.SHA1Hash,
            GetValue = () => { return HashValueSHA1; },
            SetValue = (value) => { HashValueSHA1 = value; },
            IsCalculationRequired = () => { return CalculateSHA1; }
        };
        HashMapping[HashConst.SHA256Hash] = new HashMappingElement
        {
            HashType = HashConst.SHA256Hash,
            GetValue = () => { return HashValueSHA256; },
            SetValue = (value) => { HashValueSHA256 = value; },
            IsCalculationRequired = () => { return CalculateSHA256; }
        };
        HashMapping[HashConst.SHA512Hash] = new HashMappingElement
        {
            HashType = HashConst.SHA512Hash,
            GetValue = () => { return HashValueSHA512; },
            SetValue = (value) => { HashValueSHA512 = value; },
            IsCalculationRequired = () => { return CalculateSHA512; }
        };
    }

    public string GetHashValue(string hashType)
    {
        if (!HashMapping.ContainsKey(hashType))
            throw new InvalidOperationException($@"Unknown hash type: ""{hashType}""");
        return HashMapping[hashType].GetValue();
    }

    protected void SetHashValue(string hashType, string hashValue)
    {
        if (!HashMapping.ContainsKey(hashType))
            throw new InvalidOperationException($@"Unknown hash type: ""{hashType}""");
        HashMapping[hashType].SetValue(hashValue);
    }

    public bool IsCalculateHash(string hashType)
    {
        if (!HashMapping.ContainsKey(hashType))
            throw new InvalidOperationException($@"Unknown hash type: ""{hashType}""");
        return HashMapping[hashType].IsCalculationRequired();
    }



    public ICommand AboutCommand => new Command(
        execute: () =>
        {
            string calculatedHashes = "";
            foreach (string hashType in HashTypes)
            {
                calculatedHashes += hashType + " ";
            }
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "About", AppNameAndVersionString + Environment.NewLine
                + Environment.NewLine + "Calculates cryptographic hashes of files and text."
                + Environment.NewLine + Environment.NewLine + "  Hash Types: "
                + Environment.NewLine + calculatedHashes);
        });

    public ICommand HelpCommand => new Command(
        execute: () =>
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "Help", "Help is not yet implemented." + Environment.NewLine + "See also: " +
                Environment.NewLine + "http://www2.arnes.si/~ljc3m2/igor/software/IGLibShellApp/HashForm.html");
        });

    /// <summary>Command that cancelles the current calculation, if any.</summary>
    public ICommand CancelCurrentOpperationCommand => new Command(
        execute: () =>
        {
            var tokenSource = ClassCancellationSource;
            if (tokenSource == null)
            {
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                    "Warning", "No operations available for cancellation.");
            }
            else
            {
                tokenSource.Cancel();
                //ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                //    "Info", "The current calculation has been cancelled.");
            }
        });

    public ICommand CopyHashToClipboardCommand => new Command<string>(
            execute: (string hashType) =>
            {
                string hashValue = GetHashValue(hashType);
                ServiceProvider?.GetService<IG.App.IClipboardService>()?.SetText(hashValue);

                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                        "Hash Copied",
                        $"Hash {hashType} copied to clipboard: "
                        + Environment.NewLine + hashValue, "OK");

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

    public ICommand VerifyHashCommand => new Command(
            execute: async () =>
            {
                await VerifyHashAsync();
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


    /// <summary>Used to cancel potentially long lasting operations.</summary>
    protected CancellationTokenSource ClassCancellationSource { get; set; } = null;

    /// <summary>Initiates a calculation task: increments the number of active calculation tasks (possibly nested or parallel),
    /// provisions cancellation token source for the new task, and possibly stores the cancellation token in 
    /// <see cref="ClassCancellationSource"/> such that the calculation task(s) can be cancelled via this source.
    /// <para>See also remarks.</para></summary>
    /// <remarks>
    /// The pair <see cref="InititeCalculationTask(CancellationTokenSource)"/> / <see cref="FinalizeCalcullationTask(CancellationTokenSource, CancellationTokenSource)(CancellationTokenSource)"/>
    /// provides a way to standardize initiation and finalization of calculation tasks triggered externally or internally. 
    /// this in particular refers to handling the class level <see cref="ClassCancellationSource"/> that is used to
    /// cancel the current operations (externally or internally), to count the number of possibly nested calculation tasks
    /// currently executing (<see cref="NumActiveCalculationTasks"/>), update information on whether calculation is 
    /// currently being performed (<see cref="IsCalculating"/>), and to update the state after each calculation task is 
    /// completed (i.e., <see cref="IsHashesOutdated"/>). This is the single place where handling of these things is 
    /// modified.
    /// </remarks>
    /// <param name="externalCancellationSource">The eventual external cancellation token source provided to the task
    /// by its caller, indicating that the current task is nested into (subordinate to) a broader task package.</param>
    /// <returns>The actual cancellation token source that can be used by the task (which can be either the 
    /// <paramref name="externalCancellationSource"/> provided by the caller, or object stored in <see cref="ClassCancellationSource"/>,
    /// or oblect creaated anew).</returns>
    protected virtual CancellationTokenSource InititeCalculationTask(CancellationTokenSource externalCancellationSource)
    {
        ++NumActiveCalculationTasks;
        CancellationTokenSource cancellationSource = externalCancellationSource;
        if (cancellationSource == null)
        {
            // externalCancellationSource not provided, the current method is responsible to provide
            // means of cancellation:
            if (ClassCancellationSource != null && !ClassCancellationSource.IsCancellationRequested)
            {
                // Reuse the current class' cancellation token source:
                cancellationSource = ClassCancellationSource;
            }
            else
            {
                // Create a new cancellation token source and set the class cancellation source to it:
                cancellationSource = ClassCancellationSource = new CancellationTokenSource();
            }
        }
        return cancellationSource;
    }

    /// <summary>Finalizes common aspects of the current calculation task, i.e., decrements the number of active
    /// calculation tasks (<see cref="NumActiveCalculationTasks"/>), and cleans up the class' cancellatio ntoken 
    /// source.
    /// <para>See also remarks for <see cref="InitHashMapping"/>.</para></summary>
    /// <param name="externalCancellationSource">The eventual external cancellation token source provided to the task
    /// by its caller, indicating that the current task is nested into (subordinate to) a broader task package.
    /// <para>For tasks for which external <see cref="CancellationTokenSource"/> is not provided, this paremeter should
    /// be set to null.</para></param>
    /// <param name="actualCancellationSourceUsed">Actual <see cref="CancellationTokenSource"/> that was used by the 
    /// task. If this is set to null but cancellation token source was used then the returned informaition will not
    /// necessarily be correct.</param>
    /// <returns>Value indicating whether the task that is being finalized was cancelled. This information is correct
    /// only when the parameter <paramref name="actualCancellationSourceUsed"/> is provided.</returns>
    protected virtual bool FinalizeCalcullationTask(CancellationTokenSource externalCancellationSource,
        CancellationTokenSource actualCancellationSourceUsed)
    {
        bool wasCancelled = false;
        if (actualCancellationSourceUsed!=null)
        {
            wasCancelled = actualCancellationSourceUsed.IsCancellationRequested;
        }
        --NumActiveCalculationTasks;
        if (NumActiveCalculationTasks <= 0)
        {
            CancellationTokenSource classCancellationSource = ClassCancellationSource;
            if (classCancellationSource != null)
            {
                ClassCancellationSource = null;
                classCancellationSource.Dispose();
            }
        }
        RefreshIsHashesOutdated();
        return wasCancelled;
    }

    /// <summary>Triggers calculation of missing hash values IF the <see cref="CalculateHashesAutomatically"/> is set to
    /// true and no other hash calculation is currently going on. It calls <see cref="RefreshIsHashesOutdated"/>
    /// before triggering calculation.
    /// <para>This method will not block the calling method, which will resume almost immediately after the call.</para>
    /// <para>The method is used to trigger calculation of missing hash values in asynchronous methods and property
    /// setters that cause state changes that make the calculated hash values insufficient (also when values are cleared
    /// because they bcome invalid).</para>
    /// <para>In async methods, call "<see cref="RefreshIsHashesOutdated"/>; await <see cref="CalculateMissingHashesAsync()"/>" instead.</para></summary>
    public void RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic()
    {
        if (RefreshIsHashesOutdated())
        {
            if (CalculateHashesAutomatically && !IsCalculating)
            {
                var exec = async () =>
                {
                    try
                    {
                        await CalculateMissingHashesAsync();
                    }
                    catch { }
                    finally
                    {
                        // isOutdated = GetHashesOutdated();
                    }
                };
                exec();
            }
        }
    }

    /// <summary>Calculate hash function of specific type on the current input from this class.</summary>
    /// <param name="hashType">Typ of the hash function applied.</param>
    /// <param name="externalCancellationSource">Optional cancellation token source that can be used to cancel the calculation. It can be null.</param>
    /// <returns>The Task object through which calculaion results will be accessible.</returns>
    public async Task<string> CalculateHashAsync(string hashType, 
        CancellationTokenSource externalCancellationSource = null)
    {
        bool exceptionOccurred = false;
        string ret = null;
        CancellationTokenSource cancellationSource = null;
        try
        {
            cancellationSource = InititeCalculationTask(externalCancellationSource);
            CancellationToken cancellationToken = cancellationSource.Token;
            Func<Task<string>> hashTask = null;
            // Define the calculation operation based on file / text input.
            if (IsTextHashing)
            {
                if (string.IsNullOrEmpty(this.TextToHash))
                    throw new InvalidOperationException("Hash computation: Text to be hashed is empty.");
                hashTask = async () =>
                {
                    try
                    {
                        return await HashCalculator.CalculateTextHashStringAsync(hashType, TextToHash, cancellationToken);
                    }
                    catch
                    {
                        return null;
                    }
                };
            }
            else if (IsFileHashing)
            {
                if (string.IsNullOrEmpty(FilePath))
                {
                    throw new ArgumentException("Hash computation: Path to file to be hashed is not specified.");
                }
                if (!File.Exists(FilePath))
                {
                    throw new InvalidOperationException("Hash computation: File to be hashed does not exist.");
                }
                hashTask = async () =>
                {
                    try
                    {
                        return await HashCalculator.CalculateFileHashStringAsync(hashType, FilePath, cancellationToken);
                    }
                    catch
                    {
                        return null;
                    }
                };
            }
            else
            {
                throw new InvalidOperationException("Neither file input nor text input is active.");
            }
            ret = await Task.Run(hashTask);
            SetHashValue(hashType, ret);
        }
        catch (OperationCanceledException ex)
        {
            exceptionOccurred = true;
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("Warning",
                $"Calculation of {hashType} hash was cancelled and {ex.GetType().Name} thrown.");
        }
        catch (Exception ex)
        {
            exceptionOccurred = true;
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("ERROR",
                $"Exception was thrown when calculating the {hashType} hash." + Environment.NewLine
                + $"Type: {ex.GetType().FullName}" + Environment.NewLine
                + "Message: " + Environment.NewLine + ex.Message);
        }
        finally
        {
            bool wasCancelled = FinalizeCalcullationTask(externalCancellationSource, cancellationSource);
            if (externalCancellationSource == null && !exceptionOccurred && cancellationSource.IsCancellationRequested)
            {
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("Warning",
                    $"Calculation of the {hashType} has been cancelled.");
            }
        }
        return ret;
    }

    /// <summary>For the specified hash value, verifies whether any of the hassh values of the current content
    /// corresponds to that value, and returns true if yes. It also launches an alert informing the user of the
    /// matchng hash.</summary>
    /// <param name="comparedHashValue">The provided hash values that should be verified against supported hash functions.
    /// If null then this parameter si set to the currentn value of <see cref="VerifiedHashValue"/> property.</param>
    /// <param name="externalCancellationSource">The eventual external cancellation token source provided to the task
    /// by its caller, indicating that the current task is nested into (subordinate to) a broader task package.</param>
    /// <param name="canceelWhenMatchObtained">canceel soon as match is obtained. Default is false.</param>
    /// <returns>String identifying the type of the hash function that correspond to the verified hash value, or null
    /// when no corresponding hash type could be identified among the supported hash types (this can also happen when
    /// the operation was cancelled prematurely, or error occurred, or there is insufficient data for hash calculation).</returns>
    public async Task<string> VerifyHashAsync(string comparedHashValue = null, 
        CancellationTokenSource externalCancellationSource = null, bool canceelWhenMatchObtained = false)
    {
        if (string.IsNullOrEmpty(comparedHashValue))
        {
            // If parameter not defined, take the VerifiedHashValue property:
            comparedHashValue = VerifiedHashValue;
        }
        if (comparedHashValue != null)
        {
            comparedHashValue = IsUpperCaseHashes ? comparedHashValue.ToUpper() : comparedHashValue.ToLower();
        }
        VerifiedHashValue = comparedHashValue;  // store (back) to class-level property
        if (string.IsNullOrEmpty(comparedHashValue))
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "Warning", "Hash value to be verified is not specified.");
            return null;
        }
        string matchedHashType = null;
        bool notYetReportedAMatch = true;
        // Check whether we have a matching hash among already calculated hashes:
        foreach (string hashType in HashTypesCalculated)
        {
            string value = GetHashValue(hashType);
            if (value == comparedHashValue)
            {
                matchedHashType = hashType;
                break;
            }
        }
        if (matchedHashType == null)
        {
            CancellationTokenSource cancellationSource = null;
            try
            {
                cancellationSource = InititeCalculationTask(externalCancellationSource: null);
                //StringBuilder sb = new StringBuilder();  // <DebugInfo> 
                // Matched value not found among already calculated hash values; we need to calculate
                // the other hashes and compare it to the value:
                await CalculateMissingHashesAsync(cancellationSource, HashTypesNotCalculated, 
                    (hashType, hashValue) =>
                    {
                        //sb.AppendLine($"{hashType}: {hashValue}");  // <DebugInfo> 
                        // Callback that is executed every time a new hash value is calculated:
                        if (comparedHashValue == hashValue)
                        {
                            //sb.AppendLine($"  Matching hash: {hashType}");  // <DebugInfo> 
                            // We have found the hash type for which hash value matches the value to be checked:
                            matchedHashType = hashType;
                            {
                                notYetReportedAMatch = false;
                                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                                    "Info", "The specified hash value: " + Environment.NewLine
                                    + $"  {comparedHashValue}" + Environment.NewLine
                                    + $"corresponds to the {matchedHashType} hash of the specified {(IsFileHashing ? "file" : "text")}.");
                            }
                            if (canceelWhenMatchObtained)
                            {
                                // If we don't need to continue calculation for other hashes after we the matching hash type was found:
                                cancellationSource.Cancel();
                            }
                        }
                    });

                //ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("Info",  // <DebugInfo> 
                //$"Calculation Calculated hashes matching:" + Environment.NewLine  // <DebugInfo> 
                //      + sb.ToString());  // <DebugInfo> 
            }
            catch 
            { }
            finally
            {
                bool wasCancelled = FinalizeCalcullationTask(externalCancellationSource, cancellationSource);
            }
        }
        if (string.IsNullOrEmpty(matchedHashType))
        {
            ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                "WARNING", "The specified hash value: " + Environment.NewLine
                + $"  {comparedHashValue}" + Environment.NewLine
                + "does NOT correspond to any type of hashes considered by this application.");
        }
        else
        {
            if (notYetReportedAMatch)
            {
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert(
                    "Info", "The specified hash value: " + Environment.NewLine
                    + $"  {comparedHashValue}" + Environment.NewLine
                    + $"corresponds to the {matchedHashType} hash of the specified {(IsFileHashing ? "file" : "text")}.");
            }
        }
        return matchedHashType;
    }


    /// <summary>Just calls <see cref="CalculateMissingHashesAsync(CancellationTokenSource, IList{string}, Action{string, string})"/>.</summary>
    protected async Task CalculateMissingHashesAsync() { await CalculateMissingHashesAsync(null); }

    /// <summary>Calculates the eventual missing hash values according to the parameters of the current ViewModel.</summary>
    /// <param name="externalCancellationSource">The eventual external cancellation token source provided to the task
    /// by its caller, indicating that the current task is nested into (subordinate to) a broader task package.</param>
    /// <param name="consideredHashTypes">Optional. If specified, it defines the hasg types for which hashes are
    /// calculated, and calculation is performed for the contained hash types regardless of whether hash values are
    /// already available.</param>
    /// <param name="callback">Optional. When specified, this delegate is invoked for each hash value calculated.
    /// Hash type and the calculated hash value are passed to the delegate.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual async Task CalculateMissingHashesAsync(CancellationTokenSource externalCancellationSource = null,
        IList<string> consideredHashTypes = null, Action<string, string> callback = null)
    {

        if (consideredHashTypes == null && !IsHashesOutdated)
        {
            if (externalCancellationSource == null)
            {
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("Info",
                                "Hash values are up to date, nothing calculated.");
            }
            return;
        }
        if (!IsInputDataSufficient)
        {
            if (externalCancellationSource == null)
            {
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("Warning",
                    "There is no sufficient data for hash calculation." + Environment.NewLine
                    + "Please switch to file hash calculation and select a file, or" + Environment.NewLine
                    + "switch to text hash calculation and insert the desired text" + Environment.NewLine
                    + "in the editor before running hash calculation.");
            }
            return;
        }
        if (consideredHashTypes == null)
        {
            // If hash types for which calculation is perrformed are not specified, we take
            // all those for which hases are not yet calculated:
            consideredHashTypes = HashTypesMissing;
        }
        int numHashesCAlculated = 0;
        bool exceptionOccurred = false;
        CancellationTokenSource cancellationSource = null;
        try
        {
            cancellationSource = InititeCalculationTask(externalCancellationSource);
            Dictionary<Task<string>, string> taskTypeMapping = new Dictionary<Task<string>, string>();
            foreach (string hashType in consideredHashTypes)
            {
                Task<string> hashTask = CalculateHashAsync(hashType, cancellationSource);
                taskTypeMapping.Add(hashTask, hashType);
            }
            while (taskTypeMapping.Count > 0)
            {
                Task<string> completedTask = await Task.WhenAny(taskTypeMapping.Keys);
                string hashType = taskTypeMapping[completedTask];
                taskTypeMapping.Remove(completedTask);
                string hashValue = await completedTask;
                // SetHashValue(hashType, hashValue);
                callback?.Invoke (hashType, hashValue);
                ++numHashesCAlculated;
            }
        }
        catch (OperationCanceledException)
        {
            if (externalCancellationSource == null)
            {
                exceptionOccurred = true;
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("Warning",
                    "Operation was cancelled and CancellationException thrown.");
            }
        }
        catch (Exception ex)
        {
            exceptionOccurred = true;
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("ERROR",
                    "Exception was thrown by the operation." + Environment.NewLine
                    + $"Type: {ex.GetType().FullName}" + Environment.NewLine
                    + "Message: " + Environment.NewLine + ex.Message);
        }
        finally
        {
            bool wasCancelled = FinalizeCalcullationTask(externalCancellationSource, cancellationSource);
            if (externalCancellationSource == null && !exceptionOccurred && wasCancelled)
            {
                ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("Warning",
                    "The current hash calculation has been cancelled.");
            }
            if (numHashesCAlculated > 0 && !exceptionOccurred && !wasCancelled)
            {
                // Save results to file if this is switched on:
                if (IsSaveFileHashesEligible)
                {
                    try
                    {
                        SaveHashesToFileAsync();
                    }
                    catch (Exception ex)
                    {
                        ServiceProvider?.GetService<IG.App.IAlertService>()?.ShowAlert("ERROR",
                            "Exception occurrwd when trying to save file hashes:" + Environment.NewLine
                            + ex.Message);
                    }
                }
                // Since this was an outer-most (master) calcullation operation, repeat the calculation of
                // missing hashes in case that additional calculations have been requested by the user 
                // (e.g. by checking additional check boxes while calculation whas going on):
                CancellationTokenSource repeatedCancellationSource = externalCancellationSource;
                if (repeatedCancellationSource == null)
                {
                    repeatedCancellationSource = new CancellationTokenSource();
                }
                await CalculateMissingHashesAsync(repeatedCancellationSource);
            }
        }  // finally

    }  // CalculateMissingHashesAsync(...)


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
                    foreach (string hashType in HashTypes)
                    {
                        string hashValue = GetHashValue(hashType);
                        if (!string.IsNullOrEmpty(hashValue))
                        {
                            sb.AppendLine($"  {hashType}:    " + Environment.NewLine + hashValue);
                        }
                    }
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
                DebugMessage = null;
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
                RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
                OnPropertyChanged(nameof(IsHashesCalculated));
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
                RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
            }
        }
    }


    private string _debugMessage = "<< Debug message >>";

    /// <summary>Intended just for debugging; operations can set this message such that it can be displayed 
    /// in the debugging panel when it is visible.</summary>
    public string DebugMessage { 
        get { return _debugMessage; } 
        set {
            _debugMessage = value;
            OnPropertyChanged(nameof(DebugMessage));
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
                RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
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
                RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
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
                RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
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
                RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
            }
        }
    }




    // <param name="performAutomaticCalculations">If true then automatic calculation of missing hash values is performed.

    /// <summary>Recalculates the <see cref="IsHashesOutdated"/> property.</summary>
    public bool RefreshIsHashesOutdated()
    {
        bool isOutdated = GetHashesOutdated();  // this will also call OnPropertyChanged(nameof(IsHashesOutdated));
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
            OnPropertyChanged(nameof(IsHashesCalculated));
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

    public bool IsHashesCalculated
    {
        get => IsInputDataSufficient && !IsHashesOutdated;
    }


    private int _numActiveCalculationTasks = 0;

    /// <summary>Contains the current number of active cancullation tasks.
    /// <para>
    /// The main purpose of this counter is to know when all active calcullation tasks are completed.
    /// </para></summary>
    public int NumActiveCalculationTasks
    {
        get
        {
            return _numActiveCalculationTasks;
        }
        protected set
        {
            if (value != _numActiveCalculationTasks)
            {
                _numActiveCalculationTasks = value;
                OnPropertyChanged(nameof(NumActiveCalculationTasks));
            }
            IsCalculating = _numActiveCalculationTasks > 0;
            OnPropertyChanged(nameof(IsCalculating));
        }
    }

    private bool _isCalculating = false;

    /// <summary>Wheen changed to true, this also triggers actual calculation of hash values!</summary>
    public bool IsCalculating
    {
        get => _isCalculating;
        protected set
        {
            if (value != _isCalculating)
            {
                _isCalculating = value;
            }
            OnPropertyChanged(nameof(IsCalculating));
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
        // Other properties - to make UI work properly (bugs in MAUI?) - however, the trick does not work at all.
        OnPropertyChanged(nameof(NumActiveCalculationTasks));
        OnPropertyChanged(nameof(IsCalculating));
    }

    public string HashTypeMD5 => HashConst.MD5Hash;

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

    public string HashTypeSHA1 => HashConst.SHA1Hash;

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


    public string HashTypeSHA256 => HashConst.SHA256Hash;

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

    public string HashTypeSHA512 => HashConst.SHA512Hash;

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
        RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
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
            RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
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
                RefreshIsHashesOutdatedAndCalculateMissingHashesIfAutomatic();
            }
        }
    }

    protected string LastTextToHashWhenFileHashing { get; set; } = null;

    protected string LastTextToHashWhenTextHashing { get; set; } = null;


}



