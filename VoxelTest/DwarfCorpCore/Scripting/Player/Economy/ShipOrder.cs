using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{

    /// <summary>
    /// This designation specifies that a resource should be shipped to a given port.
    /// Creatures will find a resource from stockpiles and move it to the port.
    /// </summary>
    public class ShipOrder
    {
        public ResourceAmount Resource { get; set; }
        public Room Port { get; set; }
        public List<Task> Assignments { get; set; }

        public ShipOrder(ResourceAmount resource, Room port)
        {
            Resource = resource;
            Port = port;
            Assignments = new List<Task>();
        }

        public int GetRemainingNumResources()
        {
            // TODO: Reimplement
            /*
            List<Item> items = Port.ListItems();

            int count = Assignments.Count + items.Count(i => i.UserData.Tags.Contains(Resource.ResourceType.ResourceName));

            return (int) Math.Max(Resource.NumResources - count, 0);
             */
            return 0;
        }

        public bool IsSatisfied()
        {
            return GetRemainingNumResources() == 0;
        }
    }

}