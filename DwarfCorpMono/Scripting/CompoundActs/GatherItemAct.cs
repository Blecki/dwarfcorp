using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class GatherItemAct : CompoundCreatureAct
    {
        public LocatableComponent ItemToGather { get; set; }
        public string ItemID { get; set; }


        public bool IsGatherable()
        {
            return (!ItemToGather.IsStocked) && (Agent.Master.GatherDesignations.Contains(ItemToGather) || Agent.Hands.GetFirstGrab() == ItemToGather);
        }

        public Act EntityIsGatherable()
        {
            return new Condition(IsGatherable);
        }

        public GatherItemAct(CreatureAIComponent agent, string item) :
            base(agent)
        {
            ItemID = item;
            Tree = null;
            ItemToGather = null;
            Name = "Gather Item";
        }

        public GatherItemAct(CreatureAIComponent agent, LocatableComponent item) :
            base(agent)
        {
            ItemToGather = item;
            Name = "Gather Item";
            Tree = new Sequence((EntityIsGatherable() & new GoToEntityAct(ItemToGather, Agent)),
                                (EntityIsGatherable() & new PickUpTargetAct(agent, PickUpTargetAct.PickUpType.None, null)),
                                new Sequence(
                                new SearchFreeStockpileAct(Agent),
                                new GoToVoxelAct(null, Agent),
                                new PutItemInStockpileAct(Agent, null)) | new DropItemAct(Agent));
                                
        }

        public override void Initialize()
        {

            base.Initialize();
        }


        public override IEnumerable<Status> Run()
        {

            if (Tree == null)
            {
                if (ItemToGather == null)
                {
                    ItemToGather = Agent.Blackboard.GetData<LocatableComponent>(ItemID);
                }


                if (ItemToGather != null)
                {
                    Tree = new Sequence((EntityIsGatherable() & new GoToEntityAct(ItemToGather, Agent)),
                        (EntityIsGatherable() & new PickUpTargetAct(Agent, PickUpTargetAct.PickUpType.None, null)),
                        new Sequence(
                        new SearchFreeStockpileAct(Agent),
                        new GoToVoxelAct(null, Agent),
                        new PutItemInStockpileAct(Agent, null)) | new DropItemAct(Agent));

                    Tree.Initialize();
                }
            }

            if (Tree == null)
            {
                yield return Status.Fail;
            }
            else
            {
                foreach(Status s in base.Run())
                {
                    yield return s;
                }
            }
        }


    }
}
