// Deer.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    [JsonObject(IsReference = true)]
    public class Deer : Creature
    {
        private float ANIM_SPEED = 5.0f;

        public Deer()
        {
            
        }

        public Deer(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name):
            base
            (
                new CreatureStats
                {
                    Dexterity = 12,
                    Constitution = 6,
                    Strength = 3,
                    Wisdom = 2,
                    Charisma = 1,
                    Intelligence = 3,
                    Size = 3
                },
                "Herbivore",
                DwarfGame.World.PlanService,
                manager.Factions.Factions["Herbivore"],
                new Physics
                (
                    "A Deer",
                    manager.RootComponent,
                    Matrix.CreateTranslation(position),
                    new Vector3(0.3f, 0.3f, 0.3f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                ),
                chunks, graphics, content, name
            )
        {
            Initialize(new SpriteSheet(sprites));
        }


        public void Initialize(SpriteSheet spriteSheet)
        {
            Physics.Orientation = Physics.OrientMode.RotateY;

            const int frameWidth = 48;
            const int frameHeight = 40;

            Sprite = new CharacterSprite
                (Graphics,
                Manager,
                "Deer Sprite",
                Physics,
                Matrix.CreateTranslation(Vector3.Up * 0.6f)
                );

            // Add the idle animation
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 1, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 2, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 3, 0);

            // Add the running animation
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 4, 0, 1, 2, 3);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 5, 0, 1, 2, 3);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 6, 0, 1, 2, 3);
            Sprite.AddAnimation(CharacterMode.Walking, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 7, 0, 1, 2, 3);

            // Add the jumping animation
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 8, 0, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 9, 0, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 10, 0, 1);
            Sprite.AddAnimation(CharacterMode.Jumping, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 11, 0, 1);

            // Add hands
            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            // Add sensor
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Add AI
            AI = new PacingCreatureAI(this, "Deer AI", Sensors, PlanService);

            Attacks = new List<Attack>{new Attack("None", 0.0f, 0.0f, 0.0f, ContentPaths.Audio.pick, ContentPaths.Effects.hit)};

            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 1
                }
            };

            // Shadow
            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -.25f, 0.0f);
            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);
            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, shadowTexture);

            List<Point> shP = new List<Point>
            {
                new Point(0,0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");

            // The bird will emit a shower of blood when it dies
            DeathParticleTrigger = new ParticleTrigger("blood_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10
            };

            // The bird is flammable, and can die when exposed to fire.
            Flames = new Flammable(Manager, "Flames", Physics, this);

            // Tag the physics component with some information 
            // that can be used later
            Physics.Tags.Add("Deer");
            Physics.Tags.Add("Animal");

            Stats.FullName = TextGenerator.GenerateRandom("$firstname");
            Stats.CurrentClass = new EmployeeClass()
            {
                Name = "Deer",
                Levels = new List<EmployeeClass.Level>() { new EmployeeClass.Level() { Index = 0, Name = "Deer"} }
            };

        }

    }
}
