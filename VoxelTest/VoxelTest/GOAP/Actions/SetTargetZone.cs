using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class SetTargetZone : Action
    {
        public Zone zone;

        public SetTargetZone(CreatureAIComponent creature, Zone z)
        {
            zone = z;
            Name = "SetTargetZone(" + z.ID + ")";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.None;
            PreCondition[GOAPStrings.TargetZone] = null;
            PreCondition[GOAPStrings.ZoneTags] = null;
            PreCondition[GOAPStrings.AtTarget] = false;
            PreCondition[GOAPStrings.TargetZoneType] = null;


            Effects = new WorldState();
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.Zone;
            Effects[GOAPStrings.TargetZone] = zone;
            Effects[GOAPStrings.AtTarget] = false;


            if (z is Stockpile)
            {
                Effects[GOAPStrings.TargetZoneType] = "Stockpile";
                Effects[GOAPStrings.TargetZoneFull] = ((Stockpile)z).IsFull();
                BoundingBox box = ((Stockpile)z).GetBoundingBox();
                Vector3 center = (box.Min + box.Max) * 0.5f;
                Cost = (creature.Physics.GlobalTransform.Translation - center).LengthSquared() * 0.1f;
            }
            else if (z is Room)
            {
                Effects[GOAPStrings.TargetZoneType] = "Room";
                Effects[GOAPStrings.TargetZoneFull] = ((Room)z).IsFull();
            }

            HashSet<string> tags = new HashSet<string>();
            foreach(Item i in zone.ListItems())
            {
                foreach (string t in i.userData.Tags)
                {
                    tags.Add(t);
                }
            }

            Effects[GOAPStrings.ZoneTags] = new TagList(tags);
        }

        public override Action.PerformStatus PerformContextAction(CreatureAIComponent creature, GameTime time)
        {
            creature.TargetVoxel = null;
            return base.PerformContextAction(creature, time);
        }

    }
}
