// Door.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Door : Fixture
    {
        public Faction TeamFaction { get; set; }
        public Matrix ClosedTransform { get; set; }
        public Timer OpenTimer { get; set; }
        bool IsOpen { get; set; }
        bool IsMoving { get; set; }
        public Door()
        {
            IsOpen = false;
        }

        public Door(Vector3 position, Faction team, SpriteSheet sheet, Point frame, float hp) :
            base(
            position, sheet, frame,
            WorldManager.ComponentManager.RootComponent)
        {
            IsMoving = false;
            IsOpen = false;
            OpenTimer = new Timer(0.5f, false);
            TeamFaction = team;
            Name = "Door";
            Tags.Add("Door");
            this.Sprite.OrientationType = Sprite.OrientMode.Fixed;
            Sprite.OrientationType = Sprite.OrientMode.Fixed;
            Sprite.LocalTransform = Matrix.CreateRotationY(0.5f * (float)Math.PI);
            OrientToWalls();
            ClosedTransform = LocalTransform;
            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
            Health health = new Health(WorldManager.ComponentManager, "Health", this, hp, 0.0f, hp);
           
        }

        public Matrix CreateHingeTransform(float angle)
        {
            Vector3 hinge = new Vector3(0, 0, 0.5f);
            Vector3 center = new Vector3((float)Math.Sin(angle) * 0.5f, 0, (float)Math.Cos(angle) * 0.5f);
            return  Matrix.CreateRotationY(angle) * Matrix.CreateTranslation(center - hinge);
        }

        public void Open()
        {
            if (!IsOpen)
            {
                IsMoving = true;
                OpenTimer.Reset();
            }

            IsOpen = true;
        }

        public void Close()
        {
            if (IsOpen)
            {
                IsMoving = true;
                OpenTimer.Reset();
            }
            IsOpen = false;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (IsMoving)
            {
                OpenTimer.Update(gameTime);
                if (OpenTimer.HasTriggered)
                {
                    IsMoving = false;
                }
                else
                {
                    float t = Easing.CubicEaseInOut(OpenTimer.CurrentTimeSeconds, 0.0f, 1.0f,
                        OpenTimer.TargetTimeSeconds);
                    if (IsOpen)
                    {
                        LocalTransform = CreateHingeTransform(t*1.57f)*ClosedTransform;
                    }
                    else
                    {
                        LocalTransform = CreateHingeTransform((1.0f - t)*1.57f)*ClosedTransform;
                    }
                }
            }
            else
            {
                bool anyInside = false;
                foreach (CreatureAI minion in TeamFaction.Minions)
                {
                    if ((minion.Physics.Position - Position).LengthSquared() < 1)
                    {
                        if (!IsOpen)
                        {
                            Open();
                        }
                        anyInside = true;
                        break;
                    }
                }

                if (!IsMoving && !anyInside && IsOpen)
                {
                    Close();
                }
            }
            base.Update(gameTime, chunks, camera);
        }
    }

}
