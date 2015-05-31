using System;
using System.IO;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorpCore
{
    
}

namespace DwarfCorp
{
    internal static class Program
    {
        public static string Version = "Monogame Alpha 1 . 1 . 3";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
#if CREATE_CRASH_LOGS
            try
#endif
            {
                using (DwarfGame game = new DwarfGame())
                {
                    game.Run();
                }

                Program.SignalShutdown();
            }
#if CREATE_CRASH_LOGS
            catch (Exception exception)
            {
                ProgramData.WriteExceptionLog(exception);
            }
#endif

        }

        public static ManualResetEvent ShutdownEvent = new ManualResetEvent(false);

        public static void SignalShutdown()
        {
            DwarfGame.ExitGame = true;
            ShutdownEvent.Set();
        }
    }
}