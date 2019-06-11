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
        private string[] prevAmbience = { null, null };
        private Timer AmbienceTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
        private bool firstAmbience = true;
        private void PlaySpecialAmbient(string sound)
        {
            if (prevAmbience[0] != sound)
            {
                SoundManager.PlayAmbience(sound);

                if (!string.IsNullOrEmpty(prevAmbience[0]) && prevAmbience[0] != sound)
                    SoundManager.StopAmbience(prevAmbience[0]);
                if (!string.IsNullOrEmpty(prevAmbience[1]) && prevAmbience[1] != sound)
                    SoundManager.StopAmbience(prevAmbience[1]);

                prevAmbience[0] = sound;
                prevAmbience[1] = sound;
            }
        }

        public void HandleAmbientSound()
        {
            AmbienceTimer.Update(DwarfTime.LastTime);
            if (!AmbienceTimer.HasTriggered && !firstAmbience)
            {
                return;
            }
            firstAmbience = false;

            // Before doing anything, determine if there is a rain or snow storm.
            if (Weather.IsRaining())
            {
                PlaySpecialAmbient("sfx_amb_rain_storm");
                return;
            }

            if (Weather.IsSnowing())
            {
                PlaySpecialAmbient("sfx_amb_snow_storm");
                return;
            }

            // First check voxels to see if we're underground or underwater.
            var vox = VoxelHelpers.FindFirstVisibleVoxelOnScreenRay(ChunkManager, GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2, Renderer.Camera, GraphicsDevice.Viewport, 100.0f, false, null);

            if (vox.IsValid)
            {
                float height = WaterRenderer.GetTotalWaterHeightCells(ChunkManager, vox);
                if (height > 0)
                {
                    PlaySpecialAmbient("sfx_amb_ocean");
                    return;
                }
                else
                {
                    // Unexplored voxels assumed to be cave.
                    if (vox.IsValid && !vox.IsExplored)
                    {
                        PlaySpecialAmbient("sfx_amb_cave");
                        return;
                    }

                    var above = VoxelHelpers.GetVoxelAbove(vox);
                    // Underground, do the cave test.
                    if (above.IsValid && above.IsEmpty && above.Sunlight == false)
                    {
                        PlaySpecialAmbient("sfx_amb_cave");
                        return;
                    }

                }

            }
            else
            {
                return;
            }

            // Now check for biome ambience.
            var pos = vox.WorldPosition;
            var biome = Overworld.Map.GetBiomeAt(pos, Overworld.InstanceSettings.Origin);

            if (biome != null && !string.IsNullOrEmpty(biome.DayAmbience))
            {
                if (prevAmbience[0] != biome.DayAmbience)
                {
                    if (!string.IsNullOrEmpty(prevAmbience[0]))
                    {
                        SoundManager.StopAmbience(prevAmbience[0]);
                        prevAmbience[0] = null;
                    }
                    if (!string.IsNullOrEmpty(prevAmbience[1]))
                    {
                        SoundManager.StopAmbience(prevAmbience[1]);
                        prevAmbience[1] = null;
                    }
                    SoundManager.PlayAmbience(biome.DayAmbience);
                }

                prevAmbience[0] = biome.DayAmbience;
            }

            if (!string.IsNullOrEmpty(biome.NightAmbience) && prevAmbience[1] != biome.NightAmbience)
            {
                prevAmbience[1] = biome.NightAmbience;

                SoundManager.PlayAmbience(biome.NightAmbience);
            }
        }
    }
}
