// Moleman.cs
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

namespace DwarfCorp
{

    /// <summary>
    /// Convenience class for initializing Necromancers as creatures.
    /// </summary>
    public class Moleman : Creature
    {
        public Moleman(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, ChunkManager chunks, GraphicsDevice graphics, ContentManager content, Vector3 position) :
            base(manager, stats, allies, planService, faction, 
                 chunks, graphics, content, name)
        {
            Physics = new Physics(manager, "Moleman", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Initialize();
        }

        public void Initialize()
        {
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = Physics.AddChild(new CharacterSprite(Graphics, Manager, "Moleman Sprite", Matrix.CreateTranslation(new Vector3(0, 0.1f, 0)))) as CharacterSprite;
            foreach (Animation animation in Stats.CurrentClass.Animations)
            {
                Sprite.AddAnimation(animation.Clone());
            }



            Hands = Physics.AddChild(new Grabber("hands", Manager, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero)) as Grabber;

            Sensors = Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero)) as EnemySensor;

            AI = AddChild(new CreatureAI(Manager, "Moleman AI", Sensors, PlanService)) as CreatureAI;

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Inventory = Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.BoundingBoxPos)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = 16
                }
            }) as Inventory;

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

            SpriteSheet shadowTexture = new SpriteSheet(ContentPaths.Effects.shadowcircle);

            Shadow = Physics.AddChild(new Shadow(Manager, "Shadow", shadowTransform, shadowTexture)) as Shadow;
            List<Point> shP = new List<Point>
            {
                new Point(0, 0)
            };
            Animation shadowAnimation = new Animation(Graphics, shadowTexture, "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
            Shadow.AddAnimation(shadowAnimation);
            shadowAnimation.Play();
            Shadow.SetCurrentAnimation("sh");
            Physics.Tags.Add("Necromancer");

            DeathParticleTrigger = Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 5,
                SoundToPlay = ContentPaths.Audio.gravel
            }) as ParticleTrigger;

            Flames = Physics.AddChild(new Flammable(Manager, "Flames", this)) as Flammable;


            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.mole0,
                ContentPaths.Audio.mole1,
                ContentPaths.Audio.mole2
            };

            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 3, 0)));

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            //Stats.LastName = TextGenerator.GenerateRandom("$goblinfamily");
            Stats.Size = 4;
            Stats.CanSleep = false;
            Stats.CanEat = false;
            AI.Movement.CanClimbWalls = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);

        }
    }

}
