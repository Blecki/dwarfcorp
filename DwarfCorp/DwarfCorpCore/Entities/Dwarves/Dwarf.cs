﻿// Dwarf.cs
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
using DwarfCorpCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// Convenience class for initializing Dwarves as Creatures.
    /// </summary>
    public class Dwarf : Creature
    {
        public Dwarf()
        {
            
        }
        public Dwarf(CreatureStats stats, string allies, PlanService planService, Faction faction,  string name, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, EmployeeClass workerClass, Vector3 position) :
            base(stats, allies, planService, faction, 
            new Physics( "Dwarf", WorldManager.ComponentManager.RootComponent, Matrix.CreateTranslation(position), 
                        new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0)),
               chunks, graphics, content, name)
        {
            HasMeat = false;
            HasBones = false;
            Initialize(workerClass);
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);
        }

        public void Initialize(EmployeeClass dwarfClass)
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = new CharacterSprite(Graphics, Manager, "Dwarf Sprite", Physics, Matrix.CreateTranslation(new Vector3(0, 0.15f, 0)));
            foreach (Animation animation in dwarfClass.Animations)
            {
                Sprite.AddAnimation(animation.Clone());
            }
            Sprite.SpriteSheet = Sprite.Animations.First().Value.SpriteSheet;
            Sprite.CurrentAnimation = Sprite.Animations.First().Value;
            Sprite.CurrentAnimation.NextFrame();
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            AI = new CreatureAI(this, "Dwarf AI", Sensors, PlanService);

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 128
                }
            };

            Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform,
                new SpriteSheet(ContentPaths.Effects.shadowcircle))
            {
                GlobalScale = 1.25f
            };
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");
            Physics.Tags.Add("Dwarf");

            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10, 
                SoundToPlay = ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
            };
            Flames = new Flammable(Manager, "Flames", Physics, this);

            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
                ContentPaths.Entities.Dwarf.Audio.dwarfhurt2,
                ContentPaths.Entities.Dwarf.Audio.dwarfhurt3,
                ContentPaths.Entities.Dwarf.Audio.dwarfhurt4,
            };

            NoiseMaker.Noises["Ok"] = new List<string>()
            {
                ContentPaths.Audio.ok0,
                ContentPaths.Audio.ok1,
                ContentPaths.Audio.ok2
            };


            NoiseMaker.Noises["Chew"] = new List<string> 
            {
                ContentPaths.Audio.chew
            };

            NoiseMaker.Noises["Jump"] = new List<string>
            {
                ContentPaths.Audio.jump
            };

            MinimapIcon minimapIcon = new MinimapIcon(Physics, new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.map_icons), 16, 0, 0));

            Stats.FullName = TextGenerator.GenerateRandom("$firstname", " ", "$lastname");
            Stats.Size = 5;
            Stats.CanSleep = true;
            Stats.CanEat = true;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.TriggersMourning = true;
        }
    }

}