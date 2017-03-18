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
            if (Context.Politics.WasAtWar)
            {
                Context.AddOption("Make peace", MakePeace);
                Context.AddOption("Continue the war", DeclareWar);
            }
            else
            {
                Context.Say(String.Format("{0} I am {1} of {2}.",
                        Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.Greetings),
                        Context.EnvoyName,
                        Context.Envoy.OwnerFaction.Name));
                Context.ClearOptions();
                Context.AddOption("Trade", Trade);
                Context.AddOption("Declare war", DeclareWar);
                Context.AddOption("Leave", (context) =>
                 {
                     Diplomacy.RecallEnvoy(context.Envoy);
                     Context.Say(Datastructures.SelectRandom(context.Envoy.OwnerFaction.Race.Speech.Farewells));
                     Context.ClearOptions();
                     Context.AddOption("Goodbye.", (_) =>
                     {
                         GameState.Game.StateManager.PopState();
                     });
                 });
            }
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
            Context.AddOption("Goodbye.", (_) =>
            {
                GameState.Game.StateManager.PopState();
            });
        }

        public static Action<DialogueContext> RootWithPrompt(String Prompt)
        {
            return (context) =>
            {
                context.Say(Prompt);
                context.ClearOptions();
                context.AddOption("Trade", Trade);
                context.AddOption("Leave", (_context) =>
                {
                    Diplomacy.RecallEnvoy(context.Envoy);
                    context.Say(Datastructures.SelectRandom(_context.Envoy.OwnerFaction.Race.Speech.Farewells));
                    context.ClearOptions();
                    context.AddOption("Goodbye.", (_) =>
                    {
                        GameState.Game.StateManager.PopState();
                    });
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
                Context.TradePanel.Transaction.Apply();
                Context.Transition(ConversationRoot);
            }
            else
            {
                Context.Transition(RootWithPrompt("Changed your mind?"));
            }
        }
    }
}