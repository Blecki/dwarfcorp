using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This component draws a simple textured rectangular box.
    /// </summary>
    public class Box : Tinter
    {
        public OldBoxPrimitive Primitive { get; set; }
        public Texture2D Texture { get; set; }

        public Box(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, OldBoxPrimitive primitive, Texture2D tex) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos, false)
        {
            Primitive = primitive;
            Texture = tex;
        }

        public override void Render(DwarfTime DwarfTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            base.Render(DwarfTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            effect.Parameters["xTexture"].SetValue(Texture);
            effect.Parameters["xWorld"].SetValue(GlobalTransform);

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }
        }
    }

}