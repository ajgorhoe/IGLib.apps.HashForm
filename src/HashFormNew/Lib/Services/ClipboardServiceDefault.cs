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

    /// <summary>Implementation of clipboard service that uses MAUI's default clipboard. Can be injected
    /// into ViewModel classes that should not be aware of UI (views).</summary>
    internal class ClipboardServiceDefault : IClipboardService
    {


        /// <inheritdoc />
        public bool HasText
        {
            get { return Clipboard.Default.HasText; }
        }

#nullable enable

        /// <inheritdoc />
        public async Task<string?> GetTextAsync()
        {
            return await Clipboard.Default.GetTextAsync();
        }

         /// <inheritdoc />
       public async Task SetTextAsync(string? text)
        {
            await Clipboard.Default.SetTextAsync(text);
        }

#nullable disable

        /// <inheritdoc />
        public void SetText(string text)
        {
            _ = Application.Current.MainPage.Dispatcher.Dispatch(async () =>
                await SetTextAsync(text));
        }


        /// <inheritdoc />
        public void GetText(Action<string> callback)
        {
            _ = Application.Current.MainPage.Dispatcher.Dispatch(async () =>
            {
                string text = await GetTextAsync();
                callback(text);
            });
        }


    }
}
