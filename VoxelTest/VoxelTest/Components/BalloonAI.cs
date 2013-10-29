using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
 

    public class BalloonAI : GameComponent
    {
        public PhysicsComponent Physics { get; set; }
        public PIDController VelocityController { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float MaxVelocity { get; set; }
        public float MaxForce { get; set; }
        public BalloonState State { get; set; }
        public ShipmentOrder Shipment { get; set; }
        public GameMaster Master { get; set; }

        public List<ResourceAmount> CurrentResources { get; set; }

        bool shipmentGiven = false;

        public enum BalloonState
        {
            DeliveringGoods,
            Waiting,
            Leaving
        }

        public BalloonAI(PhysicsComponent physics, Vector3 target, ShipmentOrder shipment, GameMaster master) :
            base(physics.Manager, "BalloonAI", physics)
        {
            Physics = physics;
            VelocityController = new PIDController(0.9f, 0.1f, 0.0f);
            MaxVelocity = 5.0f;
            MaxForce = 15.0f;
            TargetPosition = target;
            State = BalloonState.DeliveringGoods;
            Shipment = shipment;
            Master = master;
            CurrentResources = new List<ResourceAmount>();


        }

        public override void Die()
        {
            base.Die();
        }

        public void PutResource(LocatableComponent loc)
        {
            string resourceName = loc.Tags[0];

            foreach (ResourceAmount r in CurrentResources)
            {
                if (r.ResourceType.ResourceName == resourceName)
                {
                    r.NumResources++;
                    return;
                }
            }

            ResourceAmount newResource = new ResourceAmount();
            newResource.NumResources = 1;
            newResource.ResourceType = ResourceLibrary.Resources[resourceName];

            CurrentResources.Add(newResource);
        }

        public float GetSellOrder(Item i)
        {
            foreach (ResourceAmount amount in Shipment.SellOrder)
            {
                if (amount.ResourceType.ResourceName == i.userData.Tags[0])
                {
                    return amount.ResourceType.MoneyValue * Master.Economy.SellMultiplier;
                }
            }
            return 0.0f;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            Vector3 targetVelocity = TargetPosition - Physics.GlobalTransform.Translation;

            if (targetVelocity.LengthSquared() > 0.0001f)
            {
                targetVelocity.Normalize();
                targetVelocity *= MaxVelocity;
            }

            Vector3 force = VelocityController.GetOutput((float)gameTime.ElapsedGameTime.TotalSeconds, targetVelocity, Physics.Velocity);

            if (force.Length() > MaxForce)
            {
                force.Normalize();
                force *= MaxForce;
            }

            Physics.ApplyForce(force, (float)gameTime.ElapsedGameTime.TotalSeconds);


            Physics.HasMoved = true;
            Physics.IsSleeping = false;


            switch (State)
            {
                case BalloonState.DeliveringGoods:
                    VoxelChunk chunk = chunks.GetVoxelChunkAtWorldLocation(Physics.GlobalTransform.Translation);

                    if (chunk != null)
                    {
                        Vector3 gridPos = chunk.WorldToGrid(Physics.GlobalTransform.Translation);
                        float height = chunk.GetFilledVoxelGridHeightAt((int)gridPos.X, (int)gridPos.Y, (int)gridPos.Z) + chunk.Origin.Y;
                        TargetPosition = new Vector3(Physics.GlobalTransform.Translation.X, height + 5, Physics.GlobalTransform.Translation.Z);

                        Vector3 diff = Physics.GlobalTransform.Translation - TargetPosition;

                        if (diff.LengthSquared() < 2)
                        {
                            State = BalloonState.Waiting;
                        }
                    }
                    else
                    {
                        TargetPosition = new Vector3(Physics.GlobalTransform.Translation.X, 0, Physics.GlobalTransform.Translation.Z);
                    }

                    break;
                case BalloonState.Leaving:
                    TargetPosition = Vector3.UnitY * 100 + Physics.GlobalTransform.Translation;

                    if (Physics.GlobalTransform.Translation.Y > 50)
                    {
                        Die();
                    }

                    break;
                case BalloonState.Waiting:
                    TargetPosition = Physics.GlobalTransform.Translation;

                    if (!shipmentGiven)
                    {
                        foreach (ResourceAmount amount in Shipment.BuyOrder)
                        {
                            for (int i = 0; i < amount.NumResources; i++)
                            {
                                Vector3 pos = Physics.GlobalTransform.Translation + new Vector3((float)PlayState.random.NextDouble() - 0.5f, (float)PlayState.random.NextDouble() - 0.5f, (float)PlayState.random.NextDouble() - 0.5f) * 2;
                                LocatableComponent loc = EntityFactory.GenerateComponent(amount.ResourceType.ResourceName, pos, Manager, chunks.Content, chunks.Graphics, chunks, Master, camera);
                                Master.AddGatherDesignation(loc);
                                Master.Economy.CurrentMoney -= amount.ResourceType.MoneyValue * Master.Economy.BuyMultiplier;
                            }

                            
                        }


                        shipmentGiven = true;
                    }
                    else
                    {
                        if (Shipment.Destination != null)
                        {
                            foreach (Item i in Shipment.Destination.ListItems())
                            {
                                i.userData.Die();
                                Master.Economy.CurrentMoney += GetSellOrder(i);
                            }
                            Shipment.Destination.ClearItems();
                        }
                        State = BalloonState.Leaving;
                    }


                    break;
            }


            base.Update(gameTime, chunks, camera);
        }

      
    }
}
