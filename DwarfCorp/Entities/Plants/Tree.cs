using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Tree : Plant
    {
        public Timer HurtTimer { get; set; }

        public Tree() {
            SetFlag(Flag.DontUpdate, true);

        }

        public Tree(string name, ComponentManager manager, Vector3 position, string asset, String seed, float treeSize, bool emitWood = true) :
            base(manager, name, position, MathFunctions.Rand(-0.1f, 0.1f),
                new Vector3(
                    (70.0f / 32.0f) * 0.75f * treeSize, // Ugh, need to load the asset to get it's size so we can apply this calculation.
                    (80.0f / 32.0f) * treeSize,
                    (70.0f / 32.0f) * 0.75f * treeSize),
             asset, treeSize)
        {
            HurtTimer = new Timer(1.0f, false);

            AddChild(new Health(Manager, "HP", 100.0f * treeSize, 0.0f, 100.0f * treeSize));
            AddChild(new Flammable(Manager, "Flames"));

            Tags.Add("Vegetation");
            if (emitWood)
                Tags.Add("EmitsWood");

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBoxSize, LocalBoundingBoxOffset)) as Inventory;

            // Can these be spawned when the tree dies rather than when it is created?
            if (emitWood)
            {
                // Todo: Check entity def for resource emitted. Stop auto generating wood types.
                var wood = Library.CreateResourceType(Library.GetResourceType("Wood"));
                wood.Name = String.Format("{0} Wood", Name.Split(' ').First());
                wood.ShortName = wood.Name;
                Library.AddResourceTypeIfNew(wood);

                for (int i = 0; i < treeSize * 2; i++)
                {
                    inventory.Resources.Add(new Inventory.InventoryItem()
                    {
                        MarkedForRestock = false,
                        MarkedForUse = false,
                        Resource = wood.Name
                    });
                }
            }

            for (int i = 0; i < treeSize * 2; i++)
            {
                inventory.Resources.Add(new Inventory.InventoryItem()
                {
                    MarkedForRestock = false,
                    MarkedForUse = false,
                    Resource = seed
                });
            }

            AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter",
                Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_tree_cut_down_1
            });

            CollisionType = CollisionType.Static;
            PropogateTransforms();
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            CreateCrossPrimitive(MeshAsset);
            base.CreateCosmeticChildren(Manager);
        }

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if (messageToReceive.Type == Message.MessageType.OnHurt)
            {
                HurtTimer.Update(DwarfTime.LastTime);

                if (HurtTimer.HasTriggered)
                    if (GetComponent<ParticleTrigger>().HasValue(out var particles))
                        particles.Trigger(1);
            }

            base.ReceiveMessageRecursive(messageToReceive);
        }
    }
}
