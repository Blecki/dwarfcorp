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

namespace DwarfCorp
{
    /// <summary>
    /// This component has a color tint which can change over time.
    /// </summary>
    public class Tinter : Body, IUpdateableComponent
    {
        public bool LightsWithVoxels { get; set; }
        public Color Tint { get; set; }
        public Color TargetTint { get; set; }
        public float TintChangeRate { get; set; }
        public Timer LightingTimer { get; set; }
        public Voxel VoxelUnder = null;
        public bool ColorAppplied = false;
        private bool entityLighting = GameSettings.Default.EntityLighting;
        public Color VertexColorTint { get; set; }
        public Timer StartTimer { get; set; }

        public Tinter()
        {
            
        }

        public Tinter(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, bool collisionManager) :
            base(parent.Manager, name, parent, localTransform, boundingBoxExtents, boundingBoxPos, collisionManager)
        {
            LightsWithVoxels = true;
            Tint = new Color(255, 255, 0);
            LightingTimer = new Timer(0.2f, true);
            StartTimer = new Timer(0.5f, true);
            TargetTint = Tint;
            TintChangeRate = 1.0f;
            LightsWithVoxels = true;
            VoxelUnder = new Voxel();
            VertexColorTint = Color.White;
        }


        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if(messageToReceive.MessageString == "Chunk Modified")
            {
                ColorAppplied = false;
            }
            base.ReceiveMessageRecursive(messageToReceive);
        }

        public bool ShouldUpdate()
        {
            if(!StartTimer.HasTriggered)
            {
                return false;
            }

            return LightingTimer.HasTriggered;
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            LightingTimer.Update(gameTime);
            StartTimer.Update(gameTime);

            if (!LightsWithVoxels)
            {
                Tint = Color.White;
            }

            if(ShouldUpdate())
            {
                if (entityLighting)
                {
                    bool success = chunks.ChunkData.GetVoxel(Position, ref VoxelUnder);

                    if (success)
                    {
                        Color color = new Color(VoxelUnder.SunColor, 255, 0);

                        TargetTint = color;
                        ColorAppplied = true;
                    }
                }
                else
                {
                    TargetTint = new Color(200, 255, 0);
                }

                LightingTimer.HasTriggered = false;
                LightingTimer.Reset(LightingTimer.TargetTimeSeconds);
            }
            else if(!entityLighting)
            {
                TargetTint = new Color(200, 255, 0);
            }
            else if(LightsWithVoxels)
            {
                //Vector4 lerpTint = new Vector4((float) TargetTint.R / 255.0f, (float) TargetTint.G / 255.0f, (float) TargetTint.B / 255.0f, (float) TargetTint.A / 255.0f);
                //Vector4 currTint = new Vector4((float) Tint.R / 255.0f, (float) Tint.G / 255.0f, (float) Tint.B / 255.0f, (float) Tint.A / 255.0f);

                //Vector4 delta = lerpTint - currTint;
                //lerpTint = currTint + delta * Math.Max(Math.Min(LightingTimer.CurrentTimeSeconds * TintChangeRate, 1.0f), 0.0f);

                //Tint = new Color(lerpTint.X, lerpTint.Y, lerpTint.Z, lerpTint.W);
                Tint = TargetTint;
            }

            base.Update(gameTime, chunks, camera);
        }

        public void ApplyTintingToEffect(Shader effect)
        {
            if (IsVisible)
            {
                effect.LightRampTint = Tint;
                effect.VertexColorTint = VertexColorTint;
            }
        }
    }

}
