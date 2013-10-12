using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class TexturedBoxObject : TintableComponent
    {
        public BoxPrimitive Primitive { get; set;}
        public Texture2D Texture { get; set;}
        private static RasterizerState rasterState = new RasterizerState()
        {
            CullMode = CullMode.CullCounterClockwiseFace
        };

        public TexturedBoxObject(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, BoxPrimitive primitive, Texture2D tex) :
            base(manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos, false)
        {
            Primitive = primitive;
            Texture = tex;
        }

        public override void Render(GameTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {

            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
            Texture2D originalText = effect.Parameters["xTexture"].GetValueTexture2D();

            RasterizerState r = graphicsDevice.RasterizerState;
            graphicsDevice.RasterizerState = rasterState;
            effect.Parameters["xTexture"].SetValue(Texture);

            //Matrix oldWorld = effect.Parameters["xWorld"].GetValueMatrix();
            effect.Parameters["xView"].SetValue(camera.ViewMatrix);
            effect.Parameters["xProjection"].SetValue(camera.ProjectionMatrix);

            effect.Parameters["xWorld"].SetValue(GlobalTransform);

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Primitive.Render(graphicsDevice);
            }
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
            


            effect.Parameters["xTexture"].SetValue(originalText);


            graphicsDevice.RasterizerState = r;


        }


    }
}
