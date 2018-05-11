// Tinter.cs
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

namespace DwarfCorp
{
    public interface ITintable
    {
        void SetTint(Color Tint);
        void SetOneShotTint(Color Tint);
    }

    /// <summary>
    /// This component has a color tint which can change over time.
    /// </summary>
    public class Tinter : Body, IUpdateableComponent, ITintable
    {
        public bool LightsWithVoxels { get; set; }
        public Color Tint { get; set; }
        public float TintChangeRate { get; set; }
        public bool ColorAppplied = false;
        private bool entityLighting = GameSettings.Default.EntityLighting;
        public Color VertexColorTint { get; set; }
        public bool Stipple { get; set; }
        private string previousEffect = null;
        private Color previousColor = Color.White;

        [JsonIgnore]
        public Color OneShotTint = Color.White;
        public Tinter()
        {
            Stipple = false;
        }

        public Tinter(ComponentManager Manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool collisionManager) :
            base(Manager, name, localTransform, boundingBoxExtents, boundingBoxPos, collisionManager)
        {
            LightsWithVoxels = true;
            Tint = new Color(255, 255, 0);
            TintChangeRate = 1.0f;
            VertexColorTint = Color.White;
            Stipple = false;
            SetFlag(Flag.FrustumCull, true);
        }


        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if(messageToReceive.MessageString == "Chunk Modified")
            {
                ColorAppplied = false;
            }
            base.ReceiveMessageRecursive(messageToReceive);
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (!LightsWithVoxels)
                Tint = Color.White;

            if (entityLighting && LightsWithVoxels)
            {
                var under = new VoxelHandle(chunks.ChunkData,
                    GlobalVoxelCoordinate.FromVector3(Position));

                if (under.IsValid)
                {
                    Color color = new Color(under.SunColor, 255, 0);

                    Tint = color;
                }
            }
            else
            {
                Tint = new Color(200, 255, 0);
            }
        }

        public void ApplyTintingToEffect(Shader effect)
        {
            previousColor = effect.VertexColorTint;
            effect.LightRampTint = Tint;
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
            {
                previousEffect = null;
            }
        }

        public void EndDraw(Shader shader)
        {
            if (!String.IsNullOrEmpty(previousEffect))
            {
                shader.CurrentTechnique = shader.Techniques[previousEffect];
            }
            shader.VertexColorTint = previousColor;
        }

        public void SetTint(Color Tint)
        {
            VertexColorTint = Tint;
        }

        public void SetOneShotTint(Color Tint)
        {
            OneShotTint = Tint;
        }
    }

    public static class TintExtension
    {
        public static void SetTintRecursive(this GameComponent component, Color color, bool oneShot=false)
        {
            foreach (var sprite in component.EnumerateAll().OfType<ITintable>())
            {
                if (!oneShot)
                    sprite.SetTint(color);
                else
                    sprite.SetOneShotTint(color);
            }
        }
    }

}
