using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature puts a specified resource (in its inventory) into a zone.
    /// </summary>
    public class PutResourceInZone : CreatureAct
    {
        public string StockpileName { get; set; }
        public string ResourceName { get; set; }

        public PutResourceInZone(CreatureAI agent, string stockpileName, string resourceName) :
            base(agent)
        {
            Name = "Put Item";
            StockpileName = stockpileName;
            ResourceName = resourceName;
        }

        public override IEnumerable<Status> Run()
        {
            if (Agent == null || Agent.Blackboard == null || Creature == null || Creature.Inventory == null)
            {
                yield return Status.Fail;
                yield break;
            }

            var zone = Agent.Blackboard.GetData<Zone>(StockpileName);
            var resource = Agent.Blackboard.GetData<Resource>(ResourceName);

            if (zone == null || !(zone is Stockpile) || resource == null)
            {
                if (Creature != null) Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Status.Fail;
                yield break;
            }

            var createdItems = Creature.Inventory.RemoveAndCreate(resource, Inventory.RestockType.RestockResource);

            if ((zone as Stockpile).AddResource(resource))
            {
                if (createdItems != null)
                {
                    var toss = new TossMotion(1.0f, 2.5f, createdItems.LocalTransform, zone.GetBoundingBox().Center() + new Vector3(0.5f, 0.5f, 0.5f));

                    if (createdItems.GetRoot().GetComponent<Physics>().HasValue(out var physics))
                        physics.CollideMode = Physics.CollisionMode.None;

                    createdItems.AnimationQueue.Add(toss);
                    toss.OnComplete += createdItems.Die;
                }
                Creature.Stats.NumItemsGathered++;
            }
            else
            {
                Creature.Inventory.AddResource(resource, Inventory.RestockType.RestockResource);
                if (createdItems != null)
                    createdItems.Delete();
            }

            if (Creature.NoiseMaker != null && Creature.AI != null)
                Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);

            if (Creature.Stats != null && Creature.Stats.CurrentClass.HasValue(out var c) && Creature.Sprite != null)
            {
                Creature.CurrentCharacterMode = c.AttackMode;
                Creature.Sprite.ResetAnimations(c.AttackMode);
                Creature.Sprite.PlayAnimations(c.AttackMode);

                while (!Creature.Sprite.AnimPlayer.IsDone())
                    yield return Status.Running;
            }

            if (Library.GetResourceType(resource.TypeName).HasValue(out var res) && res.Tags.Contains("Corpse"))
                Creature.AddThought("I laid a friend to rest.", new TimeSpan(0, 8, 0, 0), 10.0f);

            yield return Status.Running;
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Status.Success;
        }
    }
}