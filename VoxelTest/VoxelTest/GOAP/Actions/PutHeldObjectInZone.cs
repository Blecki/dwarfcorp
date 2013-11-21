using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class PutHeldObjectInZone : Action
    {
        public Zone zone;

        public PutHeldObjectInZone(CreatureAIComponent creature, Zone z)
        {
            zone = z;
            Name = "Put Held Object in : " + z.ID;
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.HandState] = GOAP.HandState.Full;
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Zone;
            PreCondition[GOAPStrings.AtTarget] = true;
            PreCondition[GOAPStrings.TargetDead] = false;
            PreCondition[GOAPStrings.TargetEntityInZone] = false;
            PreCondition[GOAPStrings.MotionStatus] = GOAP.MotionStatus.Stationary;
            PreCondition[GOAPStrings.TargetZone] = z;
            PreCondition[GOAPStrings.CurrentZone] = z;
            PreCondition[GOAPStrings.TargetZoneFull] = false;

            Effects = new WorldState();
            Effects[GOAPStrings.HandState] = GOAP.HandState.Empty;
            Effects[GOAPStrings.HeldObject] = null;
            Effects[GOAPStrings.TargetEntityInZone] = true;
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.None;
            Effects[GOAPStrings.HeldItemTags] = null;
            Cost = 0.1f;

            if(z is Stockpile)
            {
                BoundingBox box = ((Stockpile) z).GetBoundingBox();
                Vector3 center = (box.Min + box.Max) * 0.5f;
                Cost = (creature.Physics.GlobalTransform.Translation - center).LengthSquared() * 0.1f;
            }
        }

        public override void Apply(WorldState state)
        {
            Item item = (Item) (state[GOAPStrings.HeldObject]);

            if(item != null)
            {
                state[GOAPStrings.TargetEntity] = new Item(item.ID, item.Zone, item.UserData);
            }
            else
            {
                state[GOAPStrings.TargetEntity] = null;
            }

            base.Apply(state);
        }

        public override void UnApply(WorldState state)
        {
            Item item = (Item) (state[GOAPStrings.TargetEntity]);

            if(item != null)
            {
                state[GOAPStrings.HeldObject] = new Item(item.ID, null, item.UserData);
            }
            else
            {
                state[GOAPStrings.HeldObject] = null;
            }

            base.UnApply(state);
        }

        public override Action.PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            Item item = (Item) creature.Goap.Belief[GOAPStrings.HeldObject];


            if(item == null)
            {
                return Action.PerformStatus.Failure;
            }

            PutHeldObjectInZone put = this;
            Zone zone = put.zone;

            if(zone is Stockpile)
            {
                Stockpile stock = (Stockpile) zone;

                if(stock.AddItem(creature.Hands.GetFirstGrab(), creature.TargetVoxel))
                {
                    LocatableComponent grabbed = creature.Hands.GetFirstGrab();
                    creature.Hands.UnGrab(grabbed);

                    Matrix m = Matrix.Identity;
                    m.Translation = creature.TargetVoxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f);
                    grabbed.LocalTransform = m;
                    grabbed.HasMoved = true;
                    grabbed.DrawBoundingBox = false;
                    item.ReservedFor = null;

                    return PerformStatus.Success;
                }
                else
                {
                    LocatableComponent grabbed = creature.Hands.GetFirstGrab();
                    creature.Hands.UnGrab(grabbed);

                    Matrix m = Matrix.Identity;
                    m.Translation = creature.Physics.GlobalTransform.Translation;
                    grabbed.LocalTransform = m;
                    creature.Goap.Belief[GOAPStrings.HandState] = GOAP.HandState.Empty;

                    creature.Master.AddGatherDesignation(grabbed);
                    item.ReservedFor = null;

                    return PerformStatus.Failure;
                }
            }
            else if(zone is Room)
            {
                Room room = (Room) zone;

                if(creature.Master.RoomDesignator.IsBuildDesignation(room))
                {
                    VoxelBuildDesignation des = creature.Master.RoomDesignator.GetBuildDesignation(room);

                    if(des == null || des.BuildDesignation.IsBuilt)
                    {
                        return PerformStatus.Invalid;
                    }

                    des.AddResource(creature.Hands.GetFirstGrab().Tags[0]);
                    LocatableComponent grabbed = creature.Hands.GetFirstGrab();
                    creature.Hands.UnGrab(grabbed);
                    grabbed.Die();


                    if(des.MeetsBuildRequirements())
                    {
                        des.Build();
                    }
                    item.ReservedFor = null;
                    return PerformStatus.Success;
                }
                else
                {
                    Room theRoom = (Room) zone;
                    theRoom.AddItem(item, creature.TargetVoxel);

                    LocatableComponent grabbed = creature.Hands.GetFirstGrab();
                    creature.Hands.UnGrab(grabbed);

                    Matrix m = Matrix.Identity;
                    m.Translation = creature.TargetVoxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f);
                    grabbed.LocalTransform = m;
                    grabbed.HasMoved = true;
                    grabbed.DrawBoundingBox = false;
                    item.ReservedFor = null;
                    return PerformStatus.Success;
                }
            }
            else
            {
                LocatableComponent grabbed = creature.Hands.GetFirstGrab();
                creature.Hands.UnGrab(grabbed);
                Matrix m = Matrix.Identity;
                m.Translation = creature.TargetVoxel.WorldPosition + new Vector3(0.5f, 1.5f, 0.5f);
                grabbed.LocalTransform = m;
                creature.Goap.Belief[GOAPStrings.HandState] = GOAP.HandState.Empty;
                item.ReservedFor = null;
                return PerformStatus.Success;
            }
        }

        public override ValidationStatus ContextValidate(CreatureAIComponent creature)
        {
            Item item = (Item) creature.Goap.Belief[GOAPStrings.HeldObject];

            if(item == null)
            {
                item.ReservedFor = null;
                return ValidationStatus.Replan;
            }

            PutHeldObjectInZone put = this;
            Zone zone = put.zone;


            if(zone == null)
            {
                return ValidationStatus.Invalid;
            }

            if(zone is Stockpile)
            {
                Stockpile stock = (Stockpile) zone;

                item.ReservedFor = null;
                return ValidationStatus.Ok;
            }
            else
            {
                if(zone is Room)
                {
                    Room r = (Room) zone;

                    if(r.RoomType.Name != "BalloonPort")
                    {
                        VoxelBuildDesignation des = creature.Master.RoomDesignator.GetBuildDesignation(r);

                        if(des == null)
                        {
                            return ValidationStatus.Invalid;
                        }
                    }
                }
                item.ReservedFor = null;
                return ValidationStatus.Ok;
            }
        }
    }

}