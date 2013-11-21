using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

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

        public void Render(GameTime time, GraphicsDevice device, Camera camera)
        {
            RenderNightSky(time, device, camera);
            RenderDaySky(time, device, camera);
            RenderSunMoon(time, device, camera, device.Viewport);
        }

        public void RenderDaySky(GameTime time, GraphicsDevice device, Camera camera)
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

        public void RenderNightSky(GameTime time, GraphicsDevice device, Camera camera)
        {
            SkyEffect.Parameters["SkyboxTexture"].SetValue(NightTexture);
            SkyEffect.Parameters["ViewMatrix"].SetValue(camera.ViewMatrix);
            SkyEffect.Parameters["ProjectionMatrix"].SetValue(camera.ProjectionMatrix);
            SkyEffect.Parameters["xTransparency"].SetValue(TimeOfDay);
            SkyEffect.Parameters["xRot"].SetValue(Matrix.CreateRotationZ(-(CosTime + 0.5f)));
            SkyEffect.Parameters["xTint"].SetValue(0.0f);
            SkyEffect.CurrentTechnique = SkyEffect.Techniques[0];
            foreach(ModelMesh mesh in SkyMesh.Meshes)
            {
                mesh.Draw();
            }
        }

        public void RenderSunMoon(GameTime time, GraphicsDevice device, Camera camera, Viewport viewPort)
        {
            Matrix rot = Matrix.CreateRotationZ(-(CosTime + 0.5f * (float) Math.PI));
            Vector3 sunPosition = new Vector3(100000, 0, 0) + camera.Position;
            Vector3 moonPosition = new Vector3(-100000, 0, 0) + camera.Position;
            sunPosition = Vector3.Transform(sunPosition, rot);
            moonPosition = Vector3.Transform(moonPosition, rot);


            Vector3 cameraFrameSun = Vector3.Transform(sunPosition, camera.ViewMatrix * camera.ProjectionMatrix);
            Vector3 cameraFramMoon = Vector3.Transform(moonPosition, camera.ViewMatrix * camera.ProjectionMatrix);


            Vector3 unProjectSun = viewPort.Project(sunPosition, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 unProjectMoon = viewPort.Project(moonPosition, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);

            if(unProjectSun.Z > 0 && cameraFrameSun.Z > 0)
            {
                DwarfGame.SpriteBatch.Draw(SunTexture, new Vector2(unProjectSun.X - SunTexture.Width / 2, unProjectSun.Y - SunTexture.Height / 2), Color.White);
            }
            if(unProjectMoon.Z > 0 && cameraFramMoon.Z > 0)
            {
                DwarfGame.SpriteBatch.Draw(MoonTexture, new Vector2(unProjectMoon.X - MoonTexture.Width / 2, unProjectMoon.Y - MoonTexture.Height / 2), Color.White);
            }
            DwarfGame.SpriteBatch.End();
        }
    }

}