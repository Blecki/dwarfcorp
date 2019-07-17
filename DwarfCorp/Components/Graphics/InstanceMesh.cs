using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class InstanceMesh : Tinter
    {
        public string ModelType { get; set; }

        [JsonIgnore]
        public NewInstanceData Instance { get; set; }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            Instance = new NewInstanceData(
                ModelType,
                GlobalTransform,
                LightRamp);
            Instance.SelectionBufferColor = this.GetGlobalIDColor();
        }

        public InstanceMesh()
        {

        }

        public InstanceMesh(ComponentManager Manager, string name, Matrix localTransform, string modelType, Vector3 BoundingBoxExtents, Vector3 BoundingBoxPos) :
            base(Manager, name, localTransform, BoundingBoxExtents, BoundingBoxPos)
        {
            PropogateTransforms();
            UpdateBoundingBox();
            ModelType = modelType;
            Instance = new NewInstanceData(ModelType, GlobalTransform, LightRamp);
            Instance.SelectionBufferColor = this.GetGlobalIDColor();
        }

        override public void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect, bool renderingForWater)
        {
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);

            if (IsVisible && !renderingForWater)
            {
                Instance.LightRamp = LightRamp;
                Instance.Transform = GlobalTransform;
                Instance.VertexColorTint = VertexColorTint;
                Instance.SelectionBufferColor = this.GetGlobalIDColor();
                Manager.World.Renderer.InstanceRenderer.RenderInstance(Instance, graphicsDevice, effect, camera, InstanceRenderMode.Normal);
            }
        }

        public override void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice, Shader effect)
        {
            base.RenderSelectionBuffer(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);

            if (IsVisible)
            {
                Instance.LightRamp = LightRamp;
                Instance.Transform = GlobalTransform;
                Instance.SelectionBufferColor = this.GetGlobalIDColor();
                Manager.World.Renderer.InstanceRenderer.RenderInstance(Instance, graphicsDevice, effect, camera, InstanceRenderMode.SelectionBuffer);
            }
        }
    }
}