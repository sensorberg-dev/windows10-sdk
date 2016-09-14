using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using SensorbergControlLibrary.Model;

namespace SensorbergSDK.Controls
{
    public sealed partial class BeaconDetailsControl : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty BeaconDetailProperty = DependencyProperty.Register("BeaconDetail", typeof(BeaconDetailsItem), typeof(BeaconDetailsControl), new PropertyMetadata(default(BeaconDetailsItem)));

        public BeaconDetailsControl()
        {
            InitializeComponent();
        }

        public BeaconDetailsItem BeaconDetail
        {
            get { return (BeaconDetailsItem) GetValue(BeaconDetailProperty); }
            set
            {
                SetValue(BeaconDetailProperty, value); 
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
