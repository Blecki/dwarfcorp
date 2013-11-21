using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BillboardList : BillboardSpriteComponent
    {
        public List<Matrix> LocalTransforms { get; set; }
        public List<float> Rotations { get; set; }
        public List<Color> Tints { get; set; }
        public float CullDistance = 1000.0f;

        public BillboardList(ComponentManager manager,
            string name,
            GameComponent parent,
            Matrix localTransform,
            Texture2D spriteSheet,
            int numBillboards) :
                base(manager, name, parent, localTransform, spriteSheet, false)
        {
            LocalTransforms = new List<Matrix>(numBillboards);
            Rotations = new List<float>(numBillboards);
            Tints = new List<Color>(numBillboards);
            FrustrumCull = false;
            OrientationType = OrientMode.Spherical;
        }

        public void AddTransform(Matrix transform, float rotation, Color tint)
        {
            LocalTransforms.Add(transform);
            Rotations.Add(rotation);
            Tints.Add(tint);
        }

        public void RemoveTransform(int index)
        {
            if(index >= 0 && index < LocalTransforms.Count)
            {
                LocalTransforms.RemoveAt(index);
                Rotations.RemoveAt(index);
                Tints.RemoveAt(index);
            }
        }

        public override void Render(GameTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Effect effect, bool renderingForWater)
        {
            IsVisible = true;
            Matrix origTransform = m_globalTransform;
            for(int i = 0; i < LocalTransforms.Count; i++)
            {
                m_globalTransform = origTransform;
                m_globalTransform = LocalTransforms[i] * m_globalTransform;

                if((m_globalTransform.Translation - camera.Position).LengthSquared() < CullDistance)
                {
                    BillboardRotation = Rotations[i];
                    Tint = Tints[i];
                    base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
                }
            }
            m_globalTransform = origTransform;
        }
    }

}