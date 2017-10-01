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
                    Context.AddOption("What is your opinion of us?", (context) =>
                    {
                        var prompt = "";
                        if (context.Politics.RecentEvents.Count > 0)
                        {
                            prompt = String.Format("So far, our relationship has been {0}",
                                context.Politics.GetCurrentRelationship());
                            if (context.Politics.RecentEvents.Count > 0)
                            {
                                prompt += ", because ";
                                prompt +=
                                    TextGenerator.GetListString(
                                        context.Politics.RecentEvents.Select(e => e.Description).ToList());
                            }
                            prompt += ".";
                        }
                        else
                        {
                            prompt = "We know nothing about you.";
                        }
                        Context.Transition(RootWithPrompt(prompt));
                    });
                    Context.AddOption("What is something you have many of?", (context) =>
                    {
                        Context.Transition(RootWithPrompt(String.Format("We have many {0}.",
                            GetPluralForm(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.CommonResources)))));
                    });
                    Context.AddOption("What is something you have few of?", (context) =>
                    {
                        if (context.Envoy.OwnerFaction.Race.RareResources.Count > 0)
                        {
                            Context.Transition(RootWithPrompt(String.Format("We have few {0}.",
                                GetPluralForm(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.RareResources)))));
                        }
                        else
                        {
                            Context.Transition(RootWithPrompt("Nothing in particular."));
                        }
                    });
                    Context.AddOption("What is something you hate?", (context) =>
                    {
                        if (context.Envoy.OwnerFaction.Race.HatedResources.Count > 0)
                        {
                            Context.Transition(RootWithPrompt(String.Format("We hate {0}.",
                                GetPluralForm(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.HatedResources)))));
                        }
                        else
                        {
                            Context.Transition(RootWithPrompt("We don't hate anything in particular."));   
                        }
                    });
                    Context.AddOption("What is something you like?", (context) =>
                    {
                        if (context.Envoy.OwnerFaction.Race.LikedResources.Count > 0)
                        {
                            Context.Transition(RootWithPrompt(String.Format("We like {0}.",
                                GetPluralForm(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.LikedResources)))));
                        }
                        else
                        {
                            Context.Transition(RootWithPrompt("We don't like anything in particular."));
                        }
                    });
                    Context.AddOption("Declare war", ConfirmDeclareWar);
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

        public static void ConfirmDeclareWar(DialogueContext Context)
        {
            Context.ClearOptions();
            Context.Say("You really want to declare war on us?");
            Context.AddOption("Yes!", DeclareWar);
            Context.AddOption("No.", ConversationRoot);
        }

        public static void DeclareWar(DialogueContext Context)
        {
            Context.Envoy.OwnerFaction.Race.Speech.Language.SayBoo();
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

            Context.World.GoalManager.OnGameEvent(new Goals.Events.DeclareWar
            {
                PlayerFaction = Context.PlayerFaction,
                OtherFaction = Context.Envoy.OwnerFaction
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
            Context.TradePanel = Context.ChoicePanel.Root.ConstructWidget(new Gui.Widgets.TradePanel
            {
                Rect = Context.ChoicePanel.Root.RenderData.VirtualScreen,
                Envoy = new Trade.EnvoyTradeEntity(Context.Envoy),
                Player = new Trade.PlayerTradeEntity(Context.PlayerFaction)
            }) as Gui.Widgets.TradePanel;

            Context.TradePanel.Layout();
            Context.ChoicePanel.Root.ShowDialog(Context.TradePanel);

            Context.Transition(WaitForTradeToFinish);
        }

        public static void WaitForTradeToFinish(DialogueContext Context)
        {
            if (Context.TradePanel.Result == Gui.Widgets.TradeDialogResult.Pending)
                Context.Transition(WaitForTradeToFinish);
            else
                Context.Transition(ProcessTrade);
        }

        public static void ProcessTrade(DialogueContext Context)
        {
            if (Context.TradePanel.Result == Gui.Widgets.TradeDialogResult.Propose)
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
                    Context.Envoy.OwnerFaction.Race.Speech.Language.SayBoo();
                    Context.Transition(GoodbyeWithPrompt(Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.BadTrades)));

                    if (!Context.Politics.HasEvent("you tried to give us something offensive"))
                    {
                        Context.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                        {
                            Change = -0.25f,
                            Description = "you tried to give us something offensive",
                            Duration = new TimeSpan(4, 0, 0, 0),
                            Time = Context.World.Time.CurrentDate
                        });
                    }
                }
                else
                {
                    if (containsLikedItem)
                    {
                        if (!Context.Politics.HasEvent("you gave us something we liked"))
                        {
                            Context.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                            {
                                Change = 0.25f,
                                Description = "you gave us something we liked",
                                Duration = new TimeSpan(4, 0, 0, 0),
                                Time = Context.World.Time.CurrentDate
                            });
                        }
                    }

                    Context.TradePanel.Transaction.Apply(Context.World);
                    Context.Transition(RootWithPrompt(Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.GoodTrades)));

                    Context.World.GoalManager.OnGameEvent(new Goals.Events.Trade
                    {
                        PlayerFaction = Context.PlayerFaction,
                        PlayerGold = Context.TradePanel.Transaction.PlayerMoney,
                        PlayerGoods = Context.TradePanel.Transaction.PlayerItems,
                        OtherFaction = Context.Envoy.OwnerFaction,
                        OtherGold = Context.TradePanel.Transaction.EnvoyMoney,
                        OtherGoods = Context.TradePanel.Transaction.EnvoyItems
                    });

                    if (!Context.Politics.HasEvent("we had profitable trade"))
                    {
                        Context.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                        {
                            Change = 0.25f,
                            Description = "we had profitable trade",
                            Duration = new TimeSpan(2, 0, 0, 0),
                            Time = Context.World.Time.CurrentDate
                        });
                    }
                    Context.Envoy.OwnerFaction.Race.Speech.Language.SayYay();
                }
            } 
            else if (Context.TradePanel.Result == Gui.Widgets.TradeDialogResult.Reject)
            {
                Context.Envoy.OwnerFaction.Race.Speech.Language.SayBoo();
                Context.Transition(RootWithPrompt(Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.BadTrades)));
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
                PluralMap = new Dictionary<Resource.ResourceTags, string>
                {
                    {Resource.ResourceTags.Edible, "edibles"},
                    {Resource.ResourceTags.Material, "materials"},
                    {Resource.ResourceTags.HardMaterial, "hard materials"},
                    {Resource.ResourceTags.Precious, "precious things"},
                    {Resource.ResourceTags.Flammable, "flammable things"},
                    {Resource.ResourceTags.SelfIlluminating, "self illuminating things"},
                    {Resource.ResourceTags.Wood, "wooden things"},
                    {Resource.ResourceTags.Metal, "metal things"},
                    {Resource.ResourceTags.Stone, "stone things"},
                    {Resource.ResourceTags.Fuel, "fuel"},
                    {Resource.ResourceTags.Magical, "magical items"},
                    {Resource.ResourceTags.Soil, "soil"},
                    {Resource.ResourceTags.Grain, "grains"},
                    {Resource.ResourceTags.Fungus, "fungi"},
                    {Resource.ResourceTags.None, "WUT"},
                    {Resource.ResourceTags.AnimalProduct, "animal products"},
                    {Resource.ResourceTags.Meat, "meats"},
                    {Resource.ResourceTags.Gem, "gems"},
                    {Resource.ResourceTags.Craft, "crafts"},
                    {Resource.ResourceTags.Encrustable, "encrustable items"},
                    {Resource.ResourceTags.Alcohol, "alcoholic drinks"},
                    {Resource.ResourceTags.Brewable, "brewed drinks"},
                    {Resource.ResourceTags.Bakeable, "baked goods"},
                    {Resource.ResourceTags.RawFood, "raw foods"},
                    {Resource.ResourceTags.PreparedFood, "prepared foods"},
                    {Resource.ResourceTags.Plantable, "seeds"},
                    {Resource.ResourceTags.AboveGroundPlant, "plants"},
                    {Resource.ResourceTags.BelowGroundPlant, "cave plants"},
                    {Resource.ResourceTags.Bone, "bones"}
                };
            }

            return PluralMap[Tag];
        }
    }
}