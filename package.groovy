def version = "1." + (env.BRANCH_NAME == "master" ? ("2." + env.BUILD_NUMBER) : ("1." + env.BUILD_NUMBER + "-beta"));
def versionVSIX = "1.2." + env.BUILD_NUMBER;

echo "version-"+version
echo "VSIX Version-"+versionVSIX

try {

    stage 'nuget restore'
    bat '"C:\\Program Files (x86)\\NuGet\\Visual Studio 2015\\nuget.exe" restore SensorbergAll.sln'

    def msbuild = tool 'Main';
    
    stage 'build AnyCPU'
    bat "\"${msbuild}\" /p:Platform=AnyCPU /p:Configuration=Release SensorbergSDK/SensorbergSDK.csproj"
    
    stage 'build arm'
    bat "\"${msbuild}\" /p:Platform=ARM /p:Configuration=Release SensorbergSDK/SensorbergSDK.csproj"

    stage 'build x64'
    bat "\"${msbuild}\" /p:Platform=x64 /p:Configuration=Release SensorbergSDK/SensorbergSDK.csproj"

    stage 'build x86'
    bat "\"${msbuild}\" /p:Platform=x86 /p:Configuration=Release SensorbergSDK/SensorbergSDK.csproj"
    
    stage 'patch files'
    def mani = readFile encoding: 'UTF-8', file: 'VSIX_Packaging/source.extension.vsixmanifest'
    mani = mani.replaceAll('0\\.5', versionVSIX);
    println(mani)
    writeFile encoding: 'UTF-8', file: 'VSIX_Packaging/source.extension.vsixmanifest', text: mani
    
    
    def ass = readFile encoding: 'UTF-8', file: 'SensorbergSDK/Properties/AssemblyInfo.cs'
    ass = ass.replaceAll('1\\.0\\.0\\.1', versionVSIX);
    println(ass)
    writeFile encoding: 'UTF-8', file: 'SensorbergSDK/Properties/AssemblyInfo.cs', text: ass
	
	
    def sdkMani = readFile encoding: 'UTF-8', file: 'VSIX_Packaging/SDKManifest.xml'
    sdkMani = sdkMani.replaceAll('0\\.5', version);
    println(sdkMani)
    writeFile encoding: 'UTF-8', file: 'VSIX_Packaging/SDKManifest.xml', text: sdkMani
    
    
    def releaseNotes = readFile encoding: 'UTF-8', file: 'VSIX_Packaging/ReleaseNotes.txt'
    releaseNotes = releaseNotes.replaceAll('0\\.5', version).replaceAll('\\$date', new Date().format("dd 'of' MMM yyyy HH:mm"));
    println(releaseNotes)
    writeFile encoding: 'UTF-8', file: 'VSIX_Packaging/ReleaseNotes.txt', text: releaseNotes
    
    def nugetPackageText = readFile encoding: 'UTF-8', file: 'SensorbergSDK/nuget/SensorbergSDK.nuspec.tmpl'
    nugetPackageText = nugetPackageText.replaceAll('0\\.6\\.7-alpha', version)
    println(nugetPackageText)
    writeFile encoding: 'UTF-8', file: 'SensorbergSDK/nuget/SensorbergSDK.nuspec', text: nugetPackageText

    stage 'package vsix'
    bat "\"${msbuild}\" VSIX_Packaging.sln"
    
    stage 'package nuget'
	bat "del *.nupkg"
    bat "createnuget.bat"
    
    stage 'install vsix package'
    //bat "\"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\IDE\\VSIXInstaller.exe\" /q /a VSIX_Packaging\\bin\\Release\\SensorbergSDK.vsix"
    
    archive 'VSIX_Packaging/bin/Release/*.vsix'
    archive 'SensorbergSDK*.nupkg'
    
	def sub = env.JOB_NAME+' - Build '+env.BUILD_NUMBER+' - '+(currentBuild.result == null? "STABLE":currentBuild.result)
	emailext body: currentBuild.toString(), subject: sub , to: '$DEFAULT_RECIPIENTS'
}
catch(e) {
    node {
		def sub = env.JOB_NAME+' - Build '+env.BUILD_NUMBER+' - FAILED'
		emailext body: "${env.JOB_NAME} failed with ${e.message}", subject: sub , to: '$DEFAULT_RECIPIENTS'
    }
    throw e
}
