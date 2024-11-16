﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Platform;

namespace Ws2812RealtimeDesktopClient.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected string GetAssemblyResource(string name)
        {
            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            using var stream = assets!.Open(new Uri(name));
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

		protected void RaisePropertyChanged(string propName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
		}

    }
}
