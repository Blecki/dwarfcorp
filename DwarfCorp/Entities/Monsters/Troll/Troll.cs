// Goblin.cs
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
    public class Troll : Creature
    {
        [EntityFactory("Troll")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Troll(
                new CreatureStats(SharedClass, 0),
                "Goblins",
                Manager.World.PlanService,
                Manager.World.Factions.Factions["Goblins"],
                Manager,
                "Troll",
                Position).Physics;
        }

        private static TrollClass SharedClass = new TrollClass();

        public Troll()
        {

        }
        public Troll(CreatureStats stats, string allies, PlanService planService, Faction faction, ComponentManager manager, string name, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(manager, "Troll", Matrix.CreateTranslation(position), new Vector3(0.5f, 0.9f, 0.5f), new Vector3(0.0f, 0.0f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateCosmeticChildren(Manager);

            HasMeat = true;
            HasBones = true;

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Troll AI", Sensors));

            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Troll");

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.GenerateRandom("$goblinname");
            AI.Movement.CanClimbWalls = true;
            AI.Movement.CanSwim = true;
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetSpeed(MoveType.Swim, 0.15f);
            AI.Movement.SetCan(MoveType.Dig, true);
            Species = "Troll";
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            Stats.CurrentClass = SharedClass;
            CreateSprite(AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.Troll.troll_animation, "Troll"), manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 1, 3))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_goblin_angered,
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 3,
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_ic_demon_hurt_1,
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }
    }
}
