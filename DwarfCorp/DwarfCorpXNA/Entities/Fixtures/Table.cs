// Table.cs
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
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Table : Body, IUpdateableComponent
    {
        public ManaBattery Battery { get; set; }

        public class ManaBattery
        {
            public ResourceEntity ManaSprite { get; set; }
            public float Charge { get; set; }
            public float MaxCharge { get; set; }
            public static float ChargeRate = 5.0f;
            public Timer ReCreateTimer { get; set; }
            public Timer ChargeTimer { get; set; }
            public ManaBattery()
            {
                ReCreateTimer = new Timer(3.0f, false);
                ChargeTimer = new Timer(1.0f, false);
            }

            public void Update(DwarfTime time, WorldManager world)
            {
                ChargeTimer.Update(time);
                if (ManaSprite != null && Charge > 0.0f && world.Master.Spells.Mana < world.Master.Spells.MaxMana)
                {
                    if (ChargeTimer.HasTriggered)
                    {
                        SoundManager.PlaySound(ContentPaths.Audio.tinkle, ManaSprite.Position);
                        IndicatorManager.DrawIndicator("+" + (int)ChargeRate + " M", ManaSprite.Position, 1.0f, Color.Green);
                        world.ParticleManager.Trigger("star_particle", ManaSprite.Position, Color.White, 1);
                        Charge -= ChargeRate;
                        world.Master.Spells.Recharge(ChargeRate);
                    }

                }
                else if (Charge <= 0.01f)
                {
                    ReCreateTimer.Update(time);
                }
            }

            public bool CreateNewManaSprite(Faction faction, Vector3 position, WorldManager world)
            {
                if (ManaSprite != null)
                {
                    ManaSprite.Die();  
                    world.ParticleManager.Trigger("star_particle", position, Color.White, 5);
                    SoundManager.PlaySound(ContentPaths.Audio.wurp, position, true);
                    ManaSprite = null;
                    ReCreateTimer.Reset();
                }
                if (ReCreateTimer.HasTriggered)
                {
                    if (faction.RemoveResources(
                        new List<ResourceAmount>() {new ResourceAmount(ResourceLibrary.ResourceType.Mana)}, position + Vector3.Up * 0.5f))
                    {
                        ManaSprite = EntityFactory.CreateEntity<ResourceEntity>("Mana Resource", position);
                        ManaSprite.Gravity = Vector3.Zero;
                        ManaSprite.CollideMode = Physics.CollisionMode.None;
                        
                        ManaSprite.Tags.Clear();
                        return true;
                    }
                }

                return false;
            }

            public void Reset(Faction faction, Vector3 position, WorldManager world)
            {
                if (CreateNewManaSprite(faction, position, world))
                {
                    Charge = MaxCharge;
                }
            }
        }

        public Table()
        {
            
        }

        public Table(ComponentManager componentManager, Vector3 position):
            this(componentManager, position, null, Point.Zero)
        {
            
        }

        public Table(ComponentManager manager, Vector3 position, string asset) :
            this(manager, position, new SpriteSheet(asset), Point.Zero)
        {

        }

        new public void Update(DwarfTime time, ChunkManager chunks, Camera camera)
        {
            if (Battery != null)
            {
                Battery.Update(time, Manager.World);

                if(Battery.Charge <= 0)
                {
                    Battery.Reset(Manager.World.PlayerFaction, Position + Vector3.Up, Manager.World);
                }
            }
            base.Update(time, chunks, camera);
        }

        public Table(ComponentManager manager, Vector3 position, SpriteSheet fixtureAsset, Point fixtureFrame) :
            base(manager, "Table", Matrix.Identity, new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero)
        {
            Matrix matrix = Matrix.CreateRotationY((float)Math.PI * 0.5f);
            matrix.Translation = position;
            LocalTransform = matrix;

            SpriteSheet spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture);
            Point topFrame = new Point(0, 6);
            Point sideFrame = new Point(1, 6);

            List<Point> frames = new List<Point>
            {
                topFrame
            };

            List<Point> sideframes = new List<Point>
            {
                sideFrame
            };

            Animation tableTop = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "tableTop", 32, 32, frames, false, Color.White, 0.01f, 1.0f, 1.0f, false);
            Animation tableAnimation = new Animation(GameState.Game.GraphicsDevice, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture), "tableTop", 32, 32, sideframes, false, Color.White, 0.01f, 1.0f, 1.0f, false);

            var tabletopSprite = AddChild(new Sprite(Manager, "sprite1", Matrix.CreateRotationX((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            }) as Sprite;
            tabletopSprite.AddAnimation(tableTop);

            var sprite = AddChild(new Sprite(Manager, "sprite", Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.Identity, spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            }) as Sprite;
            sprite.AddAnimation(tableAnimation);

            var sprite2 = AddChild(new Sprite(Manager, "sprite2", Matrix.CreateTranslation(0.0f, -0.05f, -0.0f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), spriteSheet, false)
            {
                OrientationType = Sprite.OrientMode.Fixed
            }) as Sprite;
            sprite2.AddAnimation(tableAnimation);

            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(position)));
            if (voxelUnder.IsValid)
                AddChild(new VoxelListener(manager, manager.World.ChunkManager,
                    voxelUnder));



            tableAnimation.Play();
            Tags.Add("Table");
            CollisionType = CollisionManager.CollisionType.Static;

            if (fixtureAsset != null)
                AddChild(new Fixture(Manager, new Vector3(0, 0.3f, 0), fixtureAsset, fixtureFrame));
        }
    }
}
