// OrientedAnimation.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp
{
    public class LayeredCharacterSprite : CharacterSprite, IRenderableComponent, IUpdateableComponent
    {
        private class LayeredAnimationProxy : Animation
        {
            private LayeredCharacterSprite Owner = null;

            public LayeredAnimationProxy(LayeredCharacterSprite Owner)
            {
                this.Owner = Owner;
            }

            public override Texture2D GetTexture()
            {
                return Owner.GetProxyTexture();
            }

            public override void UpdatePrimitive(BillboardPrimitive Primitive, int CurrentFrame)
            {
                // Obviously shouldn't be hard coded.
                var composite = Owner.GetProxyTexture();
                if (composite == null) return;

                SpriteSheet = new SpriteSheet(composite, 32, 40);
                base.UpdatePrimitive(Primitive, CurrentFrame);
            }

            public override bool CanUseInstancing { get => false; }
        }

        public Texture2D GetProxyTexture()
        {
            return Composite;
        }

        public override void AddAnimation(Animation animation)
        {
            var comp = animation as CompositeAnimation;
            var proxyAnim = new LayeredAnimationProxy(this)
            {
                Frames = comp == null ? animation.Frames : comp.CompositeFrames.Select(c => c.Cells[0].Tile).ToList(),
                Name = animation.Name,
                FrameHZ = 0.5f,
                Speeds = animation.Speeds,
                Tint = animation.Tint,
                Loops = animation.Loops
            };
            base.AddAnimation(proxyAnim);
        }

        public List<Texture2D> Layers = new List<Texture2D>();
        private RenderTarget2D Composite;

        public LayeredCharacterSprite()
        {
            
        }

        public LayeredCharacterSprite(ComponentManager manager, string name,  Matrix localTransform) :
                base(manager, name, localTransform)
        {
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            // Render the composite texture
            var maxSize = new Point(0, 0);
            foreach (var layer in Layers)
            {
                maxSize.X = Math.Max(layer.Width, maxSize.X);
                maxSize.Y = Math.Max(layer.Height, maxSize.Y);
            }

            if (maxSize.X == 0 || maxSize.Y == 0) return;

            var graphics = chunks.World.GraphicsDevice;

            if (Composite == null || Composite.Width != maxSize.X || Composite.Height != maxSize.Y)
                Composite = new RenderTarget2D(graphics, maxSize.X, maxSize.Y);

            graphics.SetRenderTarget(Composite);
            graphics.Clear(Color.Transparent);

            var effect = DwarfGame.GuiSkin.Effect;
            graphics.DepthStencilState = DepthStencilState.None;
            effect.CurrentTechnique = effect.Techniques[0];
            effect.Parameters["View"].SetValue(Matrix.Identity);
            effect.Parameters["Projection"].SetValue(Matrix.CreateOrthographicOffCenter(0, maxSize.X, maxSize.Y, 0, -32, 32));

            // Need to offset by the subpixel portion to avoid screen artifacts.
            // Remove this offset if porting to Monogame, monogame does it correctly.
#if GEMXNA
            effect.Parameters["World"].SetValue(Matrix.CreateTranslation(-0.5f, -0.5f, 0.0f));
#else
            effect.Parameters["World"].SetValue(Matrix.Identity);
#endif

            foreach (var layer in Layers)
            {
                effect.Parameters["Texture"].SetValue(layer);
                effect.CurrentTechnique.Passes[0].Apply();
                var mesh = Gui.Mesh.Quad().Scale(layer.Width, layer.Height);
                mesh.Render(graphics);
            }

            graphics.SetRenderTarget(null);
        }

        new public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            //If layers have changed, re-create the texture.

            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);            
        }

        
    }
}
