using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public interface ITintable
    {
        void SetLightRamp(Color Tint);
        void SetVertexColor(Color Tint);
        void SetOneShotTint(Color Tint);
    }

    /// <summary>
    /// This component has a color tint which can change over time.
    /// </summary>
    public class Tinter : GameComponent, ITintable
    {
        public bool LightsWithVoxels { get; set; }
        public Color LightRamp { get; set; }
        public float TintChangeRate { get; set; }
        public bool ColorAppplied = false;
        private bool entityLighting = GameSettings.Current.EntityLighting;
        public Color VertexColorTint { get; set; }
        public bool Stipple { get; set; }
        private string previousEffect = null;
        private Color previousColor = Color.White;

        [JsonIgnore]
        public Color OneShotTint = Color.White;
        public Tinter()
        {
        }

        public Tinter(ComponentManager Manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(Manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            LightsWithVoxels = true;
            LightRamp = new Color(255, 255, 0);
            TintChangeRate = 1.0f;
            VertexColorTint = Color.White;
            Stipple = false;
        }


        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if(messageToReceive.MessageString == "Chunk Modified")
            {
                ColorAppplied = false;
            }
            base.ReceiveMessageRecursive(messageToReceive);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (!LightsWithVoxels)
                LightRamp = Color.White;

            if (entityLighting && LightsWithVoxels)
            {
                var under = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(Position));

                if (under.IsValid)
                    LightRamp = new Color(under.Sunlight ? 255 : 80, 255, 0);
            }
            else
                LightRamp = new Color(200, 255, 0);
        }

        public void ApplyTintingToEffect(Shader effect)
        {
            previousColor = effect.VertexColorTint;
            effect.LightRamp = LightRamp;
            var tintVec = VertexColorTint.ToVector4();
            var oneShotvec = OneShotTint.ToVector4();
            tintVec.X *= oneShotvec.X;
            tintVec.Y *= oneShotvec.Y;
            tintVec.Z *= oneShotvec.Z;
            tintVec.W *= oneShotvec.W;
            effect.VertexColorTint = new Color(tintVec);
            OneShotTint = Color.White;
#if DEBUG
            if(effect.CurrentTechnique.Name == Shader.Technique.Stipple)
            {
                throw new InvalidOperationException("Stipple technique not cleaned up. Was EndDraw called?");
            }
#endif
            if (Stipple && effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBuffer] && effect.CurrentTechnique != effect.Techniques[Shader.Technique.SelectionBufferInstanced]) 
            {
                previousEffect = effect.CurrentTechnique.Name;
                effect.CurrentTechnique = effect.Techniques[Shader.Technique.Stipple];
            }
            else
                previousEffect = null;
        }

        public void EndDraw(Shader shader)
        {
            if (!String.IsNullOrEmpty(previousEffect))
            {
                shader.CurrentTechnique = shader.Techniques[previousEffect];
            }
            shader.VertexColorTint = previousColor;
        }

        public void SetVertexColor(Color Tint)
        {
            VertexColorTint = Tint;
        }

        public void SetLightRamp(Color Tint)
        {
            LightRamp = Tint;
        }

        public void SetOneShotTint(Color Tint)
        {
            OneShotTint = Tint;
        }
    }

    public static class TintExtension
    {
        public static void SetVertexColorRecursive(this GameComponent component, Color color, bool oneShot=false)
        {
            foreach (var sprite in component.EnumerateAll().OfType<ITintable>())
            {
                if (!oneShot)
                    sprite.SetVertexColor(color);
                else
                    sprite.SetOneShotTint(color);
            }
        }
    }

}
