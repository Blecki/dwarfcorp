using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.Dialogue
{
    public class DialogueTree
    {
        public static void ConversationRoot(DialogueContext Context)
        {
            RootWithPrompt(String.Format("{0} I am {1} of {2}.",
                        Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.Greetings),
                        Context.EnvoyName,
                        Context.Envoy.OwnerFaction.Name))(Context);
        }

        public static Action<DialogueContext> RootWithPrompt(String Prompt)
        {
            return (Context) =>
            {
                if (Context.Politics.WasAtWar)
                {
                    Context.Say("We are at war.");
                    Context.AddOption("Make peace", MakePeace);
                    Context.AddOption("Continue the war", DeclareWar);
                }
                else
                {
                    Context.Say(Prompt);
                    Context.ClearOptions();
                    Context.AddOption("Trade", Trade);
                    Context.AddOption("Declare war", DeclareWar);
                    Context.AddOption("What is your opinion of us?", (context) =>
                    {
                        var prompt = String.Format("So far, our relationship has been {0}", context.Politics.GetCurrentRelationship());
                        if (context.Politics.RecentEvents.Count > 0)
                        {
                            prompt += ", because ";
                            prompt += TextGenerator.GetListString(context.Politics.RecentEvents.Select(e => e.Description).ToList());
                        }
                        prompt += ".";
                        Context.Transition(RootWithPrompt(prompt));
                    });
                    Context.AddOption("What is something you have many of?", (context) =>
                    {
                        Context.Transition(RootWithPrompt(String.Format("We have many {0}.",
                            GetPluralForm(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.CommonResources)))));
                    });
                    Context.AddOption("What is something you have few of?", (context) =>
                    {
                        Context.Transition(RootWithPrompt(String.Format("We have few {0}.",
                            GetPluralForm(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.RareResources)))));
                    });
                    Context.AddOption("What is something you hate?", (context) =>
                    {
                        Context.Transition(RootWithPrompt(String.Format("We hate {0}.",
                            GetPluralForm(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.HatedResources)))));
                    });
                    Context.AddOption("What is something you like?", (context) =>
                    {
                        Context.Transition(RootWithPrompt(String.Format("We like {0}.",
                            GetPluralForm(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.LikedResources)))));
                    });
                    Context.AddOption("Leave", (context) =>
                     {
                         Context.Say(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.Speech.Farewells));
                         Context.ClearOptions();
                         Context.AddOption("Goodbye.", (_) =>
                         {
                             Diplomacy.RecallEnvoy(context.Envoy);
                             GameState.Game.StateManager.PopState();
                         });
                     });
                }
            };
        }

        public static void MakePeace(DialogueContext Context)
        {
            if (!Context.Politics.HasEvent("you made peace with us"))
            {
                Context.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                {
                    Change = 0.4f,
                    Description = "you made peace with us",
                    Duration = new TimeSpan(4, 0, 0, 0),
                    Time = Context.World.Time.CurrentDate
                });
            }

            ConversationRoot(Context);
        }

        public static void DeclareWar(DialogueContext Context)
        {
            if (!Context.Politics.HasEvent("you declared war on us"))
            {
                Context.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                {
                    Change = -2.0f,
                    Description = "you declared war on us",
                    Duration = new TimeSpan(4, 0, 0, 0),
                    Time = Context.World.Time.CurrentDate
                });
                Context.Politics.WasAtWar = true;
            }

            Context.Say(Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.WarDeclarations));
            Context.ClearOptions();

            Context.AddOption("Goodbye.", (context) =>
            {
                Diplomacy.RecallEnvoy(context.Envoy);
                GameState.Game.StateManager.PopState();
            });
        }

        public static Action<DialogueContext> GoodbyeWithPrompt(String Prompt)
        {
            return (context) =>
            {
                context.Say(Prompt);
                context.ClearOptions();
                context.AddOption("Goodbye.", (_) =>
                {
                    Diplomacy.RecallEnvoy(context.Envoy);
                    GameState.Game.StateManager.PopState();
                });
            };
        }

        public static void Trade(DialogueContext Context)
        {
            Context.TradePanel = Context.ChoicePanel.Root.ConstructWidget(new NewGui.TradePanel
            {
                Rect = Context.ChoicePanel.Root.VirtualScreen,
                Envoy = new Trade.EnvoyTradeEntity(Context.Envoy),
                Player = new Trade.PlayerTradeEntity(Context.PlayerFaction)
            }) as NewGui.TradePanel;

            Context.TradePanel.Layout();
            Context.ChoicePanel.Root.ShowDialog(Context.TradePanel);

            Context.Transition(WaitForTradeToFinish);
        }

        public static void WaitForTradeToFinish(DialogueContext Context)
        {
            if (Context.TradePanel.Result == NewGui.TradeDialogResult.Pending)
                Context.Transition(WaitForTradeToFinish);
            else
                Context.Transition(ProcessTrade);
        }

        public static void ProcessTrade(DialogueContext Context)
        {
            if (Context.TradePanel.Result == NewGui.TradeDialogResult.Propose)
            {
                var containsHatedItem = Context.TradePanel.Transaction.PlayerItems
                    .Select(item => ResourceLibrary.GetResourceByName(item.ResourceType))
                    .SelectMany(item => item.Tags)
                    .Any(tag => Context.Envoy.OwnerFaction.Race.HatedResources.Contains(tag));
                var containsLikedItem = Context.TradePanel.Transaction.PlayerItems
                    .Select(item => ResourceLibrary.GetResourceByName(item.ResourceType))
                    .SelectMany(item => item.Tags)
                    .Any(tag => Context.Envoy.OwnerFaction.Race.LikedResources.Contains(tag));

                if (containsHatedItem)
                {
                    Context.Transition(GoodbyeWithPrompt(Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.BadTrades)));

                    Context.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                    {
                        Change = -0.25f,
                        Description = "you tried to give us something offensive",
                        Duration = new TimeSpan(4, 0, 0, 0),
                        Time = Context.World.Time.CurrentDate
                    });
                }
                else
                {
                    if (containsLikedItem)
                    {
                        Context.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                        {
                            Change = 0.25f,
                            Description = "you gave us something we liked",
                            Duration = new TimeSpan(4, 0, 0, 0),
                            Time = Context.World.Time.CurrentDate
                        });
                    }

                    Context.TradePanel.Transaction.Apply();
                    Context.Transition(RootWithPrompt(Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.GoodTrades)));

                    Context.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                    {
                        Change = 0.25f,
                        Description = "we had profitable trade",
                        Duration = new TimeSpan(2, 0, 0, 0),
                        Time = Context.World.Time.CurrentDate
                    });
                }
            }
            else
            {
                Context.Transition(RootWithPrompt("Changed your mind?"));
            }
        }

        static Dictionary<Resource.ResourceTags, String> PluralMap = null;

        private static String GetPluralForm(Resource.ResourceTags Tag)
        {
            if (PluralMap == null)
            {
                PluralMap = new Dictionary<Resource.ResourceTags, string>();

                PluralMap.Add(Resource.ResourceTags.Edible, "edibles");
                PluralMap.Add(Resource.ResourceTags.Material, "materials");
                PluralMap.Add(Resource.ResourceTags.HardMaterial, "hard materials");
                PluralMap.Add(Resource.ResourceTags.Precious, "precious things");
                PluralMap.Add(Resource.ResourceTags.Flammable, "flammable things");
                PluralMap.Add(Resource.ResourceTags.SelfIlluminating, "self illuminating things");
                PluralMap.Add(Resource.ResourceTags.Wood, "wood");
                PluralMap.Add(Resource.ResourceTags.Metal, "metal");
                PluralMap.Add(Resource.ResourceTags.Stone, "stone");
                PluralMap.Add(Resource.ResourceTags.Fuel, "fuel");
                PluralMap.Add(Resource.ResourceTags.Magical, "magical items");
                PluralMap.Add(Resource.ResourceTags.Soil, "soil");
                PluralMap.Add(Resource.ResourceTags.Grain, "grain");
                PluralMap.Add(Resource.ResourceTags.Fungus, "fungi");
                PluralMap.Add(Resource.ResourceTags.None, "WUT");
                PluralMap.Add(Resource.ResourceTags.AnimalProduct, "animal products");
                PluralMap.Add(Resource.ResourceTags.Meat, "meat");
                PluralMap.Add(Resource.ResourceTags.Gem, "gems");
                PluralMap.Add(Resource.ResourceTags.Craft, "crafts");
                PluralMap.Add(Resource.ResourceTags.Encrustable, "encrustable items");
                PluralMap.Add(Resource.ResourceTags.Alcohol, "alcoholic drinks");
                PluralMap.Add(Resource.ResourceTags.Brewable, "brewed drinks");
                PluralMap.Add(Resource.ResourceTags.Bakeable, "baked goods");
                PluralMap.Add(Resource.ResourceTags.RawFood, "raw food");
                PluralMap.Add(Resource.ResourceTags.PreparedFood, "prepared food");
                PluralMap.Add(Resource.ResourceTags.Plantable, "seeds");
                PluralMap.Add(Resource.ResourceTags.AboveGroundPlant, "plants");
                PluralMap.Add(Resource.ResourceTags.BelowGroundPlant, "cave plants");
                PluralMap.Add(Resource.ResourceTags.Bone, "bones");
            }

            return PluralMap[Tag];
        }
    }
}