using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
//using Windows.Services.Maps;

namespace IG.App
{

    public interface IAlertService
    {

        /// <summary>
        /// <para>WARNING:</para>
        /// <para>Must be called on window's Dispatcher thread (Main thread), otherwise exception is thrown.
        /// From any thread, you should call the <see cref="ShowAlert(string, string, string)"/> instead.</para>
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task ShowAlertAsync(string title, string message, string cancel = "OK");

        /// <summary>
        /// 
        /// <para>WARNING:</para>
        /// <para>Must be called on window's Dispatcher thread (Main thread), otherwise exception is thrown.
        /// From any thread, you should call the <see cref="ShowAlert(string, string, string)"/> instead.</para>
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="accept"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<bool> ShowConfirmationAsync(string title, string message, string accept = "Yes", string cancel = "No");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="cancel"></param>
        void ShowAlert(string title, string message, string cancel = "OK");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="cancel"></param>
        /// <param name="timeoutMs"></param>
        /// <param name="cancellationToken"></param>
        [Obsolete("Within MAUI, there is no simple solution to this problem.")]
        void ShowAlertWithTimeout(string title, string message, string cancel = "OK", int timeoutMs = -1,
            CancellationToken? cancellationToken = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        /// <param name="callback"></param>
        /// <param name="accept"></param>
        /// <param name="cancel"></param>
        void ShowConfirmation(string title, string message, Action<bool> callback,
                              string accept = "Yes", string cancel = "No");


    }
}