using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.LayeredSprites
{
    public class LayeredCharacterSprite : CharacterSprite
    {
        public override void AddAnimation(Animation animation)
        {
            base.AddAnimation(Layers.ProxyAnimation(animation));
        }

        public override void SetAnimations(Dictionary<string, Animation> Animations)
        {
            this.Animations.Clear();
            foreach (var anim in Animations)
                AddAnimation(anim.Value);
        }

        private LayerStack Layers = new LayerStack();

        public LayerStack GetLayers()
        {
            return Layers;
        }

        public void AddLayer(Layer Layer, Palette Palette)
        {
            Layers.AddLayer(Layer, Palette);
        }

        public void RemoveLayer(LayerType Type)
        {
            Layers.RemoveLayer(Type);
        }

        public LayeredCharacterSprite()
        {
        }

        public LayeredCharacterSprite(ComponentManager manager, string name, Matrix localTransform) :
                base(manager, name, localTransform)
        {
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);
            Layers.Update(chunks.World.GraphicsDevice);
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }
    }
}
