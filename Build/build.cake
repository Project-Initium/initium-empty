#addin "nuget:?package=Cake.Npm&version=0.17.0"
#addin "nuget:?package=Cake.Coverlet&version=2.4.2"
#tool "nuget:?package=GitVersion.CommandLine&version=5.1.3"

var target = Argument<string>("target", "Default");
var buildPath = Directory("./build-artifacts");
var publishPath = buildPath + Directory("publish");
var releasePath = buildPath + Directory("release");
var coverPath = buildPath + Directory("cover");


Task("__Clean")
    .Does(() => {
        if (BuildSystem.IsLocalBuild) {
            CleanDirectories(new DirectoryPath[] {
                buildPath
            });

            CleanDirectories("../**/bin");
            CleanDirectories("../**/obj");
        }  

        CreateDirectory(releasePath);
        CreateDirectory(publishPath);
        CreateDirectory(coverPath);   
    });

Task("__Versioning")
    .Does(() => {
        var version = GitVersion(new GitVersionSettings {
            UpdateAssemblyInfo = true
        });
    
        TFBuild.Commands.UpdateBuildNumber(version.SemVer);
    });

Task("__Build")
    .Does(() => {
        var settings = new DotNetCoreBuildSettings {
            Configuration = "Release"
        };
        DotNetCoreBuild("../Initium.sln", settings);
    });
Task("__Test")
    .Does(() => {
        var testResults = MakeAbsolute(coverPath + File("xunit-report.xml")).FullPath;
        var testSettings = new DotNetCoreTestSettings {
            Configuration = "Release",
            NoBuild = true,
            Logger = $"trx;LogFileName={testResults};verbosity=normal"
    };

    var coverletSettings = new CoverletSettings {
        CollectCoverage = true,
        CoverletOutputFormat = CoverletOutputFormat.opencover|(CoverletOutputFormat)12,
        CoverletOutputDirectory = coverPath,
        CoverletOutputName = "coverage"        
    };

    DotNetCoreTest("../Initium.sln", testSettings, coverletSettings);
    });

Task("Build")
    .IsDependentOn("__Clean")
    .IsDependentOn("__Versioning")    
    .IsDependentOn("__Build")
    .IsDependentOn("__Test");

Task("Default")
    .IsDependentOn("Build");

RunTarget(target);