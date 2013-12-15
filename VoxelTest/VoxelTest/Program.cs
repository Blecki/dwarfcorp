using System;
using System.Threading;

namespace DwarfCorp
{

#if WINDOWS || XBOX
    internal static class Program
    {
        public static string Version = "1 . 0 . 29";
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

        public static ManualResetEvent ShutdownEvent = new ManualResetEvent(false);

        private static void SignalShutdown()
        {
            DwarfGame.ExitGame = true;
            ShutdownEvent.Set();
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
#endif
}