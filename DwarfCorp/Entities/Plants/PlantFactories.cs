using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static class PlantFactories
    {
        #region Haunted Tree
        [EntityFactory("Haunted Tree")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Haunted Tree", Manager, Position, "Entities\\Plants\\eviltree", ResourceType.EvilSeed, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Haunted Tree Sprout")]
        private static GameComponent __factory01(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Haunted Tree", Position, "Entities\\Plants\\eviltree-sprout")
            {
                GrowthHours = 24.0f,
                MaxSize = 2.0f,
                GoodBiomes = "Haunted Forest Waste",
                BadBiomes = "Tiaga Tundra Desert"
            };
        }
        #endregion

        #region Pine Tree
        [EntityFactory("Pine Tree")]
        private static GameComponent __factory02(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Pine Tree", Manager, Position, "Entities\\Plants\\pinetree", ResourceType.PineCone, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Pine Tree Sprout")]
        private static GameComponent __factory03(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Pine Tree", Position, "Entities\\Plants\\pinetree-sprout")
            {
                GrowthHours = 24.0f,
                MaxSize = 2.0f,
                GoodBiomes = "Tiaga Boreal Forest Deciduous Forest Jolly Forest",
                BadBiomes = "Tundra Desert Waste"
            };
        }
        #endregion

        #region Snow Pine Tree
        [EntityFactory("Snow Pine Tree")]
        private static GameComponent __factory04(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Pine Tree", Manager, Position, "Entities\\Plants\\snowpine", ResourceType.PineCone, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Snow Pine Tree Sprout")]
        private static GameComponent __factory05(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Snow Pine Tree", Position, "Entities\\Plants\\pinetree-sprout")
            {
                GrowthHours = 24.0f,
                MaxSize = 2.0f,
                GoodBiomes = "Tiaga Boreal Forest",
                BadBiomes = "Desert Tundra Waste Jungle"
            };
        }
        #endregion

        #region Candycane
        [EntityFactory("Candycane")]
        private static GameComponent __factory06(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Candycane", Manager, Position, "Entities\\Plants\\candycane", ResourceType.Peppermint, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Snow Candycane")]
        private static GameComponent __factory06b(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Candycane", Manager, Position, "Entities\\Plants\\candycane-snow", ResourceType.Peppermint, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Candycane Sprout")]
        private static GameComponent __factory07(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Candycane", Position, "Entities\\Plants\\candycane-sprout")
            {
                GrowthHours = 24.0f,
                MaxSize = 2.0f,
                GoodBiomes = "Tiaga Jolly Forest",
                BadBiomes = "Desert Tundra Waste Haunted Forest"
            };
        }
        #endregion

        #region Palm Tree
        [EntityFactory("Palm Tree")]
        private static GameComponent __factory08(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Palm Tree", Manager, Position, "Entities\\Plants\\palmtree", ResourceType.Coconut, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Palm Tree Sprout")]
        private static GameComponent __factory09(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Palm Tree", Position, "Entities\\Plants\\palmtree-sprout")
            {
                GrowthHours = 24.0f,
                MaxSize = 2.0f,
                GoodBiomes = "Jungle Desert",
                BadBiomes = "Tiaga Tundra Waste Jolly Forest"
            };
        }
        #endregion

        #region Apple Tree
        [EntityFactory("Apple Tree")]
        private static GameComponent __factory0A(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Apple Tree", Manager, Position, "Entities\\Plants\\appletree", ResourceType.Apple, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Apple Tree Sprout")]
        private static GameComponent __factory0B(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Apple Tree", Position, "Entities\\Plants\\appletree-sprout")
            {
                GrowthHours = 24.0f,
                MaxSize = 2.0f,
                GoodBiomes = "Grassland Deciduous Forest",
                BadBiomes = "Desert Tundra Tiaga Jolly Forest Waste"
            };
        }
        #endregion

        #region Cactus
        [EntityFactory("Cactus")]
        private static GameComponent __factory0C(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Cactus(Manager, Position, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Cactus Sprout")]
        private static GameComponent __factory0D(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Cactus", Position, "Entities\\Plants\\cactus-sprout")
            {
                GrowthHours = 12.0f,
                MaxSize = 0.75f,
                GoodBiomes = "Desert",
                BadBiomes = "Boreal Forest Deciduous Forest Tiaga Tundra Jungle Waste Haunted Forest Jolly Forest"
            };
        }
        #endregion

        #region Pumpkin
        [EntityFactory("Pumpkin")]
        private static GameComponent __factory0E(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Pumpkin(Manager, Position, "Entities\\Plants\\pumpkinvine", Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Pumpkin Sprout")]
        private static GameComponent __factory0F(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Pumpkin", Position, "Entities\\Plants\\pumpkinvine-sprout")
            {
                GrowthHours = 6.0f,
                MaxSize = 0.5f,
                GoodBiomes = "Grassland Deciduous Forest Boreal Forest Haunted Forest",
                BadBiomes = "Desert Waste"
            };
        }
        #endregion

        #region Berry Bush
        [EntityFactory("Berry Bush")]
        private static GameComponent __factory10(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Bush(Manager, Position, "Entities\\Plants\\berrybush", Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Berry Bush Sprout")]
        private static GameComponent __factory11(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Berry Bush", Position, "Entities\\Plants\\berrybush-sprout")
            {
                GrowthHours = 24.0f,
                MaxSize = 1.0f,
                GoodBiomes = "GrassLand Deciduous Forest Jolly Forest",
                BadBiomes = "Desert Tiaga Tundra Waste"
            };
        }
        #endregion

        #region Mushroom
        [EntityFactory("Mushroom")]
        private static GameComponent __factory12(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Mushroom(Manager, Position, "Entities\\Plants\\mushroom", ResourceType.Mushroom, 2, false);
        }

        [EntityFactory("Mushroom Sprout")]
        private static GameComponent __factory13(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Mushroom", Position, "Entities\\Plants\\mushroom-sprout")
            {
                GrowthHours = 6.0f,
                MaxSize = 0.25f
            };
        }
        #endregion

        #region Cave Mushroom
        [EntityFactory("Cave Mushroom")]
        private static GameComponent __factory14(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Mushroom(Manager, Position, "Entities\\Plants\\caveshroom", ResourceType.CaveMushroom, 4, true);
        }

        [EntityFactory("Cave Mushroom Sprout")]
        private static GameComponent __factory15(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Cave Mushroom", Position, "Entities\\Plants\\caveshroom-sprout")
            {
                GrowthHours = 6.0f,
                MaxSize = 0.25f
            };
        }
        #endregion

        #region Wheat
        [EntityFactory("Wheat")]
        private static GameComponent __factory16(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Wheat(Manager, Position);
        }

        [EntityFactory("Wheat Sprout")]
        private static GameComponent __factory17(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Wheat", Position, "Entities\\Plants\\wheat-sprout")
            {
                GrowthHours = 12.0f,
                MaxSize = 1.0f,
                GoodBiomes = "Grassland",
                BadBiomes = "Desert Tiaga Tundra Waste Haunted Forest Jolly Forest"
            };
        }
        #endregion
    }
}
