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
        public static SpellTree CreateSpellTree()
        {
            Texture2D icons = TextureManager.GetTexture(ContentPaths.GUI.icons);

            
            SpellTree toReturn =  new SpellTree()
            {
                RootSpells = new List<SpellTree.Node>()
                {
                    new SpellTree.Node()
                    {
                        Spell = new InspectSpell(InspectSpell.InspectType.InspectEntity),
                        ResearchProgress = 10.0f,
                        ResearchTime = 10.0f,
                        Children = new List<SpellTree.Node>()
                        {
                            new SpellTree.Node()
                            {
                                Spell = new BuffSpell(
                                    new Creature.StatBuff(30.0f, new CreatureStats.StatNums()
                                    {
                                        Dexterity = 2.0f,
                                        Strength = 2.0f,
                                        Wisdom = 2.0f,
                                        Intelligence = 2.0f,
                                        Charisma = 0.0f,
                                        Constitution = 0.0f,
                                        Size = 0.0f
                                    }) 
                                    )
                                {
                                    Name = "Minor Focus",
                                    Description = "Makes the selected creatures work harder for 30 seconds (+2 to DEX, STR, INT and WIS)",
                                    Hint = "Click and drag to select creatures"
                                },
                                ResearchProgress = 30.0f,
                                ResearchTime = 30.0f,

                                Children = new List<SpellTree.Node>()
                                {
                                      new SpellTree.Node()
                                      {
                                            Spell = new BuffSpell(
                                                new Creature.StatBuff(60.0f, new CreatureStats.StatNums()
                                                {
                                                    Dexterity = 5.0f,
                                                    Strength = 5.0f,
                                                    Wisdom = 5.0f,
                                                    Intelligence = 5.0f,
                                                    Charisma = 0.0f,
                                                    Constitution = 0.0f,
                                                    Size = 0.0f
                                                })
                                                )
                                            {
                                                Name = "Major Focus",
                                                Description = "Makes the selected creatures work harder for 60 seconds (+5 to DEX, STR, INT and WIS)",
                                                Hint = "Click and drag to select creatures"
                                            },
                                            Children = new List<SpellTree.Node>()
                                            {
                                                new SpellTree.Node()
                                                {
                                                    Spell = new CreateEntitySpell("Fairy", false)
                                                    {
                                                        Name = "Magic Helper",
                                                        Description = "Creates a magical helper employee who persists for 30 seconds",
                                                        Hint = "Click to spawn a helper"
                                                    },
                                                    ResearchProgress = 150.0f,
                                                    ResearchTime = 150.0f
                                                }
                                            },
                                            ResearchProgress = 60.0f,
                                            ResearchTime = 60.0f,
                                        },
                                        new SpellTree.Node()
                                        {
                                            Spell = new BuffSpell(new Creature.ThoughtBuff(30.0f, Thought.ThoughtType.Magic))
                                            {
                                                Name = "Minor Happiness",
                                                Description = "Makes the selected creatures happy for 30 seconds.",
                                                Hint = "Click and drag to select creatures"
                                            },
                                            ResearchTime = 60.0f,
                                            ResearchProgress = 60.0f,
                                            Children = new List<SpellTree.Node>()
                                            {
                                                new SpellTree.Node()
                                                {
                                                    Spell = new BuffSpell(new Creature.ThoughtBuff(60.0f, Thought.ThoughtType.Magic))
                                                    {
                                                        Name = "Major Happiness",
                                                        Description = "Makes the selected creatures happy for 60 seconds.",
                                                        Hint = "Click and drag to select creatures"
                                                    },
                                                    ResearchTime = 120.0f,
                                                    ResearchProgress = 120.0f
                                                }
                                            }
                                        }
                                }
                            }
                        }
                    },
                    new SpellTree.Node()
                    {
                        Spell = new InspectSpell(InspectSpell.InspectType.InspectBlock),
                        ResearchProgress = 25.0f,
                        ResearchTime = 25.0f,
                        Children = new List<SpellTree.Node>()
                        {
                            new SpellTree.Node()
                            {
                                Spell = new PlaceBlockSpell("Dirt", false),
                                ResearchProgress = 50.0f,
                                ResearchTime = 50.0f,
                                
                                Children = new List<SpellTree.Node>()
                                {
                                    new SpellTree.Node()
                                    {
                                        ResearchProgress = 100.0f,
                                        ResearchTime = 100.0f,
                                        Spell = new PlaceBlockSpell("Stone", false),

                                        Children = new List<SpellTree.Node>()
                                        {
                                            new SpellTree.Node()
                                            {
                                                ResearchProgress = 150.0f,
                                                ResearchTime = 150.0f,
                                                Spell = new DestroyBlockSpell()
                                            }
                                        }
                                    }
                                }
                            },
                            new SpellTree.Node()
                            {
                                Spell = new PlaceBlockSpell("Magic", false),
                                ResearchProgress = 50.0f,
                                ResearchTime = 50.0f,

                                Children = new List<SpellTree.Node>()
                                {
                                    new SpellTree.Node()
                                    {
                                        ResearchProgress = 100.0f,
                                        ResearchTime = 100.0f,
                                        Spell = new PlaceBlockSpell("Iron", true),
                                        Children = new List<SpellTree.Node>()
                                        {
                                            new SpellTree.Node()
                                            {
                                                ResearchProgress = 150.0f,
                                                ResearchTime = 150.0f,
                                                Spell = new PlaceBlockSpell("Gold", true)
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    },
                    new SpellTree.Node()
                    {
                        Spell = new BuffSpell(new Creature.OngoingHealBuff(2, 10))
                        {
                            Name = "Minor Heal",
                            Description = "Heals 2 damage per second for 10 seconds",
                            Hint = "Click and drag to select creatures"
                        },
                        ResearchProgress = 30.0f,
                        ResearchTime = 30.0f,
                        Children = new List<SpellTree.Node>()
                        {
                            new SpellTree.Node()
                            {
                                Spell = new BuffSpell(new Creature.OngoingHealBuff(5, 10))
                                {
                                    Name = "Major Heal",
                                    Description = "Heals 5 damage per second for 10 seconds",
                                    Hint = "Click and drag to select creatures"
                                }
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

        

        public static SpellTree MakeFakeTree()
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
                    Spell = new InspectSpell(val > 0.5f ?  InspectSpell.InspectType.InspectBlock : InspectSpell.InspectType.InspectEntity)
                };
                toReturn.RootSpells.Add(root);
                MakeFakeSubtree(root);
            }

            return toReturn;
        }

        public static void MakeFakeSubtree(SpellTree.Node node)
        {
            int numChildren = (int)MathFunctions.Rand(-2, 4);
            for (int j = 0; j < numChildren; j++)
            {
                SpellTree.Node newNode = new SpellTree.Node()
                {
                    ResearchProgress = MathFunctions.Rand(0, 101.0f),
                    ResearchTime = 100.0f,
                    Spell = new DestroyBlockSpell()
                };
                node.Children.Add(newNode);
                MakeFakeSubtree(newNode);
            }
        }
    }
}
