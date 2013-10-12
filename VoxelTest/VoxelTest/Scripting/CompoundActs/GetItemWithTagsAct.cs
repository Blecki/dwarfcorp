using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GetItemWithTagsAct : CompoundCreatureAct
    {
        TagList Tags { get; set; }

        public GetItemWithTagsAct(CreatureAIComponent agent, TagList tags) :
            base(agent)
        {
            Name = "Get Item With Tags " + tags.ToString();
            Tags = tags;
            Item toGather = null;

            Item closestItem = null;
            float closestDist = float.MaxValue;

            foreach (Stockpile s in Agent.Master.Stockpiles)
            {
                Item i = s.FindNearestItemWithTags(tags, Agent.Position);

                if (i != null)
                {
                    float d = (i.userData.GlobalTransform.Translation - Agent.Position).LengthSquared();
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestItem = i;
                    }
                }


            }

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
                Tree = new Sequence(new GoToEntityAct(toGather.userData, agent),
                                    new PickUpTargetAct(agent, PickUpTargetAct.PickUpType.Stockpile, toGather.Zone));
            }
            else if (toGather.Zone is Room)
            {
                Tree = new Sequence(new GoToEntityAct(toGather.userData, agent),
                                    new PickUpTargetAct(agent, PickUpTargetAct.PickUpType.Room, toGather.Zone));
            }
            else
            {
                Tree = new Sequence(new GoToEntityAct(toGather.userData, agent),
                                    new PickUpTargetAct(agent, PickUpTargetAct.PickUpType.None, null));
            }
        }
    }
}
