using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This class renders a skybox and sky elements (like the sun and moon) to the screen.
    /// </summary>
    public class SkyRenderer
    {
        public TextureCube SkyTexture { get; set; }
        public TextureCube NightTexture { get; set; }
        public Model SkyMesh { get; set; }
        public Texture2D SkyGrad { get; set; }
        public Effect SkyEffect { get; set; }
        public float TimeOfDay { get; set; }
        public float CosTime { get; set; }
        public Texture2D MoonTexture { get; set; }
        public Texture2D SunTexture { get; set; }
        public Vector3 SunPosition { get; set; }
        public Vector3 SunlightDir { get; set; }

        public SkyRenderer(Texture2D moonTexture, Texture2D sunTexture, TextureCube skyTexture, TextureCube nightTexture, Texture2D skyGrad, Model skyMesh, Effect skyEffect)
        {
            SkyTexture = skyTexture;
            NightTexture = nightTexture;
            SkyMesh = skyMesh;
            SkyEffect = skyEffect;
            SkyGrad = skyGrad;
            SkyEffect.Parameters["SkyboxTexture"].SetValue(SkyTexture);
            SkyEffect.Parameters["TintTexture"].SetValue(SkyGrad);
            MoonTexture = moonTexture;
            SunTexture = sunTexture;
            TimeOfDay = 0.0f;
            CosTime = 0.0f;

            foreach(ModelMesh mesh in SkyMesh.Meshes)
            {
                foreach(ModelMeshPart part in mesh.MeshParts)
                {
                    part.Effect = SkyEffect;
                }
            }
        }

        public void Render(DwarfTime time, GraphicsDevice device, Camera camera, float scale)
        {
            RenderNightSky(time, device, camera);
            RenderDaySky(time, device, camera);
            RenderSunMoon(time, device, camera, device.Viewport, scale);
        }

        public void RenderDaySky(DwarfTime time, GraphicsDevice device, Camera camera)
        {
            SkyEffect.Parameters["SkyboxTexture"].SetValue(SkyTexture);
            SkyEffect.Parameters["ViewMatrix"].SetValue(camera.ViewMatrix);
            SkyEffect.Parameters["ProjectionMatrix"].SetValue(camera.ProjectionMatrix);
            SkyEffect.Parameters["xTransparency"].SetValue(1.0f - (float) Math.Pow(TimeOfDay, 2));
            SkyEffect.Parameters["xRot"].SetValue(Matrix.CreateRotationY((float) time.TotalGameTime.TotalSeconds * 0.005f));
            SkyEffect.CurrentTechnique = SkyEffect.Techniques[0];
            SkyEffect.Parameters["xTint"].SetValue(TimeOfDay);
            foreach(ModelMesh mesh in SkyMesh.Meshes)
            {
                mesh.Draw();
            }
        }

        public void RenderNightSky(DwarfTime time, GraphicsDevice device, Camera camera)
        {
            SkyEffect.Parameters["SkyboxTexture"].SetValue(NightTexture);
            SkyEffect.Parameters["ViewMatrix"].SetValue(camera.ViewMatrix);
            SkyEffect.Parameters["ProjectionMatrix"].SetValue(camera.ProjectionMatrix);
            SkyEffect.Parameters["xTransparency"].SetValue(TimeOfDay);
            SkyEffect.Parameters["xRot"].SetValue(Matrix.CreateRotationZ(-(CosTime + 0.5f) * (float) Math.PI));
            SkyEffect.Parameters["xTint"].SetValue(0.0f);
            SkyEffect.CurrentTechnique = SkyEffect.Techniques[0];
            foreach(ModelMesh mesh in SkyMesh.Meshes)
            {
                mesh.Draw();
            }
        }

        public void RenderSunMoon(DwarfTime time, GraphicsDevice device, Camera camera, Viewport viewPort, float scale)
        {
            Matrix rot = Matrix.CreateRotationZ((-CosTime + 0.5f * (float) Math.PI));
            SunPosition = new Vector3(-1000, 100, 0);
            Vector3 moonPosition = new Vector3(1000, 100, 0);
            SunPosition = Vector3.Transform(SunPosition, rot);
            moonPosition = Vector3.Transform(moonPosition, rot);
            SunPosition += camera.Position;
            moonPosition += camera.Position;


            Vector3 cameraFrameSun = Vector3.Transform(SunPosition, camera.ViewMatrix);
            Vector3 cameraFramMoon = Vector3.Transform(moonPosition, camera.ViewMatrix);


            Vector3 unProjectSun = viewPort.Project(SunPosition, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 unProjectMoon = viewPort.Project(moonPosition, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            if (cameraFrameSun.Z > 0.999f)
            {
                DwarfGame.SpriteBatch.Draw(SunTexture, new Vector2(unProjectSun.X - SunTexture.Width / 2 * scale, unProjectSun.Y - SunTexture.Height / 2 * scale), null, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.0f);
            }
            if (cameraFramMoon.Z > 0.999f)
            {
                DwarfGame.SpriteBatch.Draw(MoonTexture, new Vector2(unProjectMoon.X - SunTexture.Width / 2 * scale, unProjectMoon.Y - SunTexture.Height / 2 * scale), null, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0.0f);
            }
            DwarfGame.SpriteBatch.End();

            Vector3 sunDir = (camera.Position - SunPosition);
            sunDir.Normalize();
            SunlightDir = sunDir;
        }
    }

}