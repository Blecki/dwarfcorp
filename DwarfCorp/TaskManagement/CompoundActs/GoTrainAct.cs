using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    internal class GoTrainAct : CompoundCreatureAct
    {
        public bool Magical { get; set; }

        public GoTrainAct()
        {

        }

        public GoTrainAct(CreatureAI creature) :
            base(creature)
        {
            Name = "Train";
        }

        public IEnumerable<Act.Status> DoMagicResearch()
        {
            var obj = Agent.Blackboard.GetData<GameComponent>("training-object");
            if (obj == null)
            {
                Agent.SetTaskFailureReason("Failed to find magical object for research purposes.");
                yield return Act.Status.Fail;
                yield break;
            }

            float timer = 0;
            foreach (var status in Creature.HitAndWait(false, () => { return 10.0f;}, () => { return timer; }, () => { timer++; }, () => { return obj.Position; }, ContentPaths.Audio.Oscar.sfx_ic_dwarf_magic_research))
            {
                yield return Act.Status.Running;
            }
            Creature.AI.AddXP(5);
            yield return Act.Status.Success;
        }

        public override void Initialize()
        {
            var trainAct = Magical ? new Wrap(DoMagicResearch) { Name = "Magic research" } as Act : new AttackAct(Agent, "training-object") { Training = true, Timeout = new Timer(10.0f, false) };
            var unreserveAct = new Wrap(() => Creature.Unreserve("training-object"));

            Tree = new Select(
                new Sequence(
                    new Wrap(() => Creature.FindAndReserve(Magical ? "Research" : "Train", "training-object")),
                    new Select(
                        new Sequence(
                            new GoToTaggedObjectAct(Agent) { Teleport = false, TeleportOffset = new Vector3(1, 0, 0), ObjectBlackboardName = "training-object" },
                            trainAct,
                            unreserveAct),
                        new Sequence(
                            unreserveAct,
                            false))),
                new Sequence(unreserveAct, false));

            base.Initialize();
        }

        public override void OnCanceled()
        {
            foreach (var statuses in Creature.Unreserve("training-object"))
                continue;
            base.OnCanceled();
        }
    }
}