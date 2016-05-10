To create a VSIX package, you need to do following:

1.	Build all targets of the SensorbergSDK & SensorbergSDKBackground
a.	Select Build -> Batch build
b.	Select arm, x86 & x64 RELEASE builds for both SensorbergSDK & SensorbergSDKBackground
c.	Click rebuild
2.	Open the VSIX project and select release (both release & debug will actually use release libraries)
3.	Select build from the menu & observe that each target reports copying 6 files. Files are copied from SensorbergSDKBackground  release folders. If the files have not been modified (not build since last package was made) the build process resports on output that 0 files were copied. And it will only report error if the release folder files are missing.
4.	The resulting VSIX file should be in bin\release folder now, and can be installed by clicking it

To add the reference to the library:
1.	Right click reference folder
2.	Select add reference from pop-up menu
3.	Check the box for Sensorberg SDK from the Universal Windows / Extensions list
4.	Click Ok

To uninstall previous version of the SDK package:

1.	Remove the reference to the package from all apps using it. 
1.	Right click reference folder
2.	Select add reference from pop-up menu
3.	Uncheck the box for Sensorberg SDK from the Universal Windows / Extensions list
4.	Click Ok
2.	Select Extensions & updates from tools menu
1.	Select SDKs from the installed parts
2.	Select the Sensorberg SDK from the list and click Uninstall
If uninstallation fails, you can try re-starting Visual studio and trying again. Or you can manually delete the files from “C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0\ExtensionSDKs”, just delete the whole SensorbergSDK folder, and verify with re-started Visual studio that it’s not included in the list. Note that the list is only updated when Visual studio starts.
Updating version number for the package
Appears to be that not all version number references are updated accordingly automatically, thus its best to be done manually.
Open the VSIX_Packaging project with Visual Studio and:
1.	Right click source.extension.vsixmanifest and select view code
a.	With Identity –line update the Version to new number
b.	With InstallationTarget –line update the SdkVersion to new number 
2.	Open the SDKManifest.xml file
a.	With Identity –line update the Version to new number
Current 0.5 version also lacks the content for license & release notes. Also Getting started guide & more information link should be added to the source.extension.vsixmanifest file

