def version = "1.0." + env.BUILD_NUMBER //+ env.BRANCH_NAME=="develop"?"-beta" : "-RC1";

node ('Windows') {
    
try {
    stage 'checkout'
    checkout scm: [$class: 'GitSCM', branches: [[name: 'develop'], [name: 'master']], userRemoteConfigs: [[url: 'https://github.com/sensorberg-dev/windows10-sdk.git']], clean: true]

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
    mani = mani.replaceAll('0\\.5', version);
    println(mani)
    writeFile encoding: 'UTF-8', file: 'VSIX_Packaging/source.extension.vsixmanifest', text: mani
    
    
    def sdkMani = readFile encoding: 'UTF-8', file: 'VSIX_Packaging/SDKManifest.xml'
    sdkMani = sdkMani.replaceAll('0\\.5', version);
    println(sdkMani)
    writeFile encoding: 'UTF-8', file: 'VSIX_Packaging/SDKManifest.xml', text: sdkMani
    
    
    def releaseNotes = readFile encoding: 'UTF-8', file: 'VSIX_Packaging/ReleaseNotes.txt'
    releaseNotes = releaseNotes.replaceAll('0\\.5', version).replaceAll('\\$date', new Date().format("dd 'of' MMM yyyy HH:mm"));
    println(releaseNotes)
    writeFile encoding: 'UTF-8', file: 'VSIX_Packaging/ReleaseNotes.txt', text: releaseNotes
    
    def nugetPackage = readFile encoding: 'UTF-8', file: 'SensorbergSDK/nuget/SensorbergSDK.nuspec'
    nugetPackage = nugetPackage.replaceAll('0\\.6\\.7-alpha', version)
    println(nugetPackage)
    writeFile encoding: 'UTF-8', file: 'SensorbergSDK/nuget/SensorbergSDK.nuspec', text: nugetPackage

    stage 'package vsix'
    bat "\"${msbuild}\" VSIX_Packaging.sln"
    
    stage 'package nuget'
    bat "createnuget.bat"
    
    stage 'install vsix package'
    bat "\"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\IDE\\VSIXInstaller.exe\" /q /a VSIX_Packaging\\bin\\Release\\SensorbergSDK.vsix"
    
    archive 'VSIX_Packaging/bin/Release/*.vsix'
    archive 'SensorbergSDK*.nupkg'
    
    emailext body: '$DEFAULT_CONTENT', subject: '$DEFAULT_SUBJECT', to: '$DEFAULT_RECIPIENTS'
}
catch(e) {
    node {
        emailext body: '$DEFAULT_CONTENT', subject: 'fail $DEFAULT_SUBJECT', to: '$DEFAULT_RECIPIENTS'
    }
    throw e
}

}