using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SensorbergShowcase.Common;
using SensorbergShowcase.Models;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace SensorbergShowcase.Controls
{
    public sealed partial class QrCodeScanner : UserControl
    {
        private const int IMAGE_WIDTH = 800;
        private const int IMAGE_HEIGHT = 600;
        private readonly DispatcherTimer _timer;
        private bool _scannerIsActive;
        private MediaCapture _cameraCapture;
        private DataReader _datareader;
        private int _dimensions = IMAGE_WIDTH * IMAGE_HEIGHT;
        private SemaphoreSlim _semaphore;
        private DeviceInformationCollection _cameraDevices;

        public event EventHandler<string> QrCodeResolved;
        public event EventHandler ScannerNotAvailable;

        public QrCodeScanner()
        {
            this.InitializeComponent();

            _timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(250)};

            _semaphore = new SemaphoreSlim(1);
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _cameraDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (_cameraDevices.Count < 1)
            {
                OnScannerNotAvailable();
            }
        }

        public async Task StartScanningAsync()
        {
            if (_scannerIsActive)
            {
                return;
            }

            try
            {
                Debug.WriteLine("Attempting to start preview");
                await InitMediaCaptureAsync();
                await _cameraCapture.StartPreviewAsync();
                _timer.Tick += OnTimerTick;
                _timer.Start();
                _scannerIsActive = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to start scanner.");
                OnScannerNotAvailable();
                _timer.Tick -= OnTimerTick;
                _timer.Stop();
                _scannerIsActive = false;
            }
        }

        public async Task StopScanningAsync()
        {
            if (!_scannerIsActive)
            {
                return;
            }
            try
            {
                Debug.WriteLine("Attempting to stop preview");
                _timer.Tick -= OnTimerTick;
                _timer.Stop();

                await _cameraCapture.StopPreviewAsync();
                _cameraCapture.Dispose();

                _scannerIsActive = false;
            }
            catch (Exception)
            {
                Debug.WriteLine("Failed to stop scanner.");
            }
        }

        private async void OnTimerTick(object sender, object o)
        {
            if (!_scannerIsActive)
            {
                return;
            }

            await _semaphore.WaitAsync().ConfigureAwait(false);

            string result = await GetCameraImageAsync();
            if (result != null)
            {
                OnQrCodeResolved(result);
            }

            _semaphore.Release();
        }

        private string PerformImageQRCodeSearch(byte[] luminanceBits)
        {
            Debug.WriteLine("Attempting to resolve image");
            string result = String.Empty;

            QRCodeReader c = new QRCodeReader();
            PhotoCameraLuminanceSource lsource = new PhotoCameraLuminanceSource(IMAGE_WIDTH, IMAGE_HEIGHT);
            lsource.PreviewBufferY = luminanceBits;

            Binarizer zer = new HybridBinarizer(lsource);
            BinaryBitmap bbm = new BinaryBitmap(zer);
            Result r = c.decode(bbm);

            if (r == null)
            {
                return null;
            }

            if (!String.IsNullOrEmpty(r.Text))
            {
                result = r.Text;
            }

            return result;
        }

        private async Task<string> GetCameraImageAsync()
        {
            string result = null;

            using (InMemoryRandomAccessStream imageStream = new InMemoryRandomAccessStream())
            {
                try
                {
                    await _cameraCapture.CapturePhotoToStreamAsync(new ImageEncodingProperties()
                    {
                        Subtype = "BMP",
                        Width = IMAGE_WIDTH,
                        Height = IMAGE_HEIGHT,
                    }, imageStream);

                    await imageStream.FlushAsync();

                    _datareader = new DataReader(imageStream.GetInputStreamAt((ulong)54));
                    await _datareader.LoadAsync((uint)imageStream.Size - 54);

                    byte[] luminanceBits = new byte[_dimensions];

                    uint index = 0;
                    while (_datareader.UnconsumedBufferLength > 0)
                    {
                        var b = _datareader.ReadByte();
                        var g = _datareader.ReadByte();
                        var r = _datareader.ReadByte();
                        _datareader.ReadByte();

                        int luminance = (int)(r * 0.3 + g * 0.59 + b * 0.11);
                        luminanceBits[index] = Convert.ToByte(luminance);
                        index++;
                    }

                    result = PerformImageQRCodeSearch(luminanceBits);
                }
                catch (Exception e)
                {
                    Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        _timer.Tick -= OnTimerTick;
                        _timer.Stop();
                        result = null;
                    });
                }
            }

            return result;
        }

        private void OnQrCodeResolved(string e)
        {
            QrCodeResolved?.Invoke(this, e);
        }

        private void OnScannerNotAvailable()
        {
            ScannerNotAvailable?.Invoke(this, EventArgs.Empty);
        }

        private async Task InitMediaCaptureAsync()
        {
            if (_cameraDevices == null)
            {
                return;
            }
            var deviceType = DeviceTypeHelper.GetCurrentDeviceType();

            //Always launches back camera
            var device = deviceType == Platform.Windows ? _cameraDevices.LastOrDefault() : _cameraDevices.FirstOrDefault();

            _cameraCapture = new MediaCapture();

            await _cameraCapture.InitializeAsync(new MediaCaptureInitializationSettings() {VideoDeviceId = device.Id});

            //Tablets need vertical flip, phones need rotate
            _cameraCapture.SetPreviewRotation(deviceType == Platform.Windows
                ? VideoRotation.Clockwise180Degrees
                : VideoRotation.Clockwise90Degrees);

            CaptureElement.Source = _cameraCapture;
        }
    }
}
