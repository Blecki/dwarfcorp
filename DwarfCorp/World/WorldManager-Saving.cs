using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using BloomPostprocess;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using Newtonsoft.Json;
using DwarfCorp.Events;

namespace DwarfCorp
{
    // Todo: Split into WorldManager and WorldRenderer.
    /// <summary>
    /// This is the main game state for actually playing the game.
    /// </summary>
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
                DirectoryInfo worldDirectory =
                    Directory.CreateDirectory(DwarfGame.GetWorldDirectory() +
                                              Path.DirectorySeparatorChar + Settings.Overworld.Name);

            NewOverworldFile file = new NewOverworldFile(Game.GraphicsDevice, Settings);
                file.WriteFile(worldDirectory.FullName);

                gameFile = SaveGame.CreateFromWorld(this);
            var path = worldDirectory.FullName + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)Settings.InstanceSettings.Origin.X, (int)Settings.InstanceSettings.Origin.Y);
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
