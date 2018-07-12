// Dwarf.cs
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

    /// <summary>
    /// Convenience class for initializing Dwarves as Creatures.
    /// </summary>
    public class Dwarf : Creature
    {
        public Dwarf()
        {
            
        }

        public Dwarf(ComponentManager manager, CreatureStats stats, string allies, PlanService planService, Faction faction,  string name, EmployeeClass workerClass, Vector3 position) :
            base(manager, stats, allies, planService, faction, name)
        {
            Physics = new Physics(manager, "Dwarf", Matrix.CreateTranslation(position),
                        new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Physics.AddChild(new SelectionCircle(Manager)
            {
                IsVisible = false
            });

            HasMeat = false;
            HasBones = false;
            HasCorpse = true;
            Initialize(workerClass);
        }        

        public void Initialize(EmployeeClass dwarfClass)
        {
            Stats.Gender = Mating.RandomGender();
            Physics.Orientation = Physics.OrientMode.RotateY;
            CreateDwarfSprite(dwarfClass, Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(20, 5, 20), Vector3.Zero));

            Physics.AddChild(new CreatureAI(Manager, "Dwarf AI", Sensors, PlanService));
         
            Attacks = new List<Attack>() { new Attack(Stats.CurrentClass.Attacks[0]) };

            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));


            Physics.Tags.Add("Dwarf");

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10, 
                SoundToPlay = ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
            });

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Physics.AddChild(Shadow.Create(0.75f, Manager));

            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_hurt_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_hurt_2,
            };

            NoiseMaker.Noises["Ok"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_3
            };

            NoiseMaker.Noises["Die"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_death
            };

            NoiseMaker.Noises["Pleased"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pleased
            };

            NoiseMaker.Noises["Tantrum"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_3,
            };
            NoiseMaker.Noises["Jump"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_jump
            };

            NoiseMaker.Noises["Climb"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_3
            };

            MinimapIcon minimapIcon = Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 0, 0))) as MinimapIcon;

            Stats.FullName = TextGenerator.GenerateRandom("$firstname", " ", "$lastname");
            Stats.Size = 5;
            Stats.CanSleep = true;
            Stats.CanEat = true;
            AI.Movement.CanClimbWalls = true; // Why isn't this a flag like the below?
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.EnterVehicle, true);
            AI.Movement.SetCan(MoveType.ExitVehicle, true);
            AI.Movement.SetCan(MoveType.RideVehicle, true);
            AI.Movement.SetCost(MoveType.EnterVehicle, 1.0f);
            AI.Movement.SetCost(MoveType.ExitVehicle, 1.0f);
            AI.Movement.SetCost(MoveType.RideVehicle, 0.01f);
            AI.Movement.SetSpeed(MoveType.RideVehicle, 3.0f);
            AI.Movement.SetSpeed(MoveType.EnterVehicle, 1.0f);
            AI.Movement.SetSpeed(MoveType.ExitVehicle, 1.0f);
            AI.TriggersMourning = true;
            AI.Biography = Applicant.GenerateBiography(AI.Stats.FullName, Stats.Gender);
            Species = "Dwarf";


            Physics.AddChild(new VoxelRevealer(Manager, Physics, 5)).SetFlag(Flag.ShouldSerialize, false);

            Physics.AddChild(new DwarfThoughts(Manager, "Thoughts"));
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateDwarfSprite(Stats.CurrentClass, manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new VoxelRevealer(manager, Physics, 5)).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        protected void CreateDwarfSprite(EmployeeClass employeeClass, ComponentManager manager)
        {
            if (Physics == null)
            {
                Physics = GetRoot().GetComponent<Physics>();
                if (Physics == null) return;
            }

            var sprite = Physics.AddChild(new LayeredSprites.LayeredCharacterSprite(manager, "Sprite", Matrix.CreateTranslation(new Vector3(0, 0.15f, 0)))) as LayeredSprites.LayeredCharacterSprite;

            var random = new Random(Stats.RandomSeed);

            var hairPalette = LayeredSprites.LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("hair")).SelectRandom(random);
            var skinPalette = LayeredSprites.LayerLibrary.EnumeratePalettes().Where(p => p.Layer.Contains("face")).SelectRandom(random);
            AddLayerOrDefault(sprite, random, "body");
            AddLayerOrDefault(sprite, random, "face", skinPalette);
            AddLayerOrDefault(sprite, random, "nose", skinPalette);
            AddLayerOrDefault(sprite, random, "beard", hairPalette);
            AddLayerOrDefault(sprite, random, "hair", hairPalette);
            AddLayerOrDefault(sprite, random, "tool");

            foreach (Animation animation in AnimationLibrary.LoadNewLayeredAnimationFormat(ContentPaths.dwarf_animations))
                sprite.AddAnimation(animation);

            sprite.SetCurrentAnimation(Sprite.Animations.First().Value);
            sprite.SetFlag(Flag.ShouldSerialize, false);

            
        }

        private void AddLayerOrDefault(LayeredSprites.LayeredCharacterSprite Sprite, Random Random, String Layer, LayeredSprites.Palette Palette = null)
        {
            var layers = LayeredSprites.LayerLibrary.EnumerateLayers(Layer).Where(l => !l.DefaultLayer && l.PassesFilter(this.Stats));
            if (layers.Count() > 0)
                Sprite.AddLayer(layers.SelectRandom(Random), Palette);
            else
            {
                var defaultLayer = LayeredSprites.LayerLibrary.EnumerateLayers(Layer).Where(l => l.DefaultLayer).FirstOrDefault();
                if (defaultLayer != null)
                    Sprite.AddLayer(defaultLayer, Palette);
            }
        }
    }

}
