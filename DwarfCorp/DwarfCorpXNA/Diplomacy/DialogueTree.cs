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
                 Context.AddOption("Goodbye.", (_) => { /* Close out conversation. */ });
             });
        }

        public static void Trade(DialogueContext Context)
        {
            // Create trade dialouge.

            Context.Transition(WaitForTradeToFinish);
        }

        public static void WaitForTradeToFinish(DialogueContext Context)
        {
            if (true /* Trade pending */)
                Context.Transition(WaitForTradeToFinish);
            else
                Context.Transition(ProcessTrade);
        }

        public static void ProcessTrade(DialogueContext Context)
        {
            // Process the trade.
        }
    }
}