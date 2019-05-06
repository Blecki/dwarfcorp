using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class PrimitiveComponent : Tinter
    {
        private GeometricPrimitive Primitive;
        private string Asset;
        private Texture2D Texture;

        public PrimitiveComponent(
            ComponentManager Manager, 
            Matrix localTransform, 
            Vector3 boundingBoxExtents, 
            Vector3 boundingBoxPos, 
            GeometricPrimitive Primitive, 
            String Asset) :
            base(Manager, "primitive", localTransform, boundingBoxExtents, boundingBoxPos)
        {
            this.Primitive = Primitive;
            this.Asset = Asset;
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            if (Texture == null || Texture.IsDisposed || Texture.GraphicsDevice.IsDisposed)
                Texture = AssetManager.GetContentTexture(Asset);

            ApplyTintingToEffect(effect);
            effect.MainTexture = Texture;
            effect.World = GlobalTransform;

            foreach(EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }
            EndDraw(effect);
        }
    }

}
