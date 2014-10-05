using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class CraftItemAct : CompoundCreatureAct
    {
        public CraftLibrary.CraftItemType ItemType { get; set; }
        public Voxel Voxel { get; set; }
        public CraftItemAct()
        {

        }

        public IEnumerable<Status> DestroyResources()
        {
            Creature.Inventory.Remove(CraftLibrary.CraftItems[ItemType].RequiredResources);
            yield return Status.Success;
        }

        public IEnumerable<Status> WaitAndHit(float time)
        {
            Timer timer = new Timer(time, true);
            while (!timer.HasTriggered)
            {
                timer.Update(Act.LastTime);

                Creature.CurrentCharacterMode = Creature.CharacterMode.Attacking;
                Creature.Physics.Velocity *= 0.1f;
                Creature.Attacks[0].PerformNoDamage(Act.LastTime, Creature.AI.Position);
                Drawer2D.DrawLoadBar(Agent.Position + Vector3.Up, Color.White, Color.Black, 100, 16, timer.CurrentTimeSeconds / time);

                yield return Status.Running;
            }

            Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
            Creature.AI.AddThought(Thought.ThoughtType.Crafted);
            Creature.AI.Stats.XP += (int)(time * 5);
            yield return Status.Success;
        }


        public CraftItemAct(CreatureAI creature, Voxel voxel, CraftLibrary.CraftItemType type) :
            base(creature)
        {
            ItemType = type;
            Voxel = voxel;
            Name = "Build craft item";
        }

        private static int x = 0;
        public override void Initialize()
        {
            x++;
            PlayState.AnnouncementManager.Announce("Craft Item act! " + x + "VOX: " + Voxel.GridPosition, "");
            float time = CraftLibrary.CraftItems[ItemType].BaseCraftTime / Creature.AI.Stats.Intelligence;
            Tree = new Sequence(new GetResourcesAct(Agent, CraftLibrary.CraftItems[ItemType].RequiredResources),
                new Sequence(
                    new GoToTaggedObjectAct(Agent) { Tag = "Anvil", Teleport = false, TeleportOffset = new Vector3(1, 0, 0) },
                    new Wrap(() => WaitAndHit(time)),
                    new Wrap(DestroyResources),
                    new GoToVoxelAct(Voxel, PlanAct.PlanType.Adjacent, Agent),
                    new CreateCraftItemAct(Voxel, Creature.AI, ItemType),
                    new Wrap(Creature.RestockAll)) | new Wrap(Creature.RestockAll)
                );
            base.Initialize();
        }
    }
}