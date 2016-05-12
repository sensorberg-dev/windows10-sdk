#!groovy
def version = "0.7.0."+env.BUILD_NUMBER;

node ('Windows') {

try {
    stage 'nuget restore'
    bat '"C:\\Program Files (x86)\\NuGet\\Visual Studio 2015\\nuget.exe" restore SensorbergAll.sln'

	def msbuild = tool 'Main';
	stage 'build' {
		parallel 'build arm' : {
			bat "\"${msbuild}\" /t:Clean,Build /p:Platform=ARM SensorbergSDKTests.sln"
		}, 'build x64' : {
			bat "\"${msbuild}\" /t:Clean,Build /p:Platform=x64 SensorbergSDKTests.sln"
		}, 'build x86' : {
			bat "\"${msbuild}\" /t:Clean,Build /p:Platform=x86 SensorbergSDKTests.sln"
		}
	}
    stage 'assemble appx'
    bat "\"C:\\Program Files (x86)\\Windows Kits\\10\\bin\\x64\\MakeAppx.exe\"  pack /l /h sha256 /f SensorbergSDKTests\\obj\\x86\\Debug\\package.map.txt /o /p TestProject.appx"

    bat "\"C:\\Program Files (x86)\\Windows Kits\\10\\bin\\x64\\signtool.exe\" sign /fd sha256 /f SensorbergSDKTests\\SensorbergSDKTests_TemporaryKey.pfx TestProject.appx"

    stage name: 'Test', concurrency: 1

    def vstest = tool 'VSTest'
    try {
        bat "\"${vstest}\" /Settings:SensorbergSDKTests\\.runsettings TestProject.appx"
    }
    catch(e) {
        mail subject: "${env.JOB_NAME} Test failed with ${e.message}", to: 'eagleeye@byte-welt.net', body: "${env.JOB_NAME} - ${env.BRANCH_NAME}\nTest Failed: ${e}"
        currentBuild.result = 'UNSTABLE'
    }

    stage name: 'Analyse', concurrency: 1

    def sonarQubeRunner = tool 'MSBuildSonarQubeRunner'
    bat "\"${sonarQubeRunner}\\MSBuild.SonarQube.Runner.exe\" begin /k:\"com.sensorberg:win10sdk\" /n:\"Windows10-SDK\" /v:\"${version}\" /d:sonar.resharper.solutionFile=SensorbergSDKTests.sln /d:sonar.resharper.cs.reportPath=ReSharperResult.xml /d:sonar.branch=${env.BRANCH_NAME}"
    bat "\"${msbuild}\" /p:Platform=ARM SensorbergSDKTests.sln"
    bat "\"C:\\Program Files (x86)\\JetBrains\\jb-commandline\\inspectcode.exe\" SensorbergSDKTests.sln /o=ReSharperResult.xml"
    bat "\"${sonarQubeRunner}\\MSBuild.SonarQube.Runner.exe\" end"

    emailext body: '$DEFAULT_CONTENT', subject: '$DEFAULT_SUBJECT', to: '$DEFAULT_RECIPIENTS'
}
catch(e) {
    node {
        emailext body: '$DEFAULT_CONTENT', subject: 'fail $DEFAULT_SUBJECT', to: '$DEFAULT_RECIPIENTS'
    }
    throw e
}

}
