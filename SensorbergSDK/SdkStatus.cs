// Created by Kay Czarnotta on 01.09.2016
// 
// Copyright (c) 2016,  Sensorberg
// 
// All rights reserved.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Media.Streaming.Adaptive;
using SensorbergSDK.Internal.Services;

namespace SensorbergSDK
{
    public class SdkStatus : INotifyPropertyChanged
    {
        private bool? _isApiKeyValid;
        private bool? _isResolverReachable;

        public async Task<bool> CheckApiKeysValid()
        {
            ApiKeyHelper helper = new ApiKeyHelper();
            return IsApiKeyValid = await helper.ValidateApiKey(null) == ApiKeyValidationResult.Valid;
        }

        public async Task<bool> CheckResolversReachable()
        {
            if (ServiceManager.ApiConnction == null)
            {
                return false;
            }

            NetworkResult result = ServiceManager.ApiConnction.LastCallResult;

            if (result == NetworkResult.UnknownError)
            {
                await ServiceManager.ApiConnction.LoadSettings();
                result = ServiceManager.ApiConnction.LastCallResult;
            }
            return IsResolverReachable = result != NetworkResult.NetworkError && result != NetworkResult.UnknownError;
        }

        public bool IsApiKeyValid
        {
            get
            {
                if (_isApiKeyValid == null)
                {
                    CheckApiKeysValid().ConfigureAwait(false);
                    _isApiKeyValid = false;
                }
                return _isApiKeyValid.Value;
            }
            set
            {
                _isApiKeyValid = value;
                OnPropertyChanged();
            }
        }

        public bool IsResolverReachable
        {
            get
            {
                if (_isResolverReachable == null)
                {
                    CheckResolversReachable().ConfigureAwait(false);
                    _isApiKeyValid = false;
                }
                return _isResolverReachable.Value;
            }
            set
            {
                _isResolverReachable = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}