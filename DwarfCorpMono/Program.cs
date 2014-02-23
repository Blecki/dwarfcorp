using System;
using System.Threading;

namespace DwarfCorp
{
#if WINDOWS || XBOX
    static class Program
    {
        public static string Version = "MonoGame Winodws 7 OPENGL 1 . 0 . 26";
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (DwarfGame game = new DwarfGame())
            {
                game.Run();
            }

            SignalShutdown();
        }
        public static ManualResetEvent shutdownEvent = new ManualResetEvent(false);

        static void SignalShutdown()
        {
            GeometricPrimitive.ExitGame = true;
            shutdownEvent.Set();
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

