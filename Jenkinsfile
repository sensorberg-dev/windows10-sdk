#!groovy
def version = "1." + (env.BRANCH_NAME == "master" ? ("1." + env.BUILD_NUMBER) : ("0." + env.BUILD_NUMBER + "-beta"));
def versionVSIX = "1.1." + env.BUILD_NUMBER;


node ('Windows') {

try {
	stage 'checkout'
    checkout scm//: [$class: 'GitSCM', branches: [[name: '*']], userRemoteConfigs: [[url: 'https://github.com/sensorberg-dev/windows10-sdk.git']], clean: true]

	print env.JOB_NAME
	if(env.JOB_NAME.indexOf('Package') != -1) {
		load 'package.groovy'
	}
	else {
		stage 'nuget restore'
		bat '"C:\\Program Files (x86)\\NuGet\\Visual Studio 2015\\nuget.exe" restore SensorbergAll.sln'

		def msbuild = tool 'Main';
		
		stage 'build arm' 
		bat "\"${msbuild}\" /t:Clean,Build /p:Platform=ARM SensorbergSDKTests.sln"
		
		stage 'build x64'
		bat "\"${msbuild}\" /t:Clean,Build /p:Platform=x64 SensorbergSDKTests.sln"
		
		stage 'build x86'
		bat "\"${msbuild}\" /t:Clean,Build /p:Platform=x86 SensorbergSDKTests.sln"
		
		stage 'assemble appx'
		bat "\"C:\\Program Files (x86)\\Windows Kits\\10\\bin\\x64\\MakeAppx.exe\"  pack /l /h sha256 /f SensorbergSDKTests\\obj\\x86\\Debug\\package.map.txt /o /p TestProject.appx"

		bat "\"C:\\Program Files (x86)\\Windows Kits\\10\\bin\\x64\\signtool.exe\" sign /fd sha256 /f SensorbergSDKTests\\SensorbergSDKTests_TemporaryKey.pfx TestProject.appx"

		stage name: 'Test', concurrency: 1

		def vstest = tool 'VSTest'
		try {
			bat "\"${vstest}\" /Settings:SensorbergSDKTests\\.runsettings TestProject.appx"
		}
		catch(e) {
			def sub = env.JOB_NAME+' - Build '+env.BUILD_NUMBER+' - FAILED'
			emailext body: "${env.JOB_NAME} Test failed with ${e.message}", subject: sub , to: '$DEFAULT_RECIPIENTS'
			currentBuild.result = 'UNSTABLE'
		}

		stage name: 'Analyse', concurrency: 1
		def sonarQubeRunner = tool 'MSBuildSonarQubeRunner'
		bat "\"${sonarQubeRunner}\\MSBuild.SonarQube.Runner.exe\" begin /k:\"com.sensorberg:win10sdk\" /n:\"Windows10-SDK\" /v:\"${version}\" /d:sonar.resharper.solutionFile=SensorbergSDKTests.sln /d:sonar.resharper.cs.reportPath=ReSharperResult.xml /d:sonar.branch=${env.BRANCH_NAME}"
		bat "\"${msbuild}\" /t:Clean,Build /p:Platform=ARM SensorbergSDKTests.sln"
		bat "\"C:\\Program Files (x86)\\JetBrains\\jb-commandline\\inspectcode.exe\" SensorbergSDKTests.sln /o=ReSharperResult.xml"
		bat "for /D %%f in (.sonarqube\\out\\SensorbergSDK_x*) do rmdir %%f /s /q"
		bat "\"${sonarQubeRunner}\\MSBuild.SonarQube.Runner.exe\" end"
		
		stage 'build simple app'
		bat "\"${msbuild}\" /t:Clean,Build /p:Platform=x86 SensorbergSimpleApp.sln"
		
	}
	
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

}
