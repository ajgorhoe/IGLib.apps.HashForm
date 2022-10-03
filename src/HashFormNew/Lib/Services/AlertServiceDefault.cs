using IG.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// using static System.Net.Mime.MediaTypeNames;
using Microsoft.Maui.Controls;
//using Application = Microsoft.Maui.Controls.Application;

namespace IG.App
{

    /// <summary>Implementation of <see cref="IAlertService"/> service that uses MAUI's alerts. Can be injected
    /// into ViewModel classes, which should not be aware of UIs (views).</summary>
    internal class AlertServiceDefault : IAlertService
    {

        /// <inheritdoc/>
        public Task ShowAlertAsync(string title, string message, string cancel = "OK")
        {
            return Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        /// <inheritdoc/>
        public Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No")
        {
            return Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }


        /// <inheritdoc/>
        public void ShowAlert(string title, string message, string cancel = "OK")
        {
            Application.Current.MainPage.Dispatcher.Dispatch(async () =>
                await ShowAlertAsync(title, message, cancel)
            );
        }

        /// <summary>Initial value for <see cref="DefaultTimoutMs"/></summary>
        public const int InitialDefaultTimeoutMs = 2500;

        private static int _defaultTimoutMs = InitialDefaultTimeoutMs;

        /// <summary>Default timeout, in milliseconds, for all members of this class that take as parameter 
        /// timeout defining how much time  an alert is visible if user does not respond to it.
        /// <para>Initially set to <see cref="InitialDefaultTimeoutMs"/>.</para>
        /// <para>Can be set. When attempting to set it to value equal or less than zero, it is reset to <see cref="InitialDefaultTimeoutMs"/>.</para></summary>
        public static int DefaultTimoutMs
        {
            get { return _defaultTimoutMs; }
            set
            {
                if (value <= 0)
                {
                    _defaultTimoutMs = InitialDefaultTimeoutMs;
                }
                else
                {
                    _defaultTimoutMs = value;
                }
            }
        }

        /// <inheritdoc/>
        [Obsolete("Within MAUI, there is no simple solution to this problem.")]
        public void ShowAlertWithTimeout(string title, string message, string cancel = "OK", int timeoutMs = -1, 
            CancellationToken? cancellationToken = null)
        {
            if (timeoutMs <= 0)
            {
                timeoutMs = DefaultTimoutMs;
            }
            Application.Current.MainPage.Dispatcher.Dispatch(async () =>
            {
                Task task = ShowAlertAsync(title, message, cancel);
                Task waitTask = cancellationToken == null ? task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs)) : 
                    task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs), cancellationToken.Value);
                try
                {
                    await (waitTask);
                }
                catch (Exception ex) 
                {
                    // No real solution to Alert with timeout currently available in MAUI:
                    // This would just launch another alert without timeout after OK button is clicked:
                    ShowAlert("Exception " + ex.GetType() + " thrown.", ex.Message, "OK");
                    // This would break the program:
                    throw; 

                }
            }

            );
        }


        /// <inheritdoc/>
        public void ShowConfirmation(string title, string message, Action<bool> callback,
                                     string accept = "Yes", string cancel = "No")
        {
            Application.Current.MainPage.Dispatcher.Dispatch(async () =>
            {
                bool answer = await ShowConfirmationAsync(title, message, accept, cancel);
                callback(answer);
            });
        }




    }
}
