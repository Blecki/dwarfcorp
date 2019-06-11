using DwarfCorp.GameStates;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Point = Microsoft.Xna.Framework.Point;

namespace DwarfCorp
{
    public partial class WorldManager : IDisposable
    {
        public delegate void SaveCallback(bool success, Exception e);

        public void Save(WorldManager.SaveCallback callback = null)
        {
            Paused = true;
            var waitforsave = new WaitState(Game, "Saving...", () => SaveThreadRoutine());
            if (callback != null)
                waitforsave.OnFinished += (bool b, WaitStateException e) => callback(b, e);
            GameStateManager.PushState(waitforsave);
        }

        private bool SaveThreadRoutine()
        {
#if !DEBUG
            try
            {
#endif
            Thread.CurrentThread.Name = "Save";
            // Ensure we're using the invariant culture.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            var worldDirectory = Directory.CreateDirectory(DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + Overworld.Name);

            var file = new NewOverworldFile(Game.GraphicsDevice, Overworld);
            file.WriteFile(worldDirectory.FullName);

            var gameFile = SaveGame.CreateFromWorld(this);
            var path = worldDirectory.FullName + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)Overworld.InstanceSettings.Origin.X, (int)Overworld.InstanceSettings.Origin.Y);
            SaveGame.DeleteOldestSave(path, GameSettings.Default.MaxSaves, "Autosave");
            gameFile.WriteFile(path);
            ComponentManager.CleanupSaveData();

            lock (Renderer.ScreenshotLock)
            {
                Renderer.Screenshots.Add(new WorldRenderer.Screenshot()
                {
                    FileName = path + Path.DirectorySeparatorChar + "screenshot.png",
                    Resolution = new Point(128, 128)
                });
            }

#if !DEBUG
            }
            catch (Exception exception)
            {
                Console.Error.Write(exception.ToString());
                Game.CaptureException(exception);
                throw new WaitStateException(exception.Message);
            }
#endif
            return true;
        }
    }
}
