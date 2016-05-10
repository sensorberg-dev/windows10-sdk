
To view & modify the nuget package use : Nuget Package Explorer: https://npe.codeplex.com/

To test local nuger packages see this: https://blogs.endjin.com/2014/07/how-to-test-nuget-packages-locally/

Do remember that with latest tools & stuff, all nuget packages are getting cached, thus when testing increase the version number or 
do remember to clean out the files under C:\Users\<USERNAME>\.nuget\packages 

Do remember to build batch build all 4 targets (AllCpu, ARM, X86 X64), the AllCpu is used as reference library, and other are used runtime execution.

