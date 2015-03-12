using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This component projects a billboard shadow to the ground below an entity.
    /// </summary>
    public class Shadow : Sprite
    {
        public float GlobalScale { get; set; }
        public Timer UpdateTimer { get; set; }
        private Matrix OriginalTransform { get; set; }

        public Shadow() : base()
        {
            
        }

        public Shadow(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, SpriteSheet spriteSheet) :
            base(manager, name, parent, localTransform, spriteSheet, false)
        {
            OrientationType = OrientMode.Fixed;
            GlobalScale = LocalTransform.Left.Length();
            LightsWithVoxels = false;
            UpdateTimer = new Timer(0.5f, false);
            Tint = Color.Black;
            OriginalTransform = LocalTransform;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            UpdateTimer.Update(gameTime);
            if(HasMoved && UpdateTimer.HasTriggered)
            {
                Body p = (Body) Parent;

                VoxelChunk chunk = chunks.ChunkData.GetVoxelChunkAtWorldLocation(p.GlobalTransform.Translation);

                if(chunk != null)
                {
                    Vector3 g = chunk.WorldToGrid(p.GlobalTransform.Translation + Vector3.Down * 0.25f);

                    int h = chunk.GetFilledVoxelGridHeightAt((int) g.X, (int) g.Y, (int) g.Z);

                    if(h != -1)
                    {
                        Vector3 pos = p.GlobalTransform.Translation;
                        pos.Y = h;
                        pos += VertexNoise.GetNoiseVectorFromRepeatingTexture(pos + Vector3.Down * 0.25f);
                        float scaleFactor = GlobalScale / (Math.Max((p.GlobalTransform.Translation.Y - h) * 0.25f, 1));
                        Matrix newTrans = OriginalTransform;
                        newTrans *= Matrix.CreateScale(scaleFactor);
                        newTrans.Translation = (pos - p.GlobalTransform.Translation) + new Vector3(0.0f, 0.1f, 0.0f);
                        Tint = new Color(Tint.R, Tint.G, Tint.B, (int)(scaleFactor * 255));
                        LocalTransform = newTrans;
                    }
                }
                UpdateTimer.HasTriggered = false;
            }


            base.Update(gameTime, chunks, camera);
        }
    }

}