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
    public class CandyCane : Plant
    {
        [EntityFactory("Candycane")]
        private static GameComponent __factory06(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new CandyCane("Candycane", Manager, Position, "Entities\\Plants\\candycane", Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Snow Candycane")]
        private static GameComponent __factory06b(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new CandyCane("Candycane", Manager, Position, "Entities\\Plants\\candycane-snow", Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Candycane Sprout")]
        private static GameComponent __factory07(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Candycane", Position, "Entities\\Plants\\candycane-sprout")
            {
                GrowthHours = 24.0f,
                MaxSize = 2.0f,
                GoodBiomes = "Tiaga Jolly Forest", // Todo: This should be data.
                BadBiomes = "Desert Tundra Waste Haunted Forest"
            };
        }

        public Timer HurtTimer { get; set; }

        public CandyCane() {
            SetFlag(Flag.DontUpdate, true);

        }

        public CandyCane(string name, ComponentManager manager, Vector3 position, string asset, float treeSize) :
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

            Inventory inventory = AddChild(new Inventory(Manager, "Inventory", BoundingBoxSize, LocalBoundingBoxOffset)) as Inventory;

            // Can these be spawned when the tree dies rather than when it is created?
            for (int i = 0; i < treeSize * 4; i++)
            {
                inventory.Resources.Add(new Inventory.InventoryItem()
                {
                    MarkedForRestock = false,
                    MarkedForUse = false,
                    Resource = "Peppermint"
                });
            }

            AddChild(new ParticleTrigger("snow_particle", Manager, "LeafEmitter",
                Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_tree_cut_down_1
            });

            CollisionType = CollisionType.Static;
            PropogateTransforms();
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            
            CreateQuadPrimitive(MeshAsset);
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
