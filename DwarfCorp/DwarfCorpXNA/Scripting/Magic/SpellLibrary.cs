// SpellLibrary.cs
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
    public class SpellLibrary
    {
        public static SpellTree CreateSpellTree(WorldManager world)
        {
            Texture2D icons = TextureManager.GetTexture(ContentPaths.GUI.icons);

            
            SpellTree toReturn =  new SpellTree()
            {
                RootSpells = new List<SpellTree.Node>()
                {
                    new SpellTree.Node()
                    {
                        Spell = new InspectSpell(world,InspectSpell.InspectType.InspectEntity),
                        ResearchProgress = 10.0f,
                        ResearchTime = 10.0f,
                        Children = new List<SpellTree.Node>()
                        {
                            new SpellTree.Node()
                            {
                                Spell = new BuffSpell(world,
                                    new StatBuff(30.0f, new CreatureStats.StatNums()
                                    {
                                        Dexterity = 2.0f,
                                        Strength = 2.0f,
                                        Wisdom = 2.0f,
                                        Intelligence = 2.0f,
                                        Charisma = 0.0f,
                                        Constitution = 0.0f,
                                        Size = 0.0f
                                    }) {Particles = "star_particle", SoundOnStart = ContentPaths.Audio.powerup, SoundOnEnd = ContentPaths.Audio.wurp}
                                    )
                                {
                                    Name = "Minor Inspire",
                                    Description = "Makes the selected creatures work harder for 30 seconds (+2 to DEX, STR, INT and WIS)",
                                    Hint = "Click and drag to select creatures"
                                },
                                ResearchProgress = 0.0f,
                                ResearchTime = 30.0f,

                                Children = new List<SpellTree.Node>()
                                {
                                      new SpellTree.Node()
                                      {
                                            Spell = new BuffSpell(world,
                                                new StatBuff(60.0f, new CreatureStats.StatNums()
                                                {
                                                    Dexterity = 5.0f,
                                                    Strength = 5.0f,
                                                    Wisdom = 5.0f,
                                                    Intelligence = 5.0f,
                                                    Charisma = 0.0f,
                                                    Constitution = 0.0f,
                                                    Size = 0.0f
                                                }) {Particles = "star_particle", SoundOnStart = ContentPaths.Audio.powerup, SoundOnEnd = ContentPaths.Audio.wurp}
                                                )
                                            {
                                                Name = "Major Inspire",
                                                Description = "Makes the selected creatures work harder for 60 seconds (+5 to DEX, STR, INT and WIS)",
                                                Hint = "Click and drag to select creatures"
                                            },
                                            Children = new List<SpellTree.Node>()
                                            {
                                                new SpellTree.Node()
                                                {
                                                    Spell = new CreateEntitySpell(world, "Fairy", false)
                                                    {
                                                        Name = "Magic Helper",
                                                        Description = "Creates a magical helper employee who persists for 30 seconds",
                                                        Hint = "Click to spawn a helper"
                                                    },
                                                    ResearchProgress = 0.0f,
                                                    ResearchTime = 150.0f
                                                }
                                            },
                                            ResearchProgress = 0.0f,
                                            ResearchTime = 60.0f,
                                        },
                                        new SpellTree.Node()
                                        {
                                            Spell = new BuffSpell(world,
                                                new ThoughtBuff(30.0f, Thought.ThoughtType.Magic) {SoundOnStart = ContentPaths.Audio.powerup})
                                            {
                                                Name = "Minor Happiness",
                                                Description = "Makes the selected creatures happy for 30 seconds.",
                                                Hint = "Click and drag to select creatures",
                                                Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 5, 2),
                                                TileRef =  21
                                            },
                                            ResearchTime = 60.0f,
                                            ResearchProgress = 0.0f,
                                            Children = new List<SpellTree.Node>()
                                            {
                                                new SpellTree.Node()
                                                {
                                                    Spell = new BuffSpell(world,
                                                        new ThoughtBuff(60.0f, Thought.ThoughtType.Magic) {SoundOnStart = ContentPaths.Audio.powerup})
                                                    {
                                                        Name = "Major Happiness",
                                                        Description = "Makes the selected creatures happy for 60 seconds.",
                                                        Hint = "Click and drag to select creatures",
                                                        Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 5, 2),
                                                        TileRef = 21
                                                    },
                                                    ResearchTime = 120.0f,
                                                    ResearchProgress = 0.0f
                                                }
                                            }
                                        }
                                }
                            }
                        }
                    },
                    new SpellTree.Node()
                    {
                        Spell = new InspectSpell(world, InspectSpell.InspectType.InspectBlock),
                        ResearchProgress = 25.0f,
                        ResearchTime = 25.0f,
                        Children = new List<SpellTree.Node>()
                        {
                            new SpellTree.Node()
                            {
                                Spell = new PlaceBlockSpell(world, "Dirt", false),
                                ResearchProgress = 0.0f,
                                ResearchTime = 50.0f,
                                
                                Children = new List<SpellTree.Node>()
                                {
                                    new SpellTree.Node()
                                    {
                                        ResearchProgress = 0.0f,
                                        ResearchTime = 100.0f,
                                        Spell = new PlaceBlockSpell(world, "Stone", false),

                                        Children = new List<SpellTree.Node>()
                                        {
                                            new SpellTree.Node()
                                            {
                                                ResearchProgress = 0.0f,
                                                ResearchTime = 150.0f,
                                                Spell = new DestroyBlockSpell(world)
                                            }
                                        }
                                    }
                                }
                            },
                            new SpellTree.Node()
                            {
                                Spell = new PlaceBlockSpell(world, "Magic", false)
                                {
                                     Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 2, 3),
                                     Description = "Creates a temporary magic wall.",
                                     TileRef = 26
                                },
                                ResearchProgress = 0.0f,
                                ResearchTime = 50.0f,

                                Children = new List<SpellTree.Node>()
                                {
                                    new SpellTree.Node()
                                    {
                                        ResearchProgress = 0.0f,
                                        ResearchTime = 100.0f,
                                        Spell = new PlaceBlockSpell(world, "Iron", true),
                                        Children = new List<SpellTree.Node>()
                                        {
                                            new SpellTree.Node()
                                            {
                                                ResearchProgress = 0.0f,
                                                ResearchTime = 150.0f,
                                                Spell = new PlaceBlockSpell(world, "Gold", true)
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    },
                    new SpellTree.Node()
                    {
                        Spell = new BuffSpell(world, new OngoingHealBuff(2, 10) { Particles = "heart", SoundOnStart = ContentPaths.Audio.powerup, SoundOnEnd = ContentPaths.Audio.wurp}) 
                        {
                            Name = "Minor Heal",
                            Description = "Heals 2 damage per second for 10 seconds",
                            Hint = "Click and drag to select creatures",
                            Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 3, 2),
                            TileRef = 19
                        },
                        ResearchProgress = 0.0f,
                        ResearchTime = 30.0f,
                        Children = new List<SpellTree.Node>()
                        {
                            new SpellTree.Node()
                            {
                                Spell = new BuffSpell(world, new OngoingHealBuff(5, 10){Particles = "heart", SoundOnStart = ContentPaths.Audio.powerup, SoundOnEnd = ContentPaths.Audio.wurp})
                                {
                                    Name = "Major Heal",
                                    Description = "Heals 5 damage per second for 10 seconds",
                                    Hint = "Click and drag to select creatures",
                                    Image = new ImageFrame(TextureManager.GetTexture(ContentPaths.GUI.icons), 32, 3, 2),
                                    TileRef = 19
                                },
                                ResearchProgress = 0.0f,
                                ResearchTime = 150.0f
                            }
                        }
                    }

                }
            };
             

            foreach (SpellTree.Node spell in toReturn.RootSpells)
            {
                spell.SetupParentsRecursive();
            }

            return toReturn;
        }

        

        public static SpellTree MakeFakeTree(WorldManager world)
        {
            SpellTree toReturn = new SpellTree();

            int numRoots = (int) MathFunctions.Rand(3, 5);

            for (int i = 0; i < numRoots; i++)
            {
                float val = MathFunctions.Rand(0, 1.0f);
                SpellTree.Node root = new SpellTree.Node()
                {
                    ResearchProgress = MathFunctions.Rand(0, 101.0f),
                    ResearchTime = 100.0f,
                    Spell = new InspectSpell(world, val > 0.5f ?  InspectSpell.InspectType.InspectBlock : InspectSpell.InspectType.InspectEntity)
                };
                toReturn.RootSpells.Add(root);
                MakeFakeSubtree(world, root);
            }

            return toReturn;
        }

        public static void MakeFakeSubtree(WorldManager world, SpellTree.Node node)
        {
            int numChildren = (int)MathFunctions.Rand(-2, 4);
            for (int j = 0; j < numChildren; j++)
            {
                SpellTree.Node newNode = new SpellTree.Node()
                {
                    ResearchProgress = MathFunctions.Rand(0, 101.0f),
                    ResearchTime = 100.0f,
                    Spell = new DestroyBlockSpell(world)
                };
                node.Children.Add(newNode);
                MakeFakeSubtree(world, newNode);
            }
        }
    }
}
