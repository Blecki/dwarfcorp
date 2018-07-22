// PlantFactories.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static class PlantFactories
    {
        private static int GetPlantGrowthTime()
        {
            // In order to make the mechanics affecting plant growth useful to the player, this can't be random.
            return 12; // MathFunctions.RandInt(12, 48);
        }

        [EntityFactory("Haunted Tree")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Haunted Tree", Manager, Position, "eviltree", ResourceType.EvilSeed, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Haunted Tree Sprout")]
        private static GameComponent __factory01(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Haunted Tree", Position, "eviltreesprout", GetPlantGrowthTime());
        }


        [EntityFactory("Pine Tree")]
        private static GameComponent __factory02(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Pine Tree", Manager, Position, "pine", ResourceType.PineCone, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Pine Tree Sprout")]
        private static GameComponent __factory03(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Pine Tree", Position, "pinesprout", GetPlantGrowthTime());
        }


        [EntityFactory("Snow Pine Tree")]
        private static GameComponent __factory04(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Pine Tree", Manager, Position, "snowpine", ResourceType.PineCone, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Snow Pine Tree Sprout")]
        private static GameComponent __factory05(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Snow Pine Tree", Position, "pinesprout", GetPlantGrowthTime());
        }


        [EntityFactory("Candycane")]
        private static GameComponent __factory06(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Candycane", Manager, Position, "candycane", ResourceType.Peppermint, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Candycane Sprout")]
        private static GameComponent __factory07(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Candycane", Position, "candycanesprout", GetPlantGrowthTime());
        }


        [EntityFactory("Palm Tree")]
        private static GameComponent __factory08(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Palm Tree", Manager, Position, "palm", ResourceType.Coconut, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Palm Tree Sprout")]
        private static GameComponent __factory09(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Palm Tree", Position, "palmsprout", GetPlantGrowthTime());
        }


        [EntityFactory("Apple Tree")]
        private static GameComponent __factory0A(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Tree("Apple Tree", Manager, Position, "appletree", ResourceType.Apple, Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Apple Tree Sprout")]
        private static GameComponent __factory0B(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Apple Tree", Position, "appletreesprout", GetPlantGrowthTime());
        }


        [EntityFactory("Cactus")]
        private static GameComponent __factory0C(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Cactus(Manager, Position, "cactus", Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Cactus Sprout")]
        private static GameComponent __factory0D(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Cactus", Position, "cactussprout", GetPlantGrowthTime());
        }


        [EntityFactory("Pumpkin")]
        private static GameComponent __factory0E(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Pumpkin(Manager, Position, "pumpkinvine", Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Pumpkin Sprout")]
        private static GameComponent __factory0F(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Pumpkin", Position, "pumpkinvinesprout", GetPlantGrowthTime());
        }


        [EntityFactory("Berry Bush")]
        private static GameComponent __factory10(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Bush(Manager, Position, "berrybush", Data.GetData("Scale", 1.0f));
        }

        [EntityFactory("Berry Bush Sprout")]
        private static GameComponent __factory11(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Berry Bush", Position, "berrybushsprout", GetPlantGrowthTime());
        }


        [EntityFactory("Mushroom")]
        private static GameComponent __factory12(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Mushroom(Manager, Position, ContentPaths.Entities.Plants.mushroom, ResourceType.Mushroom, 2, false);
        }

        [EntityFactory("Mushroom Sprout")]
        private static GameComponent __factory13(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Mushroom", Position, "mushroomsprout", GetPlantGrowthTime());
        }


        [EntityFactory("Cave Mushroom")]
        private static GameComponent __factory14(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Mushroom(Manager, Position, ContentPaths.Entities.Plants.caveshroom, ResourceType.CaveMushroom, 4, true);
        }

        [EntityFactory("Cave Mushroom Sprout")]
        private static GameComponent __factory15(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Cave Mushroom", Position, "caveshroomsprout", GetPlantGrowthTime());
        }


        [EntityFactory("Wheat")]
        private static GameComponent __factory16(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Wheat(Manager, Position);
        }

        [EntityFactory("Wheat Sprout")]
        private static GameComponent __factory17(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new Seedling(Manager, "Wheat", Position, "wheatsprout", GetPlantGrowthTime());
        }
    }
}
