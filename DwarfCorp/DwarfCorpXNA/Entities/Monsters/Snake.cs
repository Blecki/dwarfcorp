// Snake.cs
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
    public class Snake : Creature
    {
        private float ANIM_SPEED = 5.0f;
        public Physics[] Tail;

        public Snake(string sprites, Vector3 position, ComponentManager manager, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, string name):
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
                manager.World.PlanService,
                manager.Factions.Factions["Herbivore"],
                new Physics
                (
                    "snake",
                    manager.RootComponent,
                    Matrix.CreateTranslation(position),
                    new Vector3(1, .3f, .5f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.999f, 0.999f,
                    new Vector3(0, -10, 0)
                ),
                chunks, graphics, content, name
            )
        {
            Tail = new Physics[5];
            for (int i = 0; i < 5; ++i)
            {
                Tail[i] = new Physics
                (
                    "snaketail",
                    manager.RootComponent,
                    Matrix.CreateTranslation(position),
                    new Vector3(2, 1.5f, .7f),
                    new Vector3(0, 0, 0),
                    1.0f, 1.0f, 0.995f, 0.999f,
                    new Vector3(0, -10, 0)
                );
            }
            Initialize(new SpriteSheet(sprites));
        }

        public void Initialize(SpriteSheet spriteSheet)
        {
            Physics.Orientation = Physics.OrientMode.Fixed;

            const int frameWidth = 32;
            const int frameHeight = 32;

            Sprite = new CharacterSprite
                (Graphics,
                Manager,
                "snake Sprite",
                Physics,
                Matrix.Identity
                );

            // Add the idle animation
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);
            Sprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 0);

            for (int i = 0; i < 5; ++i)
            {
                CharacterSprite TailSprite = new CharacterSprite
                    (Graphics,
                    Manager,
                    "snake Sprite",
                    Tail[i],
                    Matrix.Identity
                    );

                TailSprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Forward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 1);
                TailSprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Left, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 1);
                TailSprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Right, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 1);
                TailSprite.AddAnimation(CharacterMode.Idle, OrientedAnimation.Orientation.Backward, spriteSheet, ANIM_SPEED, frameWidth, frameHeight, 0, 1);

                TailSprite.SetCurrentAnimation(CharacterMode.Idle.ToString());
            }
            
            // Add sensor
            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            // Add AI
            AI = new SnakeAI(this, "snake AI", Sensors, PlanService);


            Attacks = new List<Attack>() {new Attack("None", 0.0f, 0.0f, 0.0f, ContentPaths.Audio.pick, ContentPaths.Effects.hit)};

            Physics.Tags.Add("Snake");
            Physics.Tags.Add("Animal");

        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);
            Physics prev, next;
            prev = null;
            next = Physics;
            for (int i = 0; i < 5; i++)
            {
                prev = next;
                next = Tail[i];
                Vector3 prevT, nextT, distance;
                prevT = prev.GlobalTransform.Translation;
                nextT = next.GlobalTransform.Translation;
                distance = prevT - nextT;
                if (distance.LengthSquared() < .2f)
                {
                    prev.ApplyForce(distance * 1f, 1);
                    next.ApplyForce(distance * -1f, 1);
                }
                else
                {
                    prev.ApplyForce(distance * -1f, 1);
                    next.ApplyForce(distance * 1f, 1);
                }
            }
        }
    }
}
