#tool "nuget:?package=Wyam&version=2.2.9"
#addin "nuget:?package=Cake.Wyam&version=2.2.9"
#addin "nuget:?package=Cake.Npm&version=0.17.0"
#addin "nuget:?package=NetlifySharp&version=0.1.0"

using NetlifySharp;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var outputPath = MakeAbsolute(Directory("./output"));


//////////////////////////////////////////////////////////////////////
// SETUP
//////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    // Executed BEFORE the first task.
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
});



Task("Build")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            Recipe = "Blog",
            Theme = "CleanBlog",
            UpdatePackages = true,
        });
            
        Zip("./output", "output.zip", "./output/**/*");
    });

Task("Preview")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            Recipe = "Blog",
            Theme = "CleanBlog",
            UpdatePackages = false,
            Preview = true,
            Watch = true
        });
    });

// Assumes Wyam source is local and at ../../WyamIO/Wyam
Task("Debug")
    .Does(() =>
    {
        var wyamFolder = MakeAbsolute(Directory("../../wyamio/Wyam")).ToString();
        var wyamExecutable = wyamFolder + "/src/clients/Wyam/bin/Debug/netcoreapp2.1/Wyam.dll";
        var wyamIntegrationFolder = wyamFolder + "/tests/integration/Wyam.Examples.Tests";
        var wyamIntegrationBinFolder = wyamIntegrationFolder + "/bin/Debug/netcoreapp2.1";
        var wyamProject = wyamIntegrationFolder + "/Wyam.Examples.Tests.csproj";


        Information($"Building project {wyamProject}");
        DotNetCoreBuild(wyamProject);        
        Information($"Running WYAM at {wyamExecutable}");
        DotNetCoreExecute(wyamExecutable,
            $"-a \"{wyamIntegrationBinFolder}/**/*.dll\" -r \"Blog -i\" -t \"{wyamFolder}/themes/Docs/CleanBlog\" -p");
    });

Task("Netlify")
    .Does(() =>
    {
        var netlifyToken = EnvironmentVariable("NETLIFY_TOKEN");
        if(string.IsNullOrEmpty(netlifyToken))
        {
            throw new Exception("Could not get Netlify token environment variable");
        }

        // Install the Netlify CLI locally and then run the deploy command
        Information("Deploying output to Netlify");
        var client = new NetlifyClient(netlifyToken);
        client.UpdateSite($"glennawatson.netlify.com", MakeAbsolute(Directory("./output")).FullPath).SendAsync().Wait();
    });
    
//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Preview");
    
Task("BuildServer")
    .IsDependentOn("Build");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

if (!StringComparer.OrdinalIgnoreCase.Equals(target, "Deploy"))
{
    RunTarget(target);
}
