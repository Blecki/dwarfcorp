using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorpCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This component has a color tint which can change over time.
    /// </summary>
    public class Tinter : Body
    {
        public bool LightsWithVoxels { get; set; }
        private bool firstIteration = true;
        public Color Tint { get; set; }
        public Color TargetTint { get; set; }
        public float TintChangeRate { get; set; }
        public Timer LightingTimer { get; set; }
        public Voxel VoxelUnder = null;
        public bool ColorAppplied = false;
        private bool entityLighting = GameSettings.Default.EntityLighting;

        public Timer StartTimer { get; set; }

        public Tinter()
        {
            
        }

        public Tinter(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool octree) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos, octree)
        {
            LightsWithVoxels = true;
            Tint = Color.White;
            LightingTimer = new Timer(0.2f, true);
            StartTimer = new Timer(0.5f, true);
            TargetTint = Tint;
            TintChangeRate = 1.0f;
            LightsWithVoxels = true;
            VoxelUnder = new Voxel();
        }


        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if(messageToReceive.MessageString == "Chunk Modified")
            {
                ColorAppplied = false;
            }
            base.ReceiveMessageRecursive(messageToReceive);
        }

        public bool ShouldUpdate()
        {
            if(!StartTimer.HasTriggered)
            {
                return false;
            }

            bool parentHasMoved = true;

            GameComponent root = GetRootComponent();

            if(root is Body)
            {
                Body loc = (Body) root;

                parentHasMoved = loc.HasMoved;
            }

            bool moved = HasMoved || parentHasMoved;

            return LightsWithVoxels && ((moved && LightingTimer.HasTriggered) || firstIteration || !ColorAppplied);
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            LightingTimer.Update(gameTime);
            StartTimer.Update(gameTime);
            if(ShouldUpdate())
            {
                if (entityLighting)
                {
                    bool success = chunks.ChunkData.GetFirstVoxelUnder(GlobalTransform.Translation, ref VoxelUnder);

                    if (success && !VoxelUnder.Chunk.IsRebuilding && VoxelUnder.Chunk.LightingCalculated)
                    {
                        Color color =
                            new Color(
                                VoxelUnder.Chunk.Data.SunColors[
                                    VoxelUnder.Chunk.Data.IndexAt((int) VoxelUnder.GridPosition.X, (int) VoxelUnder.GridPosition.Y + 1,
                                        (int) VoxelUnder.GridPosition.Z)], 255,
                                    0);

                        TargetTint = color;
                        firstIteration = false;
                        ColorAppplied = true;
                    }
                }
                else
                {
                    TargetTint = new Color(200, 255, 0);
                }

                LightingTimer.HasTriggered = false;
                LightingTimer.Reset(LightingTimer.TargetTimeSeconds);
            }
            else if(!entityLighting)
            {
                TargetTint = new Color(200, 255, 0);
            }
            else if(LightsWithVoxels)
            {
                Vector4 lerpTint = new Vector4((float) TargetTint.R / 255.0f, (float) TargetTint.G / 255.0f, (float) TargetTint.B / 255.0f, (float) TargetTint.A / 255.0f);
                Vector4 currTint = new Vector4((float) Tint.R / 255.0f, (float) Tint.G / 255.0f, (float) Tint.B / 255.0f, (float) Tint.A / 255.0f);

                Vector4 delta = lerpTint - currTint;
                lerpTint = currTint + delta * Math.Max(Math.Min(LightingTimer.CurrentTimeSeconds * TintChangeRate, 1.0f), 0.0f);

                //Tint = new Color(lerpTint.X, lerpTint.Y, lerpTint.Z, lerpTint.W);
                Tint = TargetTint;
            }

            base.Update(gameTime, chunks, camera);
        }

        public override void Render(GameTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            if(IsVisible)
            {
                effect.Parameters["xTint"].SetValue(new Vector4(Tint.R, Tint.G, Tint.B, Tint.A));

                base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            }
        }
    }

}