using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    // Todo: Split file.
    public class Potion
    {
        public string Description;
        public Buff Effects;
        public List<Quantitiy<Resource.ResourceTags>> Ingredients;
        public int Icon;
        public Potion()
        {

        }

        public void Drink(Creature creature)
        {
            creature.AddBuff(Effects.Clone());
        }

        public bool ShouldDrink(Creature creature)
        {
            if (Effects == null)
            {
                return false;
            }
            return Effects.IsRelevant(creature);
        }
    }

    public class GatherPotionsTask : Task
    {
        public GatherPotionsTask()
        {
            Name = "Gather Potions";
            ReassignOnDeath = false;
            AutoRetry = false;
            Priority = PriorityType.Medium;
        }

        public override float ComputeCost(Creature agent, bool alreadyCheckedFeasible = false)
        {
            return 1.0f;
        }

        public override bool ShouldRetry(Creature agent)
        {
            return false;
        }

        public override Feasibility IsFeasible(Creature agent)
        {
            return agent.Faction.ListResourcesWithTag(Resource.ResourceTags.Potion).Count > 0 ? Feasibility.Feasible : Feasibility.Infeasible;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GetResourcesAct(agent.AI, new List<Quantitiy<Resource.ResourceTags>>() { new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Potion)});
        }
    }

    public static class PotionLibrary
    {
        // Todo: Jsonify!
        public static Dictionary<string, Potion> Potions = new Dictionary<string, Potion>()
        {
            {
                "Health Potion",
                new Potion()
                {
                    Ingredients = new List<Quantitiy<Resource.ResourceTags>>()
                    {
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 1),
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Edible, 1)
                    },
                    Description = "Dwarfs drink this potion when they take damage.",
                    Effects = new OngoingHealBuff(10.0f, 4.0f) { Particles = "star_particle" },
                    Icon = 44,
                }
            },
            {
                "Panacea Potion",
                new Potion()
                {
                    Ingredients = new List<Quantitiy<Resource.ResourceTags>>()
                    {
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 1),
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.AnimalProduct, 1)
                    },
                    Description = "Dwarfs drink this potion to cure all disease.",
                    Effects = new CureDiseaseBuff(),
                    Icon = 44
                }
            },
            {
                "Happiness Potion",
                new Potion()
                {
                    Ingredients = new List<Quantitiy<Resource.ResourceTags>>()
                    {
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 1),
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Alcohol, 1)
                    },
                    Description = "Dwarfs drink this potion when they are feeling down.",
                    Effects = new ThoughtBuff(120.0f, Thought.ThoughtType.Magic) {  Particles = "star_particle" },
                    Icon = 45
                }
            },
            {
                "Potion of Strength",
                new Potion()
                {
                    Ingredients = new List<Quantitiy<Resource.ResourceTags>>()
                    {
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 1),
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Bone, 1)
                    },
                    Description = "Dwarfs drink this potion to get strong.",
                    Effects = new StatBuff(120.0f, new StatAdjustment() { Name = "potion of strength", Strength = 3 })
                    {
                        Particles = "star_particle"
                    },
                    Icon = 44
                }
            },
            {
                "Potion of Speed",
                new Potion()
                {
                    Ingredients = new List<Quantitiy<Resource.ResourceTags>>()
                    {
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 1),
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Fungus, 1)
                    },
                    Description = "Dwarfs drink this potion to get a bit faster.",
                    Effects = new StatBuff(120.0f, new StatAdjustment() { Name = "potion of speed", Dexterity = 3 }) { Particles = "star_particle" },
                    Icon = 45
                }
            },
            {
                "Potion of Smarts",
                new Potion()
                {
                    Ingredients = new List<Quantitiy<Resource.ResourceTags>>()
                    {
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Magical, 1),
                        new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Fruit, 1)
                    },
                    Description = "Dwarfs drink this potion to get a cognitive boost.",
                    Effects = new StatBuff(120.0f, new StatAdjustment() { Name = "potion of smarts", Intelligence = 3 }) { Particles = "star_particle" },
                    Icon = 45
                }
            },
        };

        public static void Initialize()
        {
            foreach(var potion in Potions)
            {
                Resource resource = new Resource();

                {
                    resource.Name = potion.Key;
                    resource.PotionType = potion.Key;
                    resource.CraftPrerequisites = potion.Value.Ingredients;
                    resource.CraftInfo = new Resource.CraftItemInfo()
                    {
                        CraftItemType = potion.Key
                    };
                    resource.CanCraft = true;
                    resource.MoneyValue = 100;
                    resource.Description = potion.Value.Description;
                    resource.ShortName = potion.Key;
                    resource.Tags = new List<Resource.ResourceTags>()
                    {
                        Resource.ResourceTags.Potion
                    };
                    resource.GuiLayers = new List<Gui.TileReference>()
                    {
                        new Gui.TileReference("resources", potion.Value.Icon)
                    };
                    resource.CompositeLayers = new List<Resource.CompositeLayer>()
                    {
                        new Resource.CompositeLayer()
                        {
                            Asset = ContentPaths.Entities.Resources.resources,
                            Frame = new Microsoft.Xna.Framework.Point(potion.Value.Icon % 8, potion.Value.Icon / 8),
                            FrameSize = new Microsoft.Xna.Framework.Point(32, 32)
                        }
                    };
                    resource.Tint = Microsoft.Xna.Framework.Color.White;

                };
                ResourceLibrary.Add(resource);

                CraftItem craftItem = new CraftItem()
                {
                    CraftLocation = "Apothecary",
                    Icon = new Gui.TileReference("resources", potion.Value.Icon),
                    Category = "Potions",
                    Name = potion.Key,
                    DisplayName = potion.Key,
                    AllowHeterogenous = true,
                    IsMagical = true,
                    Type = CraftItem.CraftType.Resource,
                    Verb = StringLibrary.GetString("brew"),
                    PastTeseVerb = StringLibrary.GetString("brewed"),
                    CurrentVerb = StringLibrary.GetString("brewing"),
                    ResourceCreated = potion.Key,
                    Description = potion.Value.Description,
                    RequiredResources = potion.Value.Ingredients,
                    BaseCraftTime = 10.0f,
                };
                CraftLibrary.Add(craftItem);
            }
        }
    }

}
