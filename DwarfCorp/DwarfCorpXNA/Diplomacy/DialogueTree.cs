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
            Context.Say(String.Format("{0} I am {1} of {2}.",
                    Datastructures.SelectRandom(Context.Envoy.OwnerFaction.Race.Speech.Greetings),
                    Context.EnvoyName,
                    Context.Envoy.OwnerFaction.Name));
            Context.ClearOptions();
            Context.AddOption("Trade", Trade);
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

        public static void Trade(DialogueContext Context)
        {
            Context.TradePanel = Context.Panel.Root.ConstructWidget(new NewGui.TradePanel
            {
                Rect = Context.Panel.Root.VirtualScreen,
                Envoy = new Trade.EnvoyTradeEntity(Context.Envoy),
                Player = new Trade.PlayerTradeEntity(Context.PlayerFaction)
            }) as NewGui.TradePanel;

            Context.TradePanel.Layout();
            Context.Panel.Root.ShowDialog(Context.TradePanel);

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
                Context.TradePanel.Transaction.Apply();
            Context.Transition(ConversationRoot);
        }
    }
}