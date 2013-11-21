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

    public class Economy
    {
        public float CurrentMoney { get; set; }
        public float BuyMultiplier { get; set; }
        public float SellMultiplier { get; set; }
        public GameMaster Master { get; set; }

        public List<ShipmentOrder> OutstandingOrders { get; set; }
        public List<ShipmentOrder> TravelingOrders { get; set; }

        public Economy(GameMaster master, float currentMoney, float buyMultiplier, float sellMulitiplier)
        {
            CurrentMoney = currentMoney;
            BuyMultiplier = buyMultiplier;
            SellMultiplier = sellMulitiplier;
            OutstandingOrders = new List<ShipmentOrder>();
            TravelingOrders = new List<ShipmentOrder>();
            Master = master;
        }

        public void DispatchBalloon(ShipmentOrder order)
        {
            BoundingBox box = order.Destination.GetBoundingBox();
            Vector3 position = box.Min + (box.Max - box.Min) * 0.5f;
            EntityFactory.CreateBalloon(position, position + Vector3.UnitY * 30, Master.Components, Master.Content, Master.Graphics, order, Master);
        }

        public void Update(GameTime t)
        {
            List<ShipmentOrder> removals = new List<ShipmentOrder>();
            for(int i = 0; i < OutstandingOrders.Count; i++)
            {
                ShipmentOrder order = OutstandingOrders[i];

                if(!order.HasSentResources)
                {
                    order.HasSentResources = true;
                    foreach(ResourceAmount amount in order.SellOrder)
                    {
                        Master.AddShipDesignation(amount, order.Destination);
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