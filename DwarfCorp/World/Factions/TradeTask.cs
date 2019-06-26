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
            Priority = TaskPriority.High;
            TradePort = tradePort;
            Envoy = envoy;
        }

        IEnumerable<Act.Status> RecallEnvoyOnFail()
        {
            Envoy.RecallEnvoy();
            Envoy.OwnerFaction.World.MakeAnnouncement("Envoy from " + Envoy.OwnerFaction.ParentFaction.Name + " left. Trade port inaccessible.");
            yield return Act.Status.Success;
        }

        public override Act CreateScript(Creature agent)
        {
            return new GoToZoneAct(agent.AI, TradePort) | new Wrap(() => RecallEnvoyOnFail());
        }

    }
}