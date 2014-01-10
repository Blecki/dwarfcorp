using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    /// <summary>
    /// Controls how much money the player has, and whether the player can
    /// buy and sell certain things. Controls balloon shipments.
    /// </summary>
    public class Economy
    {
        public float CurrentMoney { get; set; }
        public float BuyMultiplier { get; set; }
        public float SellMultiplier { get; set; }
        public Faction Faction { get; set; }

        public List<ShipmentOrder> OutstandingOrders { get; set; }
        public List<ShipmentOrder> TravelingOrders { get; set; }

        public Economy(Faction faction, float currentMoney, float buyMultiplier, float sellMulitiplier)
        {
            CurrentMoney = currentMoney;
            BuyMultiplier = buyMultiplier;
            SellMultiplier = sellMulitiplier;
            OutstandingOrders = new List<ShipmentOrder>();
            TravelingOrders = new List<ShipmentOrder>();
            Faction = faction;
        }

        public void DispatchBalloon(ShipmentOrder order)
        {
            BoundingBox box = order.Destination.GetBoundingBox();
            Vector3 position = box.Min + (box.Max - box.Min) * 0.5f;
            EntityFactory.CreateBalloon(position, position + Vector3.UnitY * 30, PlayState.ComponentManager, PlayState.ChunkManager.Content, PlayState.ChunkManager.Graphics, order, Faction);
        }

        public void Update(GameTime t)
        {
            List<ShipmentOrder> removals = new List<ShipmentOrder>();
            foreach(ShipmentOrder order in OutstandingOrders)
            {
                if(!order.HasSentResources)
                {
                    order.HasSentResources = true;
                    foreach(ResourceAmount amount in order.SellOrder)
                    {
                        Faction.AddShipDesignation(amount, order.Destination);
                    }
                }

                if(!order.OrderTimer.HasTriggered)
                {
                    order.OrderTimer.Update(t);
                }
                else
                {
                    removals.Add(order);
                }
            }

            foreach(ShipmentOrder order in removals)
            {
                OutstandingOrders.Remove(order);
                TravelingOrders.Add(order);
                DispatchBalloon(order);
            }
        }
    }

}