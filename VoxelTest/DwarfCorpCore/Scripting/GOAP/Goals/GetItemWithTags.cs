using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GetItemWithTags : Goal
    {
        public TagList Tags = null;

        public GetItemWithTags(GOAP agent, TagList tags)
        {
            Tags = tags;
            Name = "Get Item with Tags: " + tags.ToString();
            Priority = 0.1f;
            Reset(agent);
        }

        public override void Reset(GOAP agent)
        {
            State[GOAPStrings.HeldItemTags] = Tags;
            State[GOAPStrings.HandState] = GOAP.HandState.Full;
        }

        public override List<Action> GetPresetPlan(CreatureAIComponent creature, GOAP agent)
        {
            List<Action> toReturn = new List<Action>();

            Item toGather = null;

            foreach(Item i in creature.Goap.Items)
            {
                if(Tags.Contains(i.UserData.Tags))
                {
                    toGather = i;
                    break;
                }
            }

            if(toGather == null)
            {
                return null;
            }
            else
            {
                toReturn.Add(new SetTargetEntity(toGather));
                toReturn.Add(new GoToTargetEntity());
                toReturn.Add(new PickupTargetEntity(agent));
            }
            return toReturn;
        }

        public override void ContextReweight(CreatureAIComponent creature)
        {
            Priority = 0.0f;
            Cost = 1.0f;
        }

        public override bool ContextValidate(CreatureAIComponent creature)
        {
            Reset(creature.Goap);
            foreach(Item i in creature.Goap.Items)
            {
                if(Tags.Contains(i.UserData.Tags))
                {
                    return true;
                }
            }
            return false;
        }

        public override Act GetBehaviorTree(CreatureAIComponent creature)
        {
            return new GetItemWithTagsAct(creature, Tags);
        }
    }

}