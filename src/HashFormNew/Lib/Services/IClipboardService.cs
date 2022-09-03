using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Services.Maps;

namespace IG.App
{

    public interface IClipboardService
    {

        /// <summary>Tests whether the clipboard service contains text.</summary>
        bool HasText { get; }

        /// <summary>Sets clipboard's text.
        /// <para>WARNING:</para>
        /// <para>Must be called on window's Dispatcher thread (Main thread), otherwise exception is thrown.
        /// From any thread, you should call <see cref="SetText(string)"/> instead.</para></summary>
        /// <param name="text">Text that is set in the clipboard.</param>
        Task SetTextAsync(string text);

        /// <summary>Gets the text from clipboard.
        /// <para>WARNING:</para>
        /// <para>Must be called on window's Dispatcher thread (Main thread), otherwise exception is thrown.
        /// From any thread, you should call <see cref="GetText(Action{string}))"/> instead.</para></summary>
        Task<string> GetTextAsync();

        /// <summary>Sets text on the clipboard. Can be called from any thread.</summary>
        /// <param name="text">Text to be stored to clipboard.</param>
        void SetText(string text);

        /// <summary>Gets text from the clipboard via specified callback.</summary>
        /// <param name="callback">Callback that gets executed, and gets the clipboard's text in parameter.
        /// In this callback, you can do what is necessary with the clipboard text.</param>
        void GetText(Action<string> callback);

    }
}