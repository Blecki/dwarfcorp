using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DwarfCorp
{
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
                prevAmbience[0] = sound;
                prevAmbience[1] = sound;
            }
        }

        public void HandleAmbientSound(DwarfTime ElapsedTime)
        {
            AmbienceTimer.Update(ElapsedTime);
            if (!AmbienceTimer.HasTriggered && !firstAmbience)
            {
                return;
            }
            firstAmbience = false;

            // Before doing anything, determine if there is a rain or snow storm.
            if (Weather.IsRaining())
            {
                PlaySpecialAmbient("Audio/oscar/sfx_amb_rain_storm");
                return;
            }

            if (Weather.IsSnowing())
            {
                PlaySpecialAmbient("Audio/oscar/sfx_amb_snow_storm");
                return;
            }

            // First check voxels to see if we're underground or underwater.
            var vox = VoxelHelpers.FindFirstVisibleVoxelOnScreenRay(ChunkManager, GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2, Renderer.Camera, GraphicsDevice.Viewport, 100.0f, false, null);

            if (vox.IsValid)
            {
                float height = WaterRenderer.GetTotalWaterHeightCells(ChunkManager, vox);
                if (height > 0)
                {
                    PlaySpecialAmbient("Audio/oscar/sfx_amb_ocean");
                    return;
                }
                else
                {
                    // Unexplored voxels assumed to be cave.
                    if (vox.IsValid && !vox.IsExplored)
                    {
                        PlaySpecialAmbient("Audio/oscar/sfx_amb_cave");
                        return;
                    }

                    var above = VoxelHelpers.GetVoxelAbove(vox);
                    // Underground, do the cave test.
                    if (above.IsValid && above.IsEmpty && above.Sunlight == false)
                    {
                        PlaySpecialAmbient("Audio/oscar/sfx_amb_cave");
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

            if (Overworld.Map.GetBiomeAt(pos).HasValue(out var biome))
            {
                if (!string.IsNullOrEmpty(biome.DayAmbience))
                {
                    if (prevAmbience[0] != biome.DayAmbience)
                    {
                        if (!string.IsNullOrEmpty(prevAmbience[0]))
                            prevAmbience[0] = null;
                        if (!string.IsNullOrEmpty(prevAmbience[1]))
                            prevAmbience[1] = null;
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
}
