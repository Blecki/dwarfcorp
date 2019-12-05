using System;
using System.IO;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpRaven;
using SharpRaven.Data;

namespace DwarfCorp
{
    internal static class Program
    {
        public static string Version = "19.08.31_XNA";
        public static string[] CompatibleVersions = { "19.08.31_XNA", "19.08.31_FNA" };
        public static string Commit = "UNKNOWN";
        public static char DirChar = Path.DirectorySeparatorChar;
        private static RavenClient ravenClient;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            try
            {
                var cwd = global::System.IO.Path.GetDirectoryName(global::System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                Directory.SetCurrentDirectory(new Uri(cwd).LocalPath);
                using (Stream stream = global::System.Reflection.Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("DwarfCorp.version.txt"))
                using (StreamReader reader = new StreamReader(stream))
                    Commit = reader.ReadToEnd();

                Commit = Commit.Trim();
            }
            catch (Exception) { }

            Thread.CurrentThread.CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = global::System.Globalization.CultureInfo.InvariantCulture;

            GameSettings.Load();


#if !DEBUG
            try
            {
                if (GameSettings.Default.AllowReporting)
                {
                    ravenClient = new RavenClient("https://af78a676a448474dacee4c72a9197dd2:0dd0a01a9d4e4fa4abc6e89ac7538346@sentry.io/192119");
                    ravenClient.Tags["Version"] = Program.Version;
                    ravenClient.Tags["Commit"] = Program.Commit;
                    ravenClient.Tags["Platform"] = "XNA";
                    ravenClient.Tags["OS"] = "Windows";
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.ToString());
            }
#endif

#if !DEBUG
            try
#endif
            {
                fbDeprofiler.DeProfiler.Load();

                using (DwarfGame game = new DwarfGame())
                {
                    game.Run();
                }

                while (AssetManagement.Steam.Steam.HasTransaction(a => true))
                    AssetManagement.Steam.Steam.Update();

                SignalShutdown();
            }
#if !DEBUG
            catch (HandledException exception)
            {
                WriteExceptionLog(exception.InnerException);
            }
            catch (Exception exception)
            {
                CaptureException(exception);
                WriteExceptionLog(exception);
                var report = GameSettings.Default.AllowReporting ? "This was automatically reported to the devs to help us debug!" : "You have opted out of automatic crash reporting.";
                System.Windows.Forms.MessageBox.Show(String.Format("An unhandled exception occurred in DwarfCorp. {1} \n {0}", exception.ToString(), report), "ERROR");
            }
#endif
            }

        public static void CaptureException(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            if (ravenClient != null)
                ravenClient.Capture(new SentryEvent(exception));
        }

        public static void LogSentryBreadcrumb(string category, string message, BreadcrumbLevel level = BreadcrumbLevel.Info)
        {
            Console.Out.WriteLine(String.Format("{0} : {1}", category, message));
            if (ravenClient != null)
                ravenClient.AddTrail(new Breadcrumb(category) { Message = message, Type = BreadcrumbType.Navigation });
        }

        public static bool ShowErrorDialog(String Message)
        {
            var report = GameSettings.Default.AllowReporting ? "This was automatically reported to the devs to help us debug!" : "You have opted out of automatic crash reporting.";
            return System.Windows.Forms.MessageBox.Show(Message + "\n" + report, "ERROR!", System.Windows.Forms.MessageBoxButtons.RetryCancel) == System.Windows.Forms.DialogResult.Cancel;
        }

        public static void WriteExceptionLog(Exception exception)
        {
            SignalShutdown();
            DirectoryInfo worldDirectory = Directory.CreateDirectory(DwarfGame.GetGameDirectory() + Path.DirectorySeparatorChar + "Logging");
            StreamWriter file =
                new StreamWriter(worldDirectory.FullName + Path.DirectorySeparatorChar + DateTime.Now.ToString("yyyyMMddHHmmssffff") + "_" + "Crashlog.txt", true);
            file.WriteLine("DwarfCorp Version " + Version);
            OperatingSystem os = Environment.OSVersion;
            file.WriteLine("OS Version: " + os.Version);
            file.WriteLine("OS Platform: " + os.Platform);
            file.WriteLine("OS SP: " + os.ServicePack);
            file.WriteLine("OS Version String: " + os.VersionString);

            if (GameState.Game != null && GameState.Game.GraphicsDevice != null)
            {
                GraphicsAdapter adapter = GameState.Game.GraphicsDevice.Adapter;
                file.WriteLine("Graphics Card: " + adapter.DeviceName + "->" + adapter.Description);
                file.WriteLine("Display Mode: " + adapter.CurrentDisplayMode.Width + "x" + adapter.CurrentDisplayMode.Height + " (" + adapter.CurrentDisplayMode.AspectRatio + ")");
                file.WriteLine("Supported display modes: ");

                foreach (var mode in adapter.SupportedDisplayModes)
                {
                    file.WriteLine(mode.Width + "x" + mode.Height + " (" + mode.AspectRatio + ")");
                }
            }

            file.WriteLine(exception.ToString());
            file.Close();
        }

        public static string CreatePath(params string[] args)
        {
            string toReturn = "";

            for (int i = 0; i < args.Length; i++)
            {
                toReturn += args[i];

                if (i < args.Length - 1)
                    toReturn += DirChar;
            }

            return toReturn;
        }

        public static ManualResetEvent ShutdownEvent = new ManualResetEvent(false);

        public static void SignalShutdown()
        {
            DwarfGame.ExitGame = true;
            ShutdownEvent.Set();
        }

    }
}
