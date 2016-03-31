using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SubtitleBroom
{
    public class SubtitleData : INotifyPropertyChanged
    {
        public SubtitleData()
        {
            AvailableVideos = new ObservableCollection<string>();
        }

        public bool IsInitialized { get; set; }

        private bool isActive;
        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (value == isActive)
                    return;

                isActive = value;
                OnPropertyChanged();
            }
        }

        public FileInfo Subtitle { get; set; }

        public ObservableCollection<string> AvailableVideos { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
