using System;
using System.IO;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

#if WINDOWS || XBOX
    internal static class ProgramData
    {
        public static void WriteExceptionLog(Exception exception)
        {
            Program.SignalShutdown();
            Console.Error.WriteLine("DwarfCorp Version " + Program.Version);
            OperatingSystem os = Environment.OSVersion;
            Console.Error.WriteLine("OS Version: " + os.Version);
            Console.Error.WriteLine("OS Platform: " + os.Platform);
            Console.Error.WriteLine("OS SP: " + os.ServicePack);
            Console.Error.WriteLine("OS Version String: " + os.VersionString);
            
            if (GameState.Game != null && GameState.Game.GraphicsDevice != null)
            {
                GraphicsAdapter adapter = GameState.Game.GraphicsDevice.Adapter;
                //file.WriteLine("Graphics Card: " + adapter.DeviceName + "->" + adapter.Description);
                Console.Error.WriteLine("Display Mode: " + adapter.CurrentDisplayMode.Width + "x" + adapter.CurrentDisplayMode.Height + " (" + adapter.CurrentDisplayMode.AspectRatio + ")");
                Console.Error.WriteLine("Supported display modes: ");

                foreach (var mode in adapter.SupportedDisplayModes)
                {
                    Console.Error.WriteLine(mode.Width + "x" + mode.Height + " (" + mode.AspectRatio + ")");
                }
            }

            Console.Error.WriteLine(exception.ToString());
            throw exception;
        }

        public static string CreatePath(params string[] args)
        {
            return String.Join(new String(Path.DirectorySeparatorChar, 1), args);
        }
    }

#endif
}