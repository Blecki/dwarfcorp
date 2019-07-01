using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Scripting
{
    public class GambleAct : CompoundCreatureAct
    {
        public Gambling Game;

        public IEnumerable<Act.Status> Gamble()
        {
            while (Game.State != Gambling.Status.Ended && Game.Participants.Contains(Agent.Creature))
            {
                Agent.Physics.Face(Game.Location);
                Agent.Physics.Velocity *= 0.9f;
                Agent.Creature.CurrentCharacterMode = CharacterMode.Sitting;
                yield return Act.Status.Running;
            }
            yield return Act.Status.Success;
        }

        public override void Initialize()
        {
            Name = "Gamble";
            Game.Join(Agent.Creature);

            var voxel = new VoxelHandle(Agent.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(Game.Location));

            if (voxel.IsValid)
                Tree = new Sequence(new GoToVoxelAct(voxel, PlanAct.PlanType.Radius, Agent) { Name = "Go to gambling site.", Radius = 3.0f }, new Wrap(Gamble)) | new Wrap(Cleanup);
            else
                Tree = new Always(Status.Fail);

            base.Initialize();
        }

        public IEnumerable<Act.Status> Cleanup()
        {
            if (Game != null && Game.Participants.Contains(Agent.Creature))
                Game.Participants.Remove(Agent.Creature);
            yield return Act.Status.Success;
        }

        public override void OnCanceled()
        {
            foreach(var status in Cleanup())
            {
                // Just need to enumerate cleanup.
            }

            base.OnCanceled();
        }
    }
}
