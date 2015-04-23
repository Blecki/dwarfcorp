using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FairyClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Fairy",
                    Pay = 0,
                    XP = 0,
                   
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Constitution = 0.0f
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions = new List<GameMaster.ToolMode>()
            {
                GameMaster.ToolMode.Gather,
                GameMaster.ToolMode.Dig,
                GameMaster.ToolMode.Craft,
                GameMaster.ToolMode.Farm,
                GameMaster.ToolMode.Chop
            };
        }

        void InitializeAnimations()
        {
            CompositeAnimation.Descriptor descriptor =
    FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
        ContentPaths.GetFileAsString(ContentPaths.Entities.Dwarf.Sprites.fairy_animation));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations(CompositeLibrary.Dwarf));
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Fairy Dust", 10.0f, 0.2f, 5.0f, ContentPaths.Audio.tinkle, ContentPaths.Effects.rings)
                {
                    Knockback = 0.5f,
                    HitParticles = "star_particle"
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Fairy Helper";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
        public FairyClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }
}
