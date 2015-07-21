# Sensorberg SDK for Windows BETA #

# Please note there is a BETA release. There is some issues that still need to be resolved.
Check the list of [issues](https://github.com/sensorberg-dev/windows10-sdk/issues) to see all issues.

## Compatibility ##

Sensorberg SDK for Windows is supported on Windows 10.

Sensorberg SDK has a dependency to SQLite library, which does not support
"Any CPU" configuration. Thus, your Sensorberg application will only support
x86, x64 and ARM builds.


## Taking Sensorberg SDK into use ##

### Prerequisites ###

VSIX package "Universal App Platform development using Visual Studio 2015 CTP"
needs to be installed to Visual Studio 2015. Package is available at
http://www.sqlite.org/download.html


### 1. Add Sensorberg SDK projects into your solution ###

Right click the solution in **Solution Explorer**, select **Add** and
**Existing Project...**

![Adding Sensorberg SDK projects](/Doc/Images/AddingExistingProject.png)

Browse to the folder where you have the two Sensorberg SDK projects
(`SensorbergSDK` and `SensorbergSDKBackground`) and select the `csproj` files of
both projects. Note that you may have to add the projects one by one.

### 2. Add Sensorberg SDK project references ###

Add the two SDK projects to your application project as references. Right click
**References** under your application project and select **Add Reference...**

![Adding reference](/Doc/Images/AddingReference.png)

Locate the two SDK projects and make sure that the check boxes in front of them
are checked and click **OK**.
 
![Adding SDK projects as references](/Doc/Images/AddingSDKProjectsAsReference.png)


### 3. Declare background tasks in manifest file ###

Add the following `Extensions` into your `Package.appxmanifest` file:

```xml
  <Applications>
    <Application Id="App"
      Executable="$targetnametoken$.exe"
      EntryPoint="MySensorbergApp.App">

      ...

      <Extensions>
        <Extension Category="windows.backgroundTasks" EntryPoint="SensorbergSDKBackground.AdvertisementWatcherBackgroundTask">
          <BackgroundTasks>
            <Task Type="bluetooth" />
          </BackgroundTasks>
        </Extension>
        <Extension Category="windows.backgroundTasks" EntryPoint="SensorbergSDKBackground.TimedBackgroundTask">
          <BackgroundTasks>
            <Task Type="timer" />
          </BackgroundTasks>
        </Extension>
      </Extensions>
      
      ...
      
    </Application>
  </Applications>
```


### 4. Declare capabilities in manifest file ###

Make sure that you have at least `internetClient` and `bluetooth` capabilities
declared in your `Package.appxmanifest` file:

```xaml
  ...
  
  </Applications>

  <Capabilities>
    <Capability Name="internetClient" />
    <DeviceCapability Name="bluetooth" />
  </Capabilities>
</Package>
```


### 5. Take SDKManager into use ###

The following snippet demonstrates how to integrate the Sensorberg SDK
functionality to the main page of your application:

```cs
using SensorbergSDK;
using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace MySensorbergApp
{
    public sealed partial class MainPage : Page
    {
        private SDKManager _sdkManager;
        
        public MainPage()
        {
            this.InitializeComponent();

            _sdkManager = SDKManager.Instance(0x1234, 0xBEAC); // Manufacturer ID and beacon code
            
            _sdkManager.BeaconActionResolved += OnBeaconActionResolvedAsync;
            Window.Current.VisibilityChanged += SDKManager.Instance.OnApplicationVisibilityChanged;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            BeaconAction pendingBeaconAction = BeaconAction.FromNavigationEventArgs(e);

            if (pendingBeaconAction != null)
            {
                _sdkManager.ClearPendingActions();

                if (await pendingBeaconAction.LaunchWebBrowserAsync())
                {
                    Application.Current.Exit();
                }
                else
                {
                    OnBeaconActionResolvedAsync(this, pendingBeaconAction);
                }
            }

            _sdkManager.InitializeAsync("04a709a208c83e2bc0ec66871c46d35af49efde5151032b3e865768bbf878db8");
            await _sdkManager.RegisterBackgroundTaskAsync();
        }
        
        private async void OnBeaconActionResolvedAsync(object sender, BeaconAction e)
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    MessageDialog messageDialog = e.ToMessageDialog();
                    await messageDialog.ShowAsync();
                });
        }
    }
}        
```

In `OnNavigatedTo` method we first check for pending actions. If the background
task has created a notification and the user clicks/taps the notification, your
application is launched with the pending action in `NavigationEventArgs`. It is
recommended to check the pending actions before initializing the SDK. Otherwise,
**all** the pending actions are delivered to `OnBeaconActionResolvedAsync` event
handler. Note that we clear all the pending actions and react only to the action
associated with the notification the user clicked/tapped. All the notifications
"remember" their actions so calling `ClearPendingActions` method will not affect
them.

Sensorberg SDK is initialized with `InitializeAsync` method, which takes your
API key as the only argument. For creating your service and API key, visit
https://manage.sensorberg.com

You must implement the handling of the beacon actions in
`OnBeaconActionResolvedAsync()`. In the example above, we simply display a
message dialog with the action content.

It is also highly recommended to ask the user for the permission to enable the
background task. Notifications are created automatically by the background task.
You can register and unregister the background task using `SDKManager` methods
`RegisterBackgroundTaskAsync` and `UnregisterBackgroundTask`.


## SDKManager public interface ##

### Events ###

| Name | Argument | Description |
| ---- | -------- | ----------- |
| BackgroundFiltersUpdated | - | Fired, when the filters containing beacon IDs for the background task trigger have been updated. |
| BeaconActionResolved | BeaconAction | Fired, when a beacon action has been resolved and the application should react to it. |
| FailedToResolveBeaconAction | string | Fired, when the SDK fails to resolve an action associated with a beacon event. Typically this event can be ignored. |
| LayoutValidityChanged | bool | Fired, when a layout (contains known beacons and actions associated with them) is either invalidated or validated. |
| ScannerStatusChanged | ScannerStatus | Fired, when the status of the scanner changes. Useful for detecting if the bluetooth has been turned off on the device (status will have value `Aborted`). |


### Properties ###

| Name | Type | Description |
| ---- | ---- | ----------- |
| Scanner | Scanner | The scanner instance. Guaranteed to be not null. |
| ApiKey | string | The API key for Sensorberg service. The recommended way to set the API key is to call `SDKManager.InitializeAsync` method. |
| ManufacturerId | UInt16 | The manufacturer ID for filtering the beacons. |
| BeaconCode | UInt16 | The beacon code for filtering the beacons. |
| IsInitialized | bool | True, if the SDK has been initialized. |
| IsBackgroundTaskRegistered | bool | True, if the background tasks have been registered. |
| IsScannerStarted | bool | True, if the scanner is running. |
| IsLayoutValid | bool | True, if the layout (contains known beacons and actions associated with them) is valid. |


### Methods ###

| Name | Arguments | Returns | Description |
| ---- | --------- | ------- | ----------- |
| Instance (static) | UInt16 manufacturerId, UInt16 beaconCode | SDKManager | Returns the singleton instance of SDKManager class. Takes the manufacturer ID and beacon code for filtering the beacons as arguments. |
| LaunchBluetoothSettingsAsync | - | - | Displays the device bluetooth settings. For allowing user to enable bluetooth, if turned off. |
| InitializeAsync | string apiKey | - | Initializes the SDK. Note that the API key is stored in the application settings automatically. |
| Deinitialize | bool stopScanner | - | De-initializes the SDK. |
| RegisterBackgroundTaskAsync | - | BackgroundTaskRegistrationResult | Registers the background tasks. Note that the API key should be set before registering the background tasks. |
| UpdateBackgroundTaskIfNeededAsync | - | BackgroundTaskRegistrationResult | Re-registers the background task. Useful, if there is a need to update the background task trigger filters. Note that in usual scenarios there is no need to call this method since the SDK updates the background trigger automatically. |
| UnregisterBackgroundTask | - | - | Unregisters the background tasks. |
| StartScanner | - | - | Starts the scanner. It is not necessary to use the scanner, if the background task is registered as the background task will receive beacon actions even when the application is on foreground. The resolved actions are delivered to the application by the background task. |
| StopScanner | - | - | Stops the scanner. |
| ClearPendingActions | - | - | Clears all pending actions (resolved by the background task) from the database. |
| InvalidateCacheAsync | - | - | Invalidates the cache including layout. |
| OnApplicationVisibilityChanged | object, VisibilityChangedEventArgs | - | Event handler for `Window.Current.VisibilityChanged`. Sets the SDK state based on the application visibility. |

## Working with the database ##

Sensorberg SDK stores data in SQLite database. Physical database file locates in application's home folder. When working on the desktop, the file locates in following place: 
C:\Users\<username>\AppData\Local\Packages\<AppID>\LocalState\sensorberg.db
Tools like DB Browser for SQLite can provide interesting insight on how the SDK works. You can for instance observe incoming beacon events that the background agent stores by looking in DBBackgroundEventsHistory table.

### Database structure ###

| Name | Description | 
| ---- | --------- | 
| DBBackgroundEventsHistory | Background agent stores all raw beacon events that it receives from the bluetooth device in this table. When new beacon is seen for the first time, it is stored in the table and this will cause beacon enter event. When the same beacon is seen again only EventTime is updated. And finally when beacon is not seen in 9 seconds it will trigger exit event for the beacon and it will be deleted from the database. |
| DBBEaconActionFromBackground | This table is a communication channel between background task and UI application. When the background agent has recognized new action it stores it here and foreground application reads it and processes them. |
| DBDelayedAction |  All delayed actions are stored here. Both background task and foreground UI regularly check if there are delayed actions that should be executed.  |
| DBHistoryAction |  Stores all actions that have been executed. The table data is used to ensure that same action is not shown twice if there are suppression rules that prohibit it. Content is regularly send to the cloud service |
| DBHistoryEvent |  Event history that is regularly send to the cloud service |

DB Browser for SQLite shows an event in DBBackgroundEventsHistory table

![Adding Sensorberg SDK projects](/Doc/Images/DBBrowserforSQLite.png)

## Sample applications ##

### Sensorberg Simple App ###

Sensorberg Simple App demonstrates how to integrate Sensorberg SDK to a
universal Windows app on a fairly basic level. The aim of this sample is to
provide reusable code to enable quick getting started experience while still
utilizing most of what the SDK has to offer.

![Sensorberg Simple App running on Lumia phone](/Doc/Images/SensorbergSimpleAppSmallScreenScaled.png)
![Sensorberg Simple App running on Surface 3](/Doc/Images/SensorbergSimpleAppLargeDisplayScaled.png)

All of the application logic including the Sensorberg SDK specific application
code is located in [MainPage.xaml.cs](/SensorbergSimpleApp/MainPage.xaml.cs)
file.

### Sensorberg Showcase ###

Sensorberg Showcase demonstrates the full capabilities of Sensorberg SDK. Rather
than being a code sample, it is designed for testing of both the SDK itself and
other applications. With Sensorberg Showcase you can verify that your API key is
valid and that you beacons and campaigns are defined correctly.

![Sensorberg Showcase running on Lumia phone](/Doc/Images/SensorbergShowcaseSmallScreenScaled.png)
![Sensorberg Showcase running on Surface 3](/Doc/Images/SensorbergShowcaseLargeDisplayScaled.png)

This showcase application has the following features (amongst others):

* Beacon scanner with extensive user interface displaying the details of scanned beacons
* Advertiser, which allows you to set the desired beacon IDs making your device act as a beacon
* Dynamic API key - if you have more than one applications, you can switch the API key dynamically or reset back to the demo API key