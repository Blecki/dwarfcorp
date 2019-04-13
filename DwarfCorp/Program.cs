using System;
using System.IO;
using System.Threading;
using ContentGenerator;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorpCore
{
    
}

#if XNA_BUILD || GEMMONO

namespace DwarfCorp
{

#if WINDOWS || XBOX
    internal static class Program
    {
        public static string Version = "19.07.XX_XNA_DEV";
        public static string[] CompatibleVersions = { "19.07.XX_XNA_DEV" };
        public static string Commit = "UNKNOWN";
        public static char DirChar = Path.DirectorySeparatorChar;
        
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


#if !DEBUG
            try
#endif
            {
#if XNA_BUILD

                fbDeprofiler.DeProfiler.Load();
#endif
                using (DwarfGame game = new DwarfGame())
                {
                    game.Run();
                }

                while (AssetManagement.Steam.Steam.HasTransaction(a => true))
                    AssetManagement.Steam.Steam.Update();

                SignalShutdown();
            }
#if !DEBUG
            catch (Exception exception)
            {
                WriteExceptionLog(exception);
                string report = "This was automatically reported to the devs to help us debug!";
                if (!GameSettings.Default.AllowReporting)
                {
                    report = "You have opted out of automatic crash reporting.";
                }
                System.Windows.Forms.MessageBox.Show(String.Format("An unhandled exception occurred in DwarfCorp. {1} \n {0}", exception.ToString(), report), "ERROR");
            }
#endif
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

            for(int i = 0; i < args.Length; i++)
            {
                toReturn += args[i];

                if(i < args.Length - 1)
                {
                    toReturn += DirChar;
                }
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
#endif
        }
#endif