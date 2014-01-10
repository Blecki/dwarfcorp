using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature finds an item from a stockpile or room with the given tags, goes to it, and picks it up.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GetItemWithTagsAct : CompoundCreatureAct
    {
        private TagList Tags { get; set; }

        public GetItemWithTagsAct()
        {

        }

        public GetItemWithTagsAct(CreatureAIComponent agent, TagList tags) :
            base(agent)
        {
            Name = "Get Item With Tags " + tags;
            Tags = tags;

        }

        public override void Initialize()
        {
            Item toGather = null;
            Item closestItem = Agent.Faction.FindNearestItemWithTags(Tags, Agent.Position, false);


            if (closestItem != null)
            {
                toGather = closestItem;
            }

            if (toGather == null)
            {
                Tree = null;
            }
            else if (toGather.Zone is Stockpile)
            {
                Tree = new Sequence(new GoToEntityAct(toGather.UserData, Agent),
                                    new SetBlackboardData<LocatableComponent>(Agent, "TargetObject", toGather.UserData),
                                    new PickUpAct(Agent, PickUpAct.PickUpType.Stockpile, toGather.Zone, "TargetObject"));
            }
            else if (toGather.Zone is Room)
            {
                Tree = new Sequence(new GoToEntityAct(toGather.UserData, Agent),
                                    new SetBlackboardData<LocatableComponent>(Agent, "TargetObject", toGather.UserData),
                                    new PickUpAct(Agent, PickUpAct.PickUpType.Room, toGather.Zone, "TargetObject"));
            }
            else
            {
                Tree = new Sequence(new GoToEntityAct(toGather.UserData, Agent),
                                    new SetBlackboardData<LocatableComponent>(Agent, "TargetObject", toGather.UserData),
                                    new PickUpAct(Agent, PickUpAct.PickUpType.None, null, "TargetObject"));
            }
            base.Initialize();
        }
    }

}