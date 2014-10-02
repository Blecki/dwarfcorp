using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Tells a creature that it should pick up an item and put it in a stockpile.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GatherItemTask : Task
    {
        public Body EntityToGather = null;
        public string ZoneType = "Stockpile";

        public GatherItemTask()
        {

        }

        public GatherItemTask(Body entity)
        {
            EntityToGather = entity;
            Name = "Gather Entity: " + entity.Name + " " + entity.GlobalID;
        }

        public override Act CreateScript(Creature creature)
        {
            return new GatherItemAct(creature.AI, EntityToGather);
        }

        public override bool IsFeasible(Creature agent)
        {
            return EntityToGather != null && !EntityToGather.IsDead && !agent.AI.GatherManager.ItemsToGather.Contains(EntityToGather) && !agent.Inventory.Resources.IsFull();
        }

        public override bool ShouldRetry(Creature agent)
        {
            return EntityToGather != null && !EntityToGather.IsDead && !agent.AI.GatherManager.ItemsToGather.Contains(EntityToGather);
        }

        public override float ComputeCost(Creature agent)
        {
            return EntityToGather == null  || EntityToGather.IsDead ? 1000 : (agent.AI.Position - EntityToGather.GlobalTransform.Translation).LengthSquared();
        }

        public override void Render(GameTime time)
        {

            Color drawColor = Color.Goldenrod;

            float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * 0.5f));
            drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));

            Drawer3D.DrawBox(EntityToGather.BoundingBox, drawColor, 0.05f * alpha + 0.05f, true);

            base.Render(time);
        }
    }

}