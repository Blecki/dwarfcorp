using Microsoft.Xna.Framework;
using System;

namespace DwarfCorp
{
    /// <summary>
    /// This component projects a billboard shadow to the ground below an entity.
    /// </summary>
    public class Shadow : SimpleSprite
    {
        public float GlobalScale { get; set; }
        public Timer UpdateTimer { get; set; }
        private Matrix OriginalTransform { get; set; }

        public static Shadow Create(float scale, ComponentManager Manager)
        {
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            var shadow = new Shadow(Manager, "Shadow", shadowTransform,
                new SpriteSheet(ContentPaths.Effects.shadowcircle))
            {
                GlobalScale = scale
            };
            shadow.SetFlag(Flag.ShouldSerialize, false);
            return shadow;
        }
        public Shadow() : base()
        {
        }

        public Shadow(ComponentManager Manager) :
            this(Manager, "Shadow", Matrix.CreateRotationX((float)Math.PI * 0.5f) *
            Matrix.CreateTranslation(Vector3.Down * 0.5f), new SpriteSheet(ContentPaths.Effects.shadowcircle))
        {
            GlobalScale = 1.0f;
            SetFlag(Flag.ShouldSerialize, false);
        }

        public Shadow(ComponentManager manager, string name, Matrix localTransform, SpriteSheet spriteSheet) :
            base(manager, name, localTransform, spriteSheet, Point.Zero)
        {
            OrientationType = OrientMode.Fixed;
            GlobalScale = LocalTransform.Left.Length();
            LightsWithVoxels = false;
            UpdateTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
            LightRamp = Color.Black;
            OriginalTransform = LocalTransform;
            SetFlag(Flag.ShouldSerialize, false);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            ImplementUpdate(gameTime, chunks);
            base.Update(gameTime, chunks, camera);
        }

        private void ImplementUpdate(DwarfTime gameTime, ChunkManager chunks)
        {
            UpdateTimer.Update(gameTime);
            if (UpdateTimer.HasTriggered)
            {
                if (Parent.HasValue(out var p))
                {

                    var voxelBelow = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(p.GlobalTransform.Translation + Vector3.Down * 0.25f));

                    if (voxelBelow.IsValid)
                    {
                        var shadowTarget = VoxelHelpers.FindFirstVoxelBelow(voxelBelow);

                        if (shadowTarget.IsValid)
                        {
                            var h = shadowTarget.Coordinate.Y + 1;
                            Vector3 pos = p.GlobalTransform.Translation;
                            pos.Y = h;
                            pos += VertexNoise.GetNoiseVectorFromRepeatingTexture(pos);
                            float scaleFactor = GlobalScale / (Math.Max((p.GlobalTransform.Translation.Y - h) * 0.25f, 1));
                            Matrix newTrans = OriginalTransform;
                            newTrans *= Matrix.CreateScale(scaleFactor);
                            newTrans.Translation = (pos - p.GlobalTransform.Translation) + new Vector3(0.0f, 0.05f, 0.0f);
                            LightRamp = new Color(LightRamp.R, LightRamp.G, LightRamp.B, (int)(scaleFactor * 255));
                            Matrix globalRotation = p.GlobalTransform;
                            globalRotation.Translation = Vector3.Zero;
                            LocalTransform = newTrans * Matrix.Invert(globalRotation);
                        }
                    }
                    UpdateTimer.HasTriggered = false;
                }
            }
        }

        public override void UpdatePaused(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            ImplementUpdate(Time, Chunks);
            base.UpdatePaused(Time, Chunks, Camera);
        }
    }

}