using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature grabs a given item and puts it in their inventory
    /// </summary>
    public class StashResourcesAct : CreatureAct
    {
        public Resource Resource;
        public Stockpile Zone = null;
        public Inventory.RestockType RestockType = Inventory.RestockType.None;

        public StashResourcesAct()
        {

        }

        public StashResourcesAct(CreatureAI agent, Stockpile zone, Resource Resource) :
            base(agent)
        {
            Zone = zone;
            this.Resource = Resource;
            Name = "Stash " + Resource.TypeName;
        }

        public override IEnumerable<Status> Run()
        {
            Creature.IsCloaked = false;
            if (Zone != null)
            {
                var resourcesToStock = Creature.Inventory.Resources.Where(a => a.MarkedForRestock && Zone is Stockpile && (Zone as Stockpile).IsAllowed(a.Resource.TypeName)).ToList();

                foreach (var resource in resourcesToStock)
                {
                    var createdItem = Creature.Inventory.RemoveAndCreate(resource.Resource, Inventory.RestockType.RestockResource);
                    if (Zone.AddResource(resource.Resource))
                        {
                            var toss = new TossMotion(1.0f, 2.5f, createdItem.LocalTransform, Zone.GetBoundingBox().Center() + new Vector3(0.5f, 0.5f, 0.5f));

                            if (createdItem.GetRoot().GetComponent<Physics>().HasValue(out var physics))
                                physics.CollideMode = Physics.CollisionMode.None;

                        createdItem.AnimationQueue.Add(toss);
                            toss.OnComplete += createdItem.Die;

                            Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
                            Creature.Stats.NumItemsGathered++;

                        if (Creature.Stats.CurrentClass.HasValue(out var c))
                        {
                            Creature.CurrentCharacterMode = c.AttackMode;
                            Creature.Sprite.ResetAnimations(c.AttackMode);
                            Creature.Sprite.PlayAnimations(c.AttackMode);

                        }
                            while (!Creature.Sprite.AnimPlayer.IsDone())
                                yield return Status.Running;

                            yield return Status.Running;
                        }
                        else
                        {
                            Creature.Inventory.AddResource(resource.Resource, Inventory.RestockType.RestockResource);
                        createdItem.Delete();
                        }
                }
            }

            Timer waitTimer = new Timer(1.0f, true);
            bool removed = Creature.World.RemoveResourcesFromSpecificZone(Resource, Zone);
            
            if (!removed)
                yield return Status.Fail;
            else
            {
                var newEntity = Agent.Manager.RootComponent.AddChild(new ResourceEntity(Agent.Manager, Resource, Zone.GetBoundingBox().Center() + new Vector3(0.0f, 1.0f, 0.0f)));

                if (newEntity.GetRoot().GetComponent<Physics>().HasValue(out var newPhysics))
                    newPhysics.CollideMode = Physics.CollisionMode.None;

                var toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f), 2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, Agent.Position);
                newEntity.AnimationQueue.Add(toss);
                toss.OnComplete += () => newEntity.Die();

                Agent.Creature.Inventory.AddResource(Resource, Inventory.RestockType.None);

                if (Creature.Stats.CurrentClass.HasValue(out var _c))
                    Agent.Creature.Sprite.ResetAnimations(_c.AttackMode);
                while (!waitTimer.HasTriggered)
                {
                    if (Creature.Stats.CurrentClass.HasValue(out var __c))
                        Agent.Creature.CurrentCharacterMode = __c.AttackMode;
                    waitTimer.Update(DwarfTime.LastTime);
                    yield return Status.Running;
                }
                Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
                yield return Status.Success;
            }

        }

    }

}

