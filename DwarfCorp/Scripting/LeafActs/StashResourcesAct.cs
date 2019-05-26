using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A creature grabs a given item and puts it in their inventory
    /// </summary>
    public class StashResourcesAct : CreatureAct
    {
        public ResourceAmount Resources { get; set; }
        public Faction Faction = null;
        public Room Zone = null;
        public Inventory.RestockType RestockType = Inventory.RestockType.None;

        public StashResourcesAct()
        {

        }

        public StashResourcesAct(CreatureAI agent, Room zone, ResourceAmount resources) :
            base(agent)
        {
            Zone = zone;
            Resources = resources;
            Name = "Stash " + Resources.Type;
        }

        public override IEnumerable<Status> Run()
        {
            Creature.IsCloaked = false;
            if (Faction == null)
            {
                Faction = Agent.Faction;
            }
            if (Zone != null)
            {
                var resourcesToStock = Creature.Inventory.Resources.Where(a => a.MarkedForRestock && Zone is Stockpile && (Zone as Stockpile).IsAllowed(a.Resource)).ToList();
                foreach (var resource in resourcesToStock)
                {
                    List<GameComponent> createdItems = Creature.Inventory.RemoveAndCreate(new ResourceAmount(resource.Resource), Inventory.RestockType.RestockResource);

                    foreach (GameComponent b in createdItems)
                    {
                        if (Zone.AddItem(b))
                        {
                            Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
                            Creature.Stats.NumItemsGathered++;
                            Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                            Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
                            Creature.Sprite.PlayAnimations(Creature.Stats.CurrentClass.AttackMode);

                            while (!Creature.Sprite.AnimPlayer.IsDone())
                            {
                                yield return Status.Running;
                            }

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
            bool removed = Faction.RemoveResources(Resources, Agent.Position, Zone, true);

            if(!removed)
            {
                yield return Status.Fail;
            }
            else
            {
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

