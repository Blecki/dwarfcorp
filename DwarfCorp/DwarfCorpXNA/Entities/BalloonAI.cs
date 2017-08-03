// BalloonAI.cs
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
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A simple hacked AI script for the DwarfCorp balloon. Has a state machine which makes it go up and down.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BalloonAI : GameComponent, IUpdateableComponent
    {
        public PIDController VelocityController { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float MaxVelocity { get; set; }
        public float MaxForce { get; set; }
        public BalloonState State { get; set; }
        public ShipmentOrder Shipment { get; set; }
        public Faction Faction { get; set; }

        public List<ResourceAmount> CurrentResources { get; set; }

        private bool shipmentGiven = false;

        public enum BalloonState
        {
            DeliveringGoods,
            Waiting,
            Leaving
        }

        public BalloonAI()
        {
            
        }

        public BalloonAI(ComponentManager Manager, Vector3 target, ShipmentOrder shipment, Faction faction) :
            base("BalloonAI", Manager)
        {
            VelocityController = new PIDController(0.9f, 0.5f, 0.0f);
            MaxVelocity = 2.0f;
            MaxForce = 15.0f;
            TargetPosition = target;
            State = BalloonState.DeliveringGoods;
            Shipment = shipment;
            Faction = faction;
            CurrentResources = new List<ResourceAmount>();
        }

        public override void Die()
        {
            if (!IsDead)
            {
                Parent.Die();
            }
        }

        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            var body = Parent as Body;
            System.Diagnostics.Debug.Assert(body != null);

            Vector3 targetVelocity = TargetPosition - body.GlobalTransform.Translation;

            if(targetVelocity.LengthSquared() > 0.0001f)
            {
                targetVelocity.Normalize();
                targetVelocity *= MaxVelocity;
            }

            Matrix m = body.LocalTransform;
            m.Translation += targetVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            body.LocalTransform = m;

            body.HasMoved = true;

            switch(State)
            {
                case BalloonState.DeliveringGoods:
                    {
                        var voxel = new VoxelHandle(chunks.ChunkData,
                            GlobalVoxelCoordinate.FromVector3(body.GlobalTransform.Translation));

                        if (voxel.IsValid)
                        {
                            var surfaceVoxel = VoxelHelpers.FindFirstVoxelBelow(voxel);
                            var height = surfaceVoxel.Coordinate.Y + 1;

                            TargetPosition = new Vector3(body.GlobalTransform.Translation.X, height + 5, body.GlobalTransform.Translation.Z);

                            Vector3 diff = body.GlobalTransform.Translation - TargetPosition;

                            if (diff.LengthSquared() < 2)
                            {
                                State = BalloonState.Waiting;
                            }
                        }
                        else
                        {
                            State = BalloonState.Leaving;
                        }
                    }
                    break;
                case BalloonState.Leaving:
                    TargetPosition = Vector3.UnitY * 100 + body.GlobalTransform.Translation;

                    if(body.GlobalTransform.Translation.Y > 300)
                    {
                        Die();
                    }

                    break;
                case BalloonState.Waiting:
                    TargetPosition = body.GlobalTransform.Translation;

                    if(!shipmentGiven)
                    {
                        
                        shipmentGiven = true;
                    }
                    else
                    {
                        State = BalloonState.Leaving;
                    }


                    break;
            }
        }
    }

}