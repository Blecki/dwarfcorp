using System;
using System.IO;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using SharpRaven.Data;

//Todo: Why can't I use SharpRaven on FNA?

namespace DwarfCorpCore
{
    
}

namespace DwarfCorp
{
#if WINDOWS || XBOX
    internal static class Program
    {
        public static string Version = "20.12.10_FNA";
        public static string[] CompatibleVersions = { "20.12.10_XNA", "20.12.10_FNA" };
        public static string Commit = "UNKNOWN";
        public static char DirChar = Path.DirectorySeparatorChar;
        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            try
            {
                var cwd = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
                Directory.SetCurrentDirectory(new Uri(cwd).LocalPath);
                using (Stream stream = new FileStream("version.txt", FileMode.Open))
                using (StreamReader reader = new StreamReader(stream))
                    Commit = reader.ReadToEnd();
                Commit = Commit.Trim();
            }
            catch (Exception) { }
            System.Net.ServicePointManager.ServerCertificateValidationCallback = SSLCallback;
#if CREATE_CRASH_LOGS
            try
#endif
#if !DEBUG
            try
#endif
            {

                Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
                //fbDeprofiler.DeProfiler.Load();
                using (DwarfGame game = new DwarfGame())
                {
                    game.Run();
                }

                SignalShutdown();
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                WriteExceptionLog(exception);
            }
#endif
#if !DEBUG
            catch (Exception exception)
            {
                SDL2.SDL.SDL_ShowSimpleMessageBox(SDL2.SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "Unhandled Exception!", String.Format("An unhandled exception occurred in DwarfCorp. This has been reported to Completely Fair Games LLC.\n {0}", exception.ToString()), IntPtr.Zero);
                WriteExceptionLog(exception);
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
            if (os != null)
            {
                file.WriteLine("OS Version: " + os.Version);
                file.WriteLine("OS Platform: " + os.Platform);
                file.WriteLine("OS SP: " + os.ServicePack);
                file.WriteLine("OS Version String: " + os.VersionString);
            }
            
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
            throw exception;
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

        // This is a very dangerous hack which forces DwarfCorp to accept all SSL certificates. This is to enable crash reporting on mac/linux.
        public static bool SSLCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public static void CaptureException(Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
#if DEBUG
            throw exception;
#endif
        }

        public static void CaptureSentryMessage(String Message)
        {
            Console.Error.WriteLine(Message);
        }


        public static void LogSentryBreadcrumb(string category, string message, BreadcrumbLevel level = BreadcrumbLevel.Info)
        {
            Console.Out.WriteLine(String.Format("{0} : {1}", category, message));
        }

        public static bool ShowErrorDialog(String Message)
        {
            return true;
        }
    }
#endif
        }