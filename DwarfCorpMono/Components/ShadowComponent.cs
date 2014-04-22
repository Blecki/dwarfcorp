using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class ShadowComponent : BillboardSpriteComponent
    {
        public float GlobalScale { get; set; }
        public Timer UpdateTimer { get; set; }
        public ShadowComponent(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Texture2D spriteSheet) :
            base(manager, name, parent, localTransform, spriteSheet, false)
        {
            OrientationType = OrientMode.Fixed;
            GlobalScale = 1.0f;
            LightsWithVoxels = false;
            UpdateTimer = new Timer(0.5f,false);
            Tint = Color.Black;

        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (HasMoved && UpdateTimer.HasTriggered)
            {
                LocatableComponent p = (LocatableComponent)Parent;

                VoxelChunk chunk = chunks.GetVoxelChunkAtWorldLocation(GlobalTransform.Translation);

                if(chunk != null)
                {

                    Vector3 g = chunk.WorldToGrid(p.GlobalTransform.Translation);

                    int h = chunk.GetFilledVoxelGridHeightAt((int)g.X, (int)g.Y, (int)g.Z);
                
                    if (h != -1)
                    {
                        Vector3 pos = p.GlobalTransform.Translation;
                        pos.Y = h;

                    
                        Matrix newTrans = LocalTransform;
                        newTrans.Translation = (pos - p.GlobalTransform.Translation) + new Vector3(0.0f, 0.15f, 0.0f) ;
                        LocalTransform = newTrans;
                    }
                    
                }
                UpdateTimer.HasTriggered = false;
            }
            else
            {
                UpdateTimer.Update(gameTime);
            }

            base.Update(gameTime, chunks, camera);
        }

    }
}
