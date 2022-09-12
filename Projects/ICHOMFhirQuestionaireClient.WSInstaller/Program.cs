using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WixSharp;
using WixSharp.Forms;

namespace ICHOMFhirQuestionaireClient.WSInstaller
{

    public class Program
    {
        public static String Manufacturer = "HL7";
        public static readonly String ProductName = "ICHOM Fhir Questionaire Client";

        public static readonly String ApplicationName = "ICHOMFhirQuestionaireClient";
        public static readonly String ApplicationExeName = ApplicationName + ".exe";

        public static readonly Guid ProductGuid = new Guid("9EAFD1BA-FAE1-4E9C-9774-1CD195074B06");
        public static readonly String NetVer = "net6.0-windows";

#if DEBUG
        /// <summary>
        /// Configuration macro => debug
        /// </summary>
        public const String Configuration = "Debug";
#elif RELEASE
        /// <summary>
        /// Configuration macro => release
        /// </summary>
        public const String Configuration = "Release";
#else
        // should never get here...
        Error!!
#endif


        static String GetVersion(String buildDir)
        {
            String mainExePath = Path.Combine(buildDir, ApplicationExeName);
            if (System.IO.File.Exists(mainExePath) == false)
                throw new Exception($"Path '{mainExePath}' does not exist");
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(mainExePath);
            return info.FileVersion;
        }


        static String FindParentDir(String dirName)
        {
            String servicePath = Path.GetFullPath(".");
            while (true)
            {
                servicePath = Path.GetFullPath(servicePath);
                String serviceDir = Path.Combine(servicePath, dirName);
                if (Directory.Exists(serviceDir))
                    return serviceDir;
                String newPath = Path.Combine(servicePath, "..");
                newPath = Path.GetFullPath(newPath);
                if (String.Compare(newPath, servicePath, StringComparison.InvariantCulture) == 0)
                    throw new Exception($"Parent directory {dirName} not found");
                servicePath = newPath;
            }
        }

        static void Main()
        {
            String buildDir;

            // If environment variable WixOut set, then we are being called
            // from nuke and we need to use WixOut as the base project directory.
            // otherwise, use normal project output

            String outDir = Environment.GetEnvironmentVariable("WixOut");
            if (String.IsNullOrEmpty(outDir))
            {
                String baseDir = FindParentDir("Projects");
                buildDir = Path.Combine(
                    baseDir,
                    ApplicationName,
                    "bin",
                    Configuration,
                    NetVer);
            }
            else
            {
                buildDir = Path.Combine(
                    outDir,
                    ApplicationName);
            }

            if (Directory.Exists(buildDir) == false)
                throw new Exception($"Path '{buildDir}' does not exist");

            String version = GetVersion(buildDir);
            Console.WriteLine($"File version = '{version}'");

            ManagedProject project = new ManagedProject(ProductName,
                new Dir(new Id("BASEDIR"),
                    Path.Combine("%ProgramFiles%", Manufacturer),
                     new Dir(new Id("INSTALLDIR"),
                        Path.Combine(ProductName, version),
                        // Install program files
                        new Files(Path.Combine(buildDir, "*.*")),
                        // Add uninstall shortcut to program files folder
                        new ExeFileShortcut("Uninstall ICHOM Fhir Questionaire Client", "[System64Folder]msiexec.exe", "/x [ProductCode]")
                    ),
                    // install start menu items
                    new Dir(new Id("MENUDIR"),
                        Path.Combine("%ProgramMenu%", Manufacturer, ProductName),
                        new ExeFileShortcut("Uninstall ICHOM Fhir Questionaire Client", "[System64Folder]msiexec.exe", "/x [ProductCode]")
                    )
                )
            );
            project.MajorUpgrade = new MajorUpgrade
            {
                AllowSameVersionUpgrades = true,
                DowngradeErrorMessage = "Newer version already installed"
            };

            project.OutFileName = $"{ProductName}.{Configuration}.{version}";
            project.GUID = ProductGuid;
            project.Version = new Version(version);
            project.ManagedUI = ManagedUI.Empty;    //no standard UI dialogs
            project.ManagedUI = ManagedUI.Default;  //all standard UI dialogs

            //custom set of standard UI dialogs
            project.ManagedUI = new ManagedUI();

            project.ManagedUI
                .InstallDialogs
                    .Add(Dialogs.Welcome)
                    .Add(Dialogs.Licence)
                    //.Add(Dialogs.SetupType)
                    //.Add(Dialogs.Features)
                    //.Add(Dialogs.InstallDir)
                    .Add(Dialogs.Progress)
                    .Add(Dialogs.Exit);

            project.ManagedUI.ModifyDialogs.Add(
                Dialogs.MaintenanceType)
                //.Add(Dialogs.Features)
                .Add(Dialogs.Progress)
                .Add(Dialogs.Exit);

            project.Load += Msi_Load;
            project.BeforeInstall += Msi_BeforeInstall;
            project.AfterInstall += Msi_AfterInstall;
            //project.SourceBaseDir = "<input dir path>";

            if (String.IsNullOrEmpty(outDir) == false)
                project.OutDir = outDir;

            project.BuildMsi();
        }

        static void Msi_Load(SetupEventArgs e)
        {
            //if (!e.IsUISupressed && !e.IsUninstalling)
            //    MessageBox.Show(e.ToString(), "Load");
        }

        static void Msi_BeforeInstall(SetupEventArgs e)
        {
            //if (!e.IsUISupressed && !e.IsUninstalling)
            //    MessageBox.Show(e.ToString(), "BeforeInstall");
        }

        static void Msi_AfterInstall(SetupEventArgs e)
        {
            if (!e.IsUninstalling)
            {
                try
                {
                    e.Session.Log($"Executing Msi_AfterInstall");
                    e.Session.Log($"Msi_AfterInstall complete");
                } 
                catch(Exception err)
                {
                    e.Session.Log($"Exception caught patching Options.json. Install failed. {err.Message}");
                    e.Result = ActionResult.Failure;
                    MessageBox.Show($"Install post processing failed with error. See log for details.");
                }
            }
        }
    }
}
