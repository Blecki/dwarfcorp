using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DwarfCorp
{

    public class ShipmentOrder
    {
        public List<ResourceAmount> BuyOrder { get; set; }
        public List<ResourceAmount> SellOrder { get; set; }
        public float OrderTotal { get; set; }
        public Timer OrderTimer { get; set; }
        public Room Destination { get; set; }
        public bool HasSentResources { get; set; }

        public ShipmentOrder(float time, Room destination)
        {
            BuyOrder = new List<ResourceAmount>();
            SellOrder = new List<ResourceAmount>();
            OrderTotal = 0.0f;
            OrderTimer = new Timer(time, true);
            Destination = destination;
            HasSentResources = false;
        }
    }

}