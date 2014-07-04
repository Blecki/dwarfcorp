using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class AxeDwarfClass : EmployeeClass
    {
        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Thug",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 6
                    }

                },
                new Level
                {
                    Index = 1,
                    Name = "Sellsword",
                    Pay = 50,
                    XP = 100,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 7,
                        Constitution = 6,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 2,
                    Name = "Private",
                    Pay = 100,
                    XP = 250,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 7,
                        Constitution = 7,
                        Charisma = 6
                    }
                },
                new Level
                {
                    Index = 3,
                    Name = "Corporal",
                    Pay = 200,
                    XP = 500,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 7,
                        Constitution = 7,
                        Charisma = 6,
                        Dexterity = 6
                    }
                },
                new Level
                {
                    Index = 4,
                    Name = "Sergant",
                    Pay = 500,
                    XP = 1000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 8,
                        Constitution = 7,
                        Charisma = 6,
                        Dexterity = 6
                    }
                },
                new Level
                {
                    Index = 5,
                    Name = "Master Sergant",
                    Pay = 1000,
                    XP = 5000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 9,
                        Constitution = 8,
                        Charisma = 7,
                        Dexterity = 7
                    }
                },
                new Level
                {
                    Index = 6,
                    Name = "Lieutenant",
                    Pay = 5000,
                    XP = 10000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 8,
                        Charisma = 8,
                        Dexterity = 8
                    }
                },
                new Level
                {
                    Index = 7,
                    Name = "Major",
                    Pay = 10000,
                    XP = 20000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 9,
                        Charisma = 9,
                        Dexterity = 9,
                        Intelligence = 6
                    }

                },
                new Level
                {
                    Index = 8,
                    Name = "Colonel",
                    Pay = 50000,
                    XP = 1000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Intelligence = 6
                    }
                },
                new Level
                {
                    Index = 9,
                    Name = "General",
                    Pay = 100000,
                    XP = 2000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Intelligence = 10
                    }
                },
                new Level
                {
                    Index = 10,
                    Name = "Commander in Chief",
                    Pay = 100000,
                    XP = 5000000,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Strength = 10,
                        Constitution = 10,
                        Charisma = 10,
                        Dexterity = 10,
                        Intelligence = 10
                    }
                }
            };
        }

        void InitializeAnimations()
        {
            const int frameWidth = 48;
            const int frameHeight = 40;
            Texture2D dwarfSprites = TextureManager.GetTexture(ContentPaths.Entities.Dwarf.Sprites.soldier_axe_shield);

            Animations = new List<Animation>()
            {
                
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Walking, OrientedAnimation.Orientation.Forward, dwarfSprites, 15.0f, frameWidth, frameHeight, 0, 0, 1, 2, 1, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Walking, OrientedAnimation.Orientation.Right, dwarfSprites, 15.0f, frameWidth, frameHeight, 2, 0, 1, 2, 1, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Walking, OrientedAnimation.Orientation.Left, dwarfSprites, 15.0f, frameWidth, frameHeight, 1, 0, 1, 2, 1, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Walking, OrientedAnimation.Orientation.Backward, dwarfSprites, 15.0f, frameWidth, frameHeight, 3, 0, 1, 2, 1),

            CharacterSprite.CreateAnimation(Creature.CharacterMode.Idle, OrientedAnimation.Orientation.Forward, dwarfSprites, 0.8f, frameWidth, frameHeight, 0, 1, 3, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Idle, OrientedAnimation.Orientation.Right, dwarfSprites, 0.8f, frameWidth, frameHeight, 2, 2, 0, 2),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Idle, OrientedAnimation.Orientation.Left, dwarfSprites, 0.8f, frameWidth, frameHeight, 1, 1, 3, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Idle, OrientedAnimation.Orientation.Backward, dwarfSprites, 0.8f, frameWidth, frameHeight, 3, 1),

            CharacterSprite.CreateAnimation(Creature.CharacterMode.Attacking, OrientedAnimation.Orientation.Forward, dwarfSprites, 8.0f, frameWidth, frameHeight, 8, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Attacking, OrientedAnimation.Orientation.Right, dwarfSprites, 8.0f, frameWidth, frameHeight, 10, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Attacking, OrientedAnimation.Orientation.Left, dwarfSprites, 8.0f, frameWidth, frameHeight, 9, 0, 1, 2, 3),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Attacking, OrientedAnimation.Orientation.Backward, dwarfSprites, 8.0f, frameWidth, frameHeight, 11, 0, 1, 2, 3),

            CharacterSprite.CreateAnimation(Creature.CharacterMode.Falling, OrientedAnimation.Orientation.Forward, dwarfSprites, 15.0f, frameWidth, frameHeight, 4, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Falling, OrientedAnimation.Orientation.Right, dwarfSprites, 15.0f, frameWidth, frameHeight, 6, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Falling, OrientedAnimation.Orientation.Left, dwarfSprites, 15.0f, frameWidth, frameHeight, 5, 1),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Falling, OrientedAnimation.Orientation.Backward, dwarfSprites, 15.0f, frameWidth, frameHeight, 7, 1),

            CharacterSprite.CreateAnimation(Creature.CharacterMode.Jumping, OrientedAnimation.Orientation.Forward, dwarfSprites, 15.0f, frameWidth, frameHeight, 4, 0),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Jumping, OrientedAnimation.Orientation.Right, dwarfSprites, 15.0f, frameWidth, frameHeight, 6, 0),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Jumping, OrientedAnimation.Orientation.Left, dwarfSprites, 15.0f, frameWidth, frameHeight, 5, 0),
            CharacterSprite.CreateAnimation(Creature.CharacterMode.Jumping, OrientedAnimation.Orientation.Backward, dwarfSprites, 15.0f, frameWidth, frameHeight, 7, 0),

            };
        }

        void InitializeActions()
        {
            Actions = new List<GameMaster.ToolMode>()
            {
                GameMaster.ToolMode.Chop,
                GameMaster.ToolMode.Gather,
                GameMaster.ToolMode.Guard,
                GameMaster.ToolMode.Attack
            };
        }

        public void InitializeWeapons()
        {
            MeleeAttack = new Attack("Axe", 4.0f, 1.0f, 1.0f, ContentPaths.Audio.sword) {Knockback = 10.0f, 
                HitAnimation = new Animation(ContentPaths.Effects.slice, 32, 32, 0, 1, 2, 3)};
        }

        protected override sealed void InitializeStatics()
        {
            Name = "AxeDwarf";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }


        public AxeDwarfClass()
        {
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }
    }
}
