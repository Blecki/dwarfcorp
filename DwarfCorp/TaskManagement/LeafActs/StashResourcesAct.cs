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
        public ResourceAmount Resources { get; set; }
        public Stockpile Zone = null;
        public Inventory.RestockType RestockType = Inventory.RestockType.None;

        public StashResourcesAct()
        {

        }

        public StashResourcesAct(CreatureAI agent, Stockpile zone, ResourceAmount resources) :
            base(agent)
        {
            Zone = zone;
            Resources = resources;
            Name = "Stash " + Resources.Type;
        }

        public override IEnumerable<Status> Run()
        {
            Creature.IsCloaked = false;
            if (Zone != null)
            {
                var resourcesToStock = Creature.Inventory.Resources.Where(a => a.MarkedForRestock && Zone is Stockpile && (Zone as Stockpile).IsAllowed(a.Resource)).ToList();
                foreach (var resource in resourcesToStock)
                {
                    List<GameComponent> createdItems = Creature.Inventory.RemoveAndCreate(new ResourceAmount(resource.Resource, 1), Inventory.RestockType.RestockResource);

                    foreach (var b in createdItems.OfType<ResourceEntity>())
                    {
                        if (Zone.AddResource(b.Resource))
                        {
                            var toss = new TossMotion(1.0f, 2.5f, b.LocalTransform, Zone.GetBoundingBox().Center() + new Vector3(0.5f, 0.5f, 0.5f));

                            if (b.GetRoot().GetComponent<Physics>().HasValue(out var physics))
                                physics.CollideMode = Physics.CollisionMode.None;

                            b.AnimationQueue.Add(toss);
                            toss.OnComplete += b.Die;

                            Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
                            Creature.Stats.NumItemsGathered++;
                            Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                            Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
                            Creature.Sprite.PlayAnimations(Creature.Stats.CurrentClass.AttackMode);

                            while (!Creature.Sprite.AnimPlayer.IsDone())
                                yield return Status.Running;

                            yield return Status.Running;
                        }
                        else
                        {
                            Creature.Inventory.AddResource(new ResourceAmount(resource.Resource, 1), Inventory.RestockType.RestockResource);
                            b.Delete();
                        }
                    }
                }
            }

            Timer waitTimer = new Timer(1.0f, true);
            bool removed = Creature.World.RemoveResourcesFromSpecificZone(Resources, Zone);
            
            if (!removed)
                yield return Status.Fail;
            else
            {
                var newEntity = EntityFactory.CreateEntity<GameComponent>(Resources.Type + " Resource", Zone.GetBoundingBox().Center() + new Vector3(0.0f, 1.0f, 0.0f));

                if (newEntity.GetRoot().GetComponent<Physics>().HasValue(out var newPhysics))
                    newPhysics.CollideMode = Physics.CollisionMode.None;

                var toss = new TossMotion(1.0f + MathFunctions.Rand(0.1f, 0.2f), 2.5f + MathFunctions.Rand(-0.5f, 0.5f), newEntity.LocalTransform, Agent.Position);
                newEntity.AnimationQueue.Add(toss);
                toss.OnComplete += () => newEntity.Die();

                Agent.Creature.Inventory.AddResource(Resources.CloneResource(), Inventory.RestockType.None);
                Agent.Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
                while (!waitTimer.HasTriggered)
                {
                    Agent.Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                    waitTimer.Update(DwarfTime.LastTime);
                    yield return Status.Running;
                }
                Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
                yield return Status.Success;
            }

        }

    }

}

