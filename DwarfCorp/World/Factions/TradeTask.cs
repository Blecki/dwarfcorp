using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class TradeTask : Task
    {
        public Zone TradePort;
        public TradeEnvoy Envoy;

        public TradeTask()
        {

        }

        public TradeTask(Zone tradePort, TradeEnvoy envoy)
        {
            Name = "Trade";
            Priority = PriorityType.High;
            TradePort = tradePort;
            Envoy = envoy;
        }

        IEnumerable<Act.Status> RecallEnvoyOnFail(TradeEnvoy envoy)
        {
            Diplomacy.RecallEnvoy(envoy);
            TradePort.Faction.World.MakeAnnouncement("Envoy from " + envoy.OwnerFaction.ParentFaction.Name + " left. Trade port inaccessible.");
            yield return Act.Status.Success;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GoToZoneAct(agent.AI, TradePort) | new Wrap(() => RecallEnvoyOnFail(Envoy));
        }

    }
}