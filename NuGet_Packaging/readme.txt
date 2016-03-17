
To view & modify the nuget package use : Nuget Package Explorer: https://npe.codeplex.com/

To test local nuger packages see this: https://blogs.endjin.com/2014/07/how-to-test-nuget-packages-locally/

Do remember that with latest tools & stuff, all nuget packages are getting cached, thus when testing increase the version number or 
do remember to clean out the files under C:\Users\<USERNAME>\.nuget\packages 

Issues with current package
I'm unable to build any reference libraries with neutral targets (i.e. AllCPU), thus the nuget package does only work with the x86.

In general the reference is only used for class viewing and that the using statements find stuff right, the actual implementation
is selected runtime from the files inclluded under the runtimes folder.

