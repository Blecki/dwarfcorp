using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class GetItemWithTagsAct : CompoundCreatureAct
    {
        private TagList Tags { get; set; }

        public GetItemWithTagsAct(CreatureAIComponent agent, TagList tags) :
            base(agent)
        {
            Name = "Get Item With Tags " + tags.ToString();
            Tags = tags;
            Item toGather = null;

            Item closestItem = null;
            float closestDist = float.MaxValue;

            foreach(Stockpile s in Agent.Master.Stockpiles)
            {
                Item i = s.FindNearestItemWithTags(tags, Agent.Position);

                if(i != null)
                {
                    float d = (i.UserData.GlobalTransform.Translation - Agent.Position).LengthSquared();
                    if(d < closestDist)
                    {
                        closestDist = d;
                        closestItem = i;
                    }
                }
            }

            if(closestItem != null)
            {
                toGather = closestItem;
            }

            if(toGather == null)
            {
                Tree = null;
            }
            else if(toGather.Zone is Stockpile)
            {
                Tree = new Sequence(new GoToEntityAct(toGather.UserData, agent),
                                    new SetBlackboardData<LocatableComponent>(agent, "TargetObject", toGather.UserData),
                                    new PickUpAct(agent, PickUpAct.PickUpType.Stockpile, toGather.Zone, "TargetObject"));
            }
            else if(toGather.Zone is Room)
            {
                Tree = new Sequence(new GoToEntityAct(toGather.UserData, agent),
                                    new SetBlackboardData<LocatableComponent>(agent, "TargetObject", toGather.UserData),
                                    new PickUpAct(agent, PickUpAct.PickUpType.Room, toGather.Zone, "TargetObject"));
            }
            else
            {
                Tree = new Sequence(new GoToEntityAct(toGather.UserData, agent),
                                    new SetBlackboardData<LocatableComponent>(agent, "TargetObject", toGather.UserData),
                                    new PickUpAct(agent, PickUpAct.PickUpType.None, null, "TargetObject"));
            }
        }
    }

}