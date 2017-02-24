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
            LightDir = new Vector3(0.2f, -1, 0.5f);
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
            ShadowMap = new RenderTarget2D(device, ShadowWith, ShadowHeight, true, SurfaceFormat.Rg32, DepthFormat.Depth16);
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

            BoundingBox transformedBBox = new BoundingBox { Min = MathFunctions.Min(transformedCorners), 
                                                           Max = MathFunctions.Max(transformedCorners) };

            float width = transformedBBox.Max.X - transformedBBox.Min.X;
            float height = transformedBBox.Max.Y - transformedBBox.Min.Y;
            float near = transformedBBox.Min.Z - 5;
            float far = transformedBBox.Max.Z + 10;
            LightProj = Matrix.CreateOrthographic(width, height, near, far);
        }

        public void ClearEdges(GraphicsDevice device)
        {
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            DwarfGame.SpriteBatch.Draw(Drawer2D.Pixel, new Rectangle(0, 0, ShadowMap.Width, 2), Color.Black);
            DwarfGame.SpriteBatch.Draw(Drawer2D.Pixel, new Rectangle(0, 0, 2, ShadowMap.Height), Color.Black);
            DwarfGame.SpriteBatch.Draw(Drawer2D.Pixel, new Rectangle(ShadowMap.Width - 2, 0, 2, ShadowMap.Height), Color.Black);
            DwarfGame.SpriteBatch.Draw(Drawer2D.Pixel, new Rectangle(0, ShadowMap.Height - 2, ShadowMap.Width, 2), Color.Black);
            DwarfGame.SpriteBatch.End();
        }

        public void UnbindShadowmap(GraphicsDevice device)
        {
            device.SetRenderTarget(null);
            ShadowTexture = (Texture2D)ShadowMap;
            device.DepthStencilState = OldDepthState;
            device.BlendState = OldBlendState;
        }

        public void BindShadowmapEffect(Shader effect)
        {
            effect.ShadowMap = ShadowTexture;
            effect.LightView = LightView;
            effect.LightProjection = LightProj;
        }

        public void BindShadowmap(GraphicsDevice device)
        {
            device.Textures[0] = null;
            device.Indices = null;
            device.SetVertexBuffer(null);
            device.BlendState = BlendState.NonPremultiplied;
            device.DepthStencilState = DepthStencilState.Default;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
            device.SamplerStates[0] = SamplerState.PointClamp;
            device.SetRenderTarget(null);
            device.SetRenderTarget(ShadowMap);
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);
            OldDepthState = device.DepthStencilState;
            OldBlendState = device.BlendState;
            device.DepthStencilState = DepthState;
            device.BlendState = BlendMode;
        }

        public void PrepareEffect(Shader effect, bool instanced)
        {
            effect.CurrentTechnique = instanced ? effect.Techniques[Shader.Technique.ShadowMapInstanced] : effect.Techniques[Shader.Technique.ShadowMap];
            effect.View = LightView;
            effect.Projection = LightProj;
            effect.EnbleFog = false;
        }

    }
}
