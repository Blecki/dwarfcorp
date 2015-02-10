using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class FarmAct : CompoundCreatureAct
    {
        public Farm FarmToWork { get; set; }

        public FarmAct()
        {
            Name = "Work a farm";
        }

        public FarmAct(CreatureAI agent) :
            base(agent)
        {
            Name = "Work a farm";
        }

        public IEnumerable<Status> FarmATile(string tileName)
        {
            Farm.FarmTile tile = Creature.AI.Blackboard.GetData<Farm.FarmTile>(tileName);
            if (tile == null) yield return Status.Fail;
            else if (!tile.IsFree()) yield return Status.Success;
            else
            {
                if (tile.Plant != null && tile.Plant.IsDead) tile.Plant = null;
                while (tile.Progress < 100.0f && tile.IsFree())
                {
                    Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                    Creature.Physics.Velocity *= 0.1f;
                    tile.Progress += Creature.Stats.BaseFarmSpeed;

                    Drawer2D.DrawLoadBar(Agent.Position + Vector3.Up, Color.White, Color.Black, 100, 16, tile.Progress / 100.0f);
                    if (tile.Progress >= 100.0f && tile.IsFree())
                    {
                        tile.Progress = 0.0f;
                        FarmToWork.CreatePlant(tile);
                    }

                    yield return Status.Running;
                }
                Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                Creature.AI.AddThought(Thought.ThoughtType.Farmed);
                Creature.AI.AddXP(10);
                yield return Status.Success;
            }
        }

        public IEnumerable<Status> GetClosestTile()
        {
            Farm.FarmTile closestTile = FarmToWork.GetNearestFreeFarmTile(Creature.AI.Position);
            if (closestTile == null) yield return Status.Fail;
            else
            {
                Creature.AI.Blackboard.SetData("ClosestTile", closestTile);
                Creature.AI.Blackboard.SetData("ClosestVoxel", closestTile.Vox);
                yield return Status.Success;   
            }
        }

        public override void Initialize()
        {
            if (FarmToWork != null)
            {
                Farm.FarmTile closestTile = FarmToWork.GetNearestFreeFarmTile(Creature.AI.Position);
                if (closestTile != null)
                {
                    Tree = new Sequence(
                        new Wrap(GetClosestTile),
                        new GoToVoxelAct("ClosestVoxel", PlanAct.PlanType.Adjacent, Creature.AI),
                        new StopAct(Creature.AI),
                        new Wrap(() => FarmATile("ClosestTile")));
                }
            }

            base.Initialize();
        }
    }
}
