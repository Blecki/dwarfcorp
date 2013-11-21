using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class TintableComponent : LocatableComponent
    {
        public bool LightsWithVoxels { get; set; }
        private bool firstIteration = true;
        public Color Tint { get; set; }
        public Color TargetTint { get; set; }
        public float TintChangeRate { get; set; }
        public Timer LightingTimer { get; set; }
       
        public bool ColorAppplied = false;
        private bool entityLighting = GameSettings.Default.EntityLighting;

        public TintableComponent(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool octree) :
            base(manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos, octree)
        {
            LightsWithVoxels = true;
            Tint = Color.White;
            LightingTimer = new Timer(0.2f, true);
            TargetTint = Tint;
            TintChangeRate = 1.0f;
            LightsWithVoxels = true;
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
            bool parentHasMoved = true;

            GameComponent root = GetRootComponent();

            if(root is LocatableComponent)
            {
                LocatableComponent loc = (LocatableComponent) root;

                if(loc.HasMoved)
                {
                    parentHasMoved = true;
                }
                else
                {
                    parentHasMoved = false;
                }
            }

            bool moved = HasMoved || parentHasMoved;

            return LightsWithVoxels && ((moved && LightingTimer.HasTriggered) || firstIteration || !ColorAppplied);
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if(ShouldUpdate())
            {
                if(entityLighting)
                {
                    Voxel v = chunks.GetFirstVisibleBlockUnder(GlobalTransform.Translation, true);

                    if(v != null && !v.Chunk.IsRebuilding && v.Chunk.LightingCalculated)
                    {
                        //VoxelVertex bestKey = VoxelChunk.GetNearestDelta(GlobalTransform.Translation - v.Position);

                        Color color = new Color(v.Chunk.SunColors[(int) v.GridPosition.X][(int) v.GridPosition.Y + 1][(int) v.GridPosition.Z], 255, v.Chunk.DynamicColors[(int) v.GridPosition.X][(int) v.GridPosition.Y][(int) v.GridPosition.Z]);

                        TargetTint = color;
                        firstIteration = false;
                        ColorAppplied = true;
                    }
                    else
                    {
                        TargetTint = new Color(200, 255, 0);
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
                LightingTimer.Update(gameTime);
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