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
    public class Door : CraftedFixture, IUpdateableComponent
    {
        public Faction TeamFaction { get; set; }
        public Matrix ClosedTransform { get; set; }
        public Timer OpenTimer { get; set; }
        bool IsOpen { get; set; }
        bool IsMoving { get; set; }


        protected static Dictionary<Resource.ResourceTags, Point> Sprites = new Dictionary<Resource.ResourceTags, Point>()
        {
            {
                Resource.ResourceTags.Metal,
                new Point(1, 8)
            },
            {
                Resource.ResourceTags.Stone,
                new Point(0, 8)
            },
            {
                Resource.ResourceTags.Wood,
                new Point(3, 1)
            }
        };

        protected static Dictionary<Resource.ResourceTags, float> Healths = new Dictionary<Resource.ResourceTags, float>()
        {
            {
                Resource.ResourceTags.Metal,
                75.0f
            },
            {
                Resource.ResourceTags.Stone,
                80.0f
            },
            {
                Resource.ResourceTags.Wood,
                30.0f
            }
        };

        protected static float DefaultHealth = 30.0f;
        protected static Point DefaultSprite = new Point(0, 8);

        protected static float GetHealth(ResourceLibrary.ResourceType type)
        {
            var resource = ResourceLibrary.GetResourceByName(type);
            foreach(var tag in resource.Tags)
            {
                if (Healths.ContainsKey(tag))
                {
                    return Healths[tag];
                }
            }
            return DefaultHealth;
        }

        public Door()
        {
            IsOpen = false;
        }

        public Door(ComponentManager manager, Vector3 position, Faction team, List<ResourceAmount> resourceType) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new FixtureCraftDetails(manager)
            {
                Resources = resourceType.ConvertAll(p => new ResourceAmount(p)),
                Sprites = Door.Sprites,
                DefaultSpriteFrame = Door.DefaultSprite
            }, SimpleSprite.OrientMode.Fixed)
        {
            IsMoving = false;
            IsOpen = false;
            OpenTimer = new Timer(0.5f, false);
            TeamFaction = team;
            Name = resourceType.FirstOrDefault().ResourceType + " Door";
            Tags.Add("Door");

            OrientToWalls();
            ClosedTransform = LocalTransform;
            AddToCollisionManager = true;
            CollisionType = CollisionManager.CollisionType.Static;
            var hp = GetHealth(resourceType.FirstOrDefault().ResourceType);
            AddChild(new Health(manager, "Health", hp, 0.0f, hp));
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            GetComponent<SimpleSprite>().OrientationType = SimpleSprite.OrientMode.Fixed;
            GetComponent<SimpleSprite>().LocalTransform = Matrix.CreateScale(new Vector3(-1.0f, 1.0f, 1.0f)) * Matrix.CreateRotationY(0.5f * (float)Math.PI);
        }

        public Matrix CreateHingeTransform(float angle)
        {
            Matrix toReturn = Matrix.Identity;
            Vector3 hinge = new Vector3(0, 0, -0.5f);
            toReturn = Matrix.CreateTranslation(-hinge) * toReturn;
            toReturn = Matrix.CreateRotationY(angle) * toReturn;
            toReturn = Matrix.CreateTranslation(hinge)* toReturn;
            return toReturn;
        }

        public void Open()
        {
            if (!IsOpen)
            {
                IsMoving = true;
                OpenTimer.Reset();
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_door_open_generic, Position, true, 0.5f);
            }

            IsOpen = true;
        }

        public void Close()
        {
            if (IsOpen)
            {
                IsMoving = true;
                OpenTimer.Reset();
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_env_door_close_generic, Position, true, 0.5f);
            }
            IsOpen = false;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!Active)
            {
                return;
            }

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
