using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class ShadowRenderer
    {
        public Vector3 LightDir { get; set; }
        public Matrix LightView { get; set; }
        public Matrix LightProj { get; set; }
        public RenderTarget2D ShadowMap { get; set; }
        public Texture2D ShadowTexture { get; set; }
        public int ShadowWith { get; set; }
        public int ShadowHeight { get; set; }
        public DepthStencilState DepthState { get; set; }
        public DepthStencilState OldDepthState { get; set; }
        public BlendState BlendMode { get; set; }
        public BlendState OldBlendState { get; set; }

        public ShadowRenderer(GraphicsDevice device, int width, int height)
        {
            LightDir = Vector3.Down;
            LightView = Matrix.Identity;
            LightProj = Matrix.Identity;
            ShadowWith = width;
            ShadowHeight = height;
            DepthState = new DepthStencilState()
            {
                DepthBufferFunction = CompareFunction.LessEqual
            };

            BlendMode = BlendState.Opaque;

            InitializeShadowMap(device);
        }

        public void InitializeShadowMap(GraphicsDevice device)
        {
            ShadowMap = new RenderTarget2D(device, ShadowWith, ShadowHeight, true, device.PresentationParameters.BackBufferFormat, DepthFormat.Depth24);
        }

        public void SetupViewProj(BoundingBox worldBBox)
        {
            Vector3 lightPos = worldBBox.Center() - LightDir;
            Vector3 up = Vector3.Up;
            LightView = Matrix.CreateLookAt(lightPos, worldBBox.Center(), up);
            Vector3[] corners = worldBBox.GetCorners();
            Vector3[] transformedCorners = new Vector3[8];
            for(int i = 0; i < 8; i++)
            {
                transformedCorners[i] = Vector3.Transform(corners[i], LightView);
            }

            BoundingBox transformedBBox = new BoundingBox { Min = MathFunctions.Min(transformedCorners), Max = MathFunctions.Max(transformedCorners) };

            float width = transformedBBox.Max.X - transformedBBox.Min.X;
            float height = transformedBBox.Max.Y - transformedBBox.Min.Y;
            float near = transformedBBox.Min.Z - 10;
            float far = transformedBBox.Max.Z + 10;
            LightProj = Matrix.CreateOrthographic(width, height, near, far);
            //LightProj = Matrix.CreatePerspective(ShadowWith, ShadowHeight, 0.1f, 10000);
            //LightProj = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi - 0.5f, 1f, 1f, 1000f);
        }

        public void UnbindShadowmap(GraphicsDevice device)
        {
            device.SetRenderTarget(null);
            ShadowTexture = (Texture2D)ShadowMap;
            device.DepthStencilState = OldDepthState;
            device.BlendState = OldBlendState;
        }

        public void BindShadowmapEffect(Effect effect)
        {
            effect.Parameters["xShadowMap"].SetValue(ShadowTexture);
            effect.Parameters["xLightView"].SetValue(LightView);
            effect.Parameters["xLightProj"].SetValue(LightProj);
        }

        public void BindShadowmap(GraphicsDevice device)
        {
            device.SetRenderTarget(ShadowMap);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1.0f, 0);
            OldDepthState = device.DepthStencilState;
            OldBlendState = device.BlendState;
            device.DepthStencilState = DepthState;
            device.BlendState = BlendMode;
        }

        public void PrepareEffect(Effect effect, bool instanced)
        {
            effect.CurrentTechnique = instanced ? effect.Techniques["ShadowInstanced"] : effect.Techniques["Shadow"];
            effect.Parameters["xView"].SetValue(LightView);
            effect.Parameters["xProjection"].SetValue(LightProj);
        }

    }
}
