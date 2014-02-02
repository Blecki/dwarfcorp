using System;
using System.Threading;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

#if WINDOWS || XBOX
    internal static class Program
    {
        public static string Version = "1 . 0 . 31";
        public static char DirChar = System.IO.Path.DirectorySeparatorChar;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            using(DwarfGame game = new DwarfGame())
            {
                game.Run();
            }

            SignalShutdown();
            
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

        private static void SignalShutdown()
        {
            DwarfGame.ExitGame = true;
            ShutdownEvent.Set();
        }

    }
#endif
}