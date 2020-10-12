using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMBClient.ViewModels
{
    public static class DialogService
    {
        public delegate Task AsyncEventHandler<in TArgs>(object? sender, TArgs args);

        public static event AsyncEventHandler<object>? DialogRequested;

        public static async Task ShowDialog(object dialogModel)
        {
            await InvokeAsync(DialogRequested, dialogModel);
        }

        private static async Task InvokeAsync<T>(AsyncEventHandler<T>? @event, T eventArgs)
        {
            if (@event != null)
            {
                await Task.WhenAll(@event.GetInvocationList().Select(eventHandler => ((AsyncEventHandler<T>)eventHandler).Invoke(null, eventArgs)));
            }
        }
    }
}
