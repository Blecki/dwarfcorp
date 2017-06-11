// Fairy.cs
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
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{

    public class Fairy : Creature, IUpdateableComponent
    {

        public Timer ParticleTimer { get; set; }
        public Timer DeathTimer { get; set; }
        public Fairy()
        {

        }
        public Fairy(ComponentManager manager, string allies, Vector3 position) :
            base(new CreatureStats(new FairyClass(), 0), "Player", manager.World.PlanService, manager.World.ComponentManager.Factions.Factions[allies],
           new Physics("Fairy", manager.World.ComponentManager.RootComponent, Matrix.CreateTranslation(position),
                       new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, 0, 0)),
              manager.World.ChunkManager, GameState.Game.GraphicsDevice, GameState.Game.Content, "Fairy")
        {
            HasMeat = false;
            HasBones = false;
            ParticleTimer = new Timer(0.2f, false);
            DeathTimer = new Timer(30.0f, true);
            Initialize(new FairyClass());
        }

        new public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (ParticleTimer.HasTriggered)
            {
                Manager.World.ParticleManager.Trigger("star_particle", Sprite.Position, Color.White, 1);    
            }
            DeathTimer.Update(gameTime);
            ParticleTimer.Update(gameTime);

            if (DeathTimer.HasTriggered)
            {
                Physics.Die();
            }

            base.Update(gameTime, chunks, camera);
        }

        

        public void Initialize(EmployeeClass dwarfClass)
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = new CharacterSprite(Graphics, Manager, "Fairy Sprite", Physics, Matrix.CreateTranslation(new Vector3(0, 0.5f, 0)));
            foreach (Animation animation in dwarfClass.Animations)
            {
                Sprite.AddAnimation(animation.Clone());
            }
            Sprite.LightsWithVoxels = false;

            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero);

            AI = new CreatureAI(this, "Fairy AI", Sensors, PlanService);

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform, new SpriteSheet(ContentPaths.Effects.shadowcircle));
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle), "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");
            Physics.Tags.Add("Dwarf");



            DeathParticleTrigger = new ParticleTrigger("star_particle", Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.wurp,
            };
          
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.tinkle
            };


            NoiseMaker.Noises["Chew"] = new List<string> 
            {
                ContentPaths.Audio.tinkle
            };

            NoiseMaker.Noises["Jump"] = new List<string>
            {
                ContentPaths.Audio.tinkle
            };

            MinimapIcon minimapIcon = new MinimapIcon(Physics, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 0, 0));

            //new LightEmitter("Light Emitter", Sprite, Matrix.Identity, Vector3.One, Vector3.One, 255, 150);
            new Bobber(0.25f, 3.0f, MathFunctions.Rand(), Sprite);
          
            Stats.FullName = TextGenerator.GenerateRandom("$firstname");
            //Stats.LastName = "The Fairy";
            
            Stats.CanSleep = false;
            Stats.CanEat = false;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            Species = "Fairy";
        }
    }

}
