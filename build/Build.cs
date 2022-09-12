using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release;

    [Solution] readonly Solution Solution;
    //[GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath SourceDirectory => RootDirectory / "Projects";

    Project MainProject => Solution.GetProject("ICHOMFhirQuestionaireClient");
    Project WixSharpProject => Solution.GetProject("ICHOMFhirQuestionaireClient.WSInstaller");

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            //System.Diagnostics.Debugger.Launch();
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
            if (MainProject == null)
                throw new System.Exception($"MainProjectnot found");
            if (WixSharpProject == null)
                throw new System.Exception($"WixSharpProject not found");
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target CompileMainProject => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetPublish(s => s
                .SetConfiguration(Configuration.Release)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.AssemblySemFileVer)
                .SetDescription(GitVersion.InformationalVersion)
                .SetRuntime("win-x64")
                .SetSelfContained(true)
                .SetProject(MainProject)
                .SetOutput(OutputDirectory / MainProject.Name)
            );
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .DependsOn(CompileMainProject)
        ;

    Target Installer => _ => _
        .DependsOn(Clean)
        .DependsOn(Compile)
        .Executes(() =>
        {
            /*
             * We set the environment variable
             * WixOut.
             * This optional environment variables te4lls wix where to find the base output dirs
             */
            DotNetBuild(s => s
                .SetProcessEnvironmentVariable("WixOut", OutputDirectory)
                .SetConfiguration(Configuration.Release)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.AssemblySemFileVer)
                .SetDescription(GitVersion.InformationalVersion)
                .SetProjectFile(WixSharpProject)
            );

            System.IO.Directory.Delete(OutputDirectory / MainProject.Directory.Name, true);
        });

    Target Publish => _ => _
        .DependsOn(Clean)
        .DependsOn(Compile)
        .DependsOn(Installer)
        ;
}
