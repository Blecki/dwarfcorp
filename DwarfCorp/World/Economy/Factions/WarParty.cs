using System;
using System.Linq;

namespace DwarfCorp
{
    public class WarParty : Expedition
    {
        public DateTimer PrepareTimer = null;
        public WarParty(DateTime date) : base(date)
        {
            PrepareTimer = new DateTimer(date, new TimeSpan(0, 1, 0, 0, 0));
            this.ExpiditionState = Expedition.State.Arriving;
        }

        public bool IsPreparing()
        {
            return !PrepareTimer.HasTriggered;
        }

        public bool UpdateTimer(DateTime now)
        {
            PrepareTimer.Update(now);
            return PrepareTimer.HasTriggered;
        }

        public void Update(WorldManager World)
        {
            var doneWaiting = UpdateTimer(World.Time.CurrentDate);
            Creatures.RemoveAll(creature => creature.IsDead);
            if (DeathTimer.Update(World.Time.CurrentDate))
                Creatures.ForEach((creature) => creature.Die());

            var politics = World.Overworld.GetPolitics(OwnerFaction.ParentFaction, OtherFaction.ParentFaction);

            if (politics.GetCurrentRelationship() != Relationship.Hateful)
                RecallWarParty();

            if (Creatures.All(creature => creature.IsDead))
                ShouldRemove = true;

            if (doneWaiting)
            {
                foreach (var creature in OwnerFaction.Minions)
                    if (creature.Tasks.Count == 0)
                    {
                        var enemyMinion = OtherFaction.GetNearestMinion(creature.Position);
                        if (enemyMinion != null)// && !enemyMinion.Stats.IsFleeing)
                            creature.AssignTask(new KillEntityTask(enemyMinion.Physics, KillEntityTask.KillType.Attack));
                    }

                if (ExpiditionState == Expedition.State.Arriving)
                {
                    World.MakeAnnouncement(String.Format("The war party from {0} is attacking!", OwnerFaction.ParentFaction.Name));
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.15f);
                    ExpiditionState = Expedition.State.Fighting;
                }
            }
        }

        public void RecallWarParty()
        {
            ExpiditionState = Expedition.State.Leaving;
            foreach (CreatureAI creature in Creatures)
                creature.LeaveWorld();
        }
    }
}