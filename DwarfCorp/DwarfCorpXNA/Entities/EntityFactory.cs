// EntityFactory.cs
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
using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This class is used to create entities. It should probably be replaced with a more modular system (or a set of data files)
    /// Right now, its just an ugly class for initializing most of the entities in the game.
    /// </summary>
    internal class EntityFactory
    {
        public static WorldManager World = null;
        private static ComponentManager Components { get { return World.ComponentManager; } }
        private static List<Action> LazyActions = new List<Action>();

        public static Dictionary<string, Func<Vector3, Blackboard, GameComponent>> EntityFuncs { get; set; }

        // This exists in case we want to call the entity factory from  a thread, allowing us
        // to lazy-load entities later.
        public static void DoLazyActions()
        {
            foreach (var func in LazyActions)
            {
                if (func != null)
                    func.Invoke();
            }
            LazyActions.Clear();
        }

        public static void Initialize(WorldManager world)
        {
            World = world;
            RegisterEntity("Crate", (position, data) => new Crate(world.ComponentManager, position));
            RegisterEntity("Balloon", (position, data) => CreateBalloon(position + new Vector3(0, 1000, 0), position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, null, world.PlayerFaction));
            RegisterEntity("Work Pile", (position, data) => new WorkPile(world.ComponentManager, position));

            RegisterEntity("Haunted Tree", (position, data) => new Tree("Haunted Tree", world.ComponentManager, position, "eviltree", ResourceLibrary.ResourceType.EvilSeed, data.GetData("Scale", 1.0f), "eviltreesprout"));
            RegisterEntity("Haunted Tree Sprout", (position, data) => new Seedling(world.ComponentManager, "Haunted Tree", position, "eviltreesprout", 12));

            RegisterEntity("Pine Tree", (position, data) => new Tree("Pine Tree", world.ComponentManager, position, "pine", ResourceLibrary.ResourceType.PineCone, data.GetData("Scale", 1.0f), "pinesprout"));
            RegisterEntity("Pine Tree Sprout", (position, data) => new Seedling(world.ComponentManager, "Pine Tree", position, "pinesprout", 12));
            RegisterEntity("Snow Pine Tree", (position, data) => new Tree("Pine Tree", world.ComponentManager, position, "snowpine", ResourceLibrary.ResourceType.PineCone, data.GetData("Scale", 1.0f), "pinesprout"));
            RegisterEntity("Snow Pine Tree Sprout", (position, data) => new Seedling(world.ComponentManager, "Snow Pine Tree", position, "pinesprout", 12));

            RegisterEntity("Palm Tree", (position, data) => new Tree("Palm Tree", world.ComponentManager, position, "palm", ResourceLibrary.ResourceType.Coconut, data.GetData("Scale", 1.0f), "palmsprout"));
            RegisterEntity("Palm Tree Sprout", (position, data) => new Seedling(world.ComponentManager, "Palm Tree", position, "palmsprout", 12));

            RegisterEntity("Apple Tree", (position, data) => new Tree("Apple Tree", world.ComponentManager, position, "appletree", ResourceLibrary.ResourceType.Apple, data.GetData("Scale", 1.0f), "appletreesprout"));
            RegisterEntity("Apple Tree Sprout", (position, data) => new Seedling(world.ComponentManager, "Apple Tree", position, "appletreesprout", 12));

            RegisterEntity("Cactus", (position, data) => new Cactus(world.ComponentManager, position, "cactus", data.GetData("Scale", 1.0f)));
            RegisterEntity("Cactus Sprout", (position, data) => new Seedling(world.ComponentManager, "Cactus", position, "cactussprout", 12));

            RegisterEntity("Pumpkin", (position, data) => new Pumpkin(world.ComponentManager, position, "pumpkinvine", data.GetData("Scale", 1.0f)));
            RegisterEntity("Pumpkin Sprout", (position, data) => new Seedling(world.ComponentManager, "Pumpkin", position, "pumpkinvinesprout", 12));

            RegisterEntity("Berry Bush", (position, data) => new Bush(world.ComponentManager, position, "berrybush", data.GetData("Scale", 1.0f)));
            RegisterEntity("Berry Bush Sprout", (position, data) => new Seedling(world.ComponentManager, "Berry Bush", position, "berrybushsprout", 12));

            RegisterEntity("Bird", (position, data) => new Bird(ContentPaths.Entities.Animals.Birds.GetRandomBird(), position, world.ComponentManager, "Bird"));
            RegisterEntity("Bat", (position, data) => new Bat(world.ComponentManager, position));
            RegisterEntity("Scorpion", (position, data) => new Scorpion(ContentPaths.Entities.Animals.Scorpion.scorption_animation, position, world.ComponentManager, "Scorpion"));
            RegisterEntity("Spider", (position, data) => new Spider(world.ComponentManager, ContentPaths.Entities.Animals.Spider.spider_animation, position));
            RegisterEntity("Frog", (position, data) => new Frog(ContentPaths.Entities.Animals.Frog.frog0_animation, position, world.ComponentManager, "Frog"));
            RegisterEntity("Tree Frog", (position, data) => new Frog(ContentPaths.Entities.Animals.Frog.frog1_animation, position, world.ComponentManager, "Frog"));
            RegisterEntity("Brown Rabbit", (position, data) => new Rabbit(ContentPaths.Entities.Animals.Rabbit.rabbit0_animation, position, world.ComponentManager, "Brown Rabbit"));
            RegisterEntity("White Rabbit", (position, data) => new Rabbit(ContentPaths.Entities.Animals.Rabbit.rabbit1_animation, position, world.ComponentManager, "White Rabbit"));
            RegisterEntity("Deer", (position, data) => new Deer(ContentPaths.Entities.Animals.Deer.deer, position, world.ComponentManager, "Deer"));
            RegisterEntity("Dwarf", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.Worker], 0));
            //RegisterEntity("TestDwarf", (position, data) => GenerateTestDwarf(position));
            //RegisterEntity("TestGoblin", (position, data) => GenerateTestGoblin(position));
            //RegisterEntity("TestSkeleton", (position, data) => GenerateTestSeketon(position));
            //RegisterEntity("TestMoleman", (position, data) => GenerateTestMoleman(position));
            RegisterEntity("AxeDwarf", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.AxeDwarf], 0));
            RegisterEntity("CraftsDwarf", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.CraftsDwarf], 0));
            RegisterEntity("Wizard", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.Wizard], 0));
            RegisterEntity("MusketDwarf", (position, data) => GenerateDwarf(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, world.PlayerFaction, world.PlanService, "Player", JobLibrary.Classes[JobLibrary.JobType.MusketDwarf], 0));
            RegisterEntity("Moleman", (position, data) => GenerateMoleman(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, World.Factions.Factions["Molemen"], world.PlanService, "Molemen"));
            RegisterEntity("Goblin", (position, data) => GenerateGoblin(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, World.Factions.Factions["Goblins"], world.PlanService, "Goblins"));
            RegisterEntity("Skeleton", (position, data) => GenerateSkeleton(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, World.Factions.Factions["Undead"], world.PlanService, "Undead"));
            RegisterEntity("Necromancer", (position, data) => GenerateNecromancer(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice, world.ChunkManager, world.Camera, World.Factions.Factions["Undead"], world.PlanService, "Undead"));
            RegisterEntity("Bed", (position, data) => new Bed(world.ComponentManager, position));
            RegisterEntity("Barrel", (position, data) => new Barrel(world.ComponentManager, position));
            RegisterEntity("Bear Trap", (position, data) => new BearTrap(world.ComponentManager, position));
            RegisterEntity("Lamp", (position, data) => new Lamp(world.ComponentManager, position));
            RegisterEntity("Table", (position, data) => new Table(world.ComponentManager, position));
            RegisterEntity("Chair", (position, data) => new Chair(world.ComponentManager, position));
            RegisterEntity("Flag", (position, data) => new Flag(world.ComponentManager, position, world.PlayerCompany.Information));

            RegisterEntity("Mushroom", (position, data) => new Mushroom(world.ComponentManager, position, ContentPaths.Entities.Plants.mushroom, ResourceLibrary.ResourceType.Mushroom, 2, false, "mushroomsprout"));
            RegisterEntity("Mushroom Sprout", (position, data) => new Seedling(world.ComponentManager, "Mushroom", position, "mushroomsprout", 12));

            RegisterEntity("Cave Mushroom", (position, data) => new Mushroom(world.ComponentManager, position, ContentPaths.Entities.Plants.cavemushroom, ResourceLibrary.ResourceType.CaveMushroom, 4, true, "cavemushroomsprout"));
            RegisterEntity("Cave Mushroom Sprout", (position, data) => new Seedling(world.ComponentManager, "Cave Mushroom", position, "cavemushroomsprout", 12));

            RegisterEntity("Wheat", (position, data) => new Wheat(world.ComponentManager, position));
            RegisterEntity("Wheat Sprout", (position, data) => new Seedling(world.ComponentManager, "Wheat", position, "wheatsprout", 12));


            RegisterEntity("Kitchen Table", (position, data) => new Table(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(0, 7)) { Tags = new List<string>() { "Cutting Board" } });
            RegisterEntity("Books", (position, data) => new Table(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(0, 4)) { Tags = new List<string>() { "Research" }, Battery = new Table.ManaBattery() { Charge = 0.0f, MaxCharge = 100.0f } });
            RegisterEntity("Potions", (position, data) => new Table(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(1, 4)) { Tags = new List<string>() { "Research" }, Battery = new Table.ManaBattery() { Charge = 0.0f, MaxCharge = 100.0f } });
            RegisterEntity("Anvil", (position, data) => new Anvil(world.ComponentManager, position));
            RegisterEntity("Forge", (position, data) => new Forge(world.ComponentManager, position));
            RegisterEntity("Elf", (position, data) => GenerateElf(world, position, World.Factions.Factions["Elf"], "Elf"));
            RegisterEntity("Demon", (position, data) => GenerateDemon(world, position, World.Factions.Factions["Demon"], "Demon"));
            RegisterEntity("Arrow", (position, data) => new ArrowProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Bullet", (position, data) => new BulletProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Web", (position, data) => new WebProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Fireball", (position, data) => new FireballProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Fairy", (position, data) => new Fairy(world.ComponentManager, "Player", position));
            RegisterEntity("Target", (position, data) => new Target(world.ComponentManager, position));
            RegisterEntity("Stove", (position, data) => new Stove(world.ComponentManager, position));
            RegisterEntity("Strawman", (position, data) =>
            {
                float value = (float)MathFunctions.Random.NextDouble();
                return value < 0.33
                    ? (Body)(new Strawman(world.ComponentManager, position))
                    : (value < 0.66 ? (Body)(new WeightRack(world.ComponentManager, position)) : (Body)(new PunchingBag(world.ComponentManager, position)));
            });
            RegisterEntity("Snake", (position, data) => GenerateSnake(position, world.ComponentManager, GameState.Game.Content, GameState.Game.GraphicsDevice,
                world.ChunkManager));
            RegisterEntity("Necrosnake", (position, data) =>
            {
                var snek = new Snake(new SpriteSheet(ContentPaths.Entities.Animals.Snake.bonesnake, 32),
                position, world.ComponentManager, "Snake");
                snek.Attacks[0].DiseaseToSpread = "Necrorot";
                return snek.Physics;
            });
            RegisterEntity("Bookshelf", (position, data) => new Bookshelf(world.ComponentManager, position) { Tags = new List<string>() { "Research"}});
            RegisterEntity("Wooden Door", (position, data) => new Door(world.ComponentManager, position, world.PlayerFaction, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 1), 50));
            RegisterEntity("Metal Door", (position, data) => new Door(world.ComponentManager, position, world.PlayerFaction, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 8), 100));
            RegisterEntity("Stone Door", (position, data) => new Door(world.ComponentManager, position, world.PlayerFaction, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 8), 75));
            RegisterEntity("Wooden Ladder", (position, data) => new Ladder(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 0)));
            RegisterEntity("Stone Ladder", (position, data) => new Ladder(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(2, 8)));
            RegisterEntity("Metal Ladder", (position, data) => new Ladder(world.ComponentManager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 8)));
            RegisterEntity("RandTrinket", (position, data) => CreateRandomTrinket(world, position));
            RegisterEntity("RandFood", (position, data) => CreateRandomFood(world, position));
            RegisterEntity("Turret", (position, data) => new TurretTrap(world.ComponentManager, position, world.PlayerFaction));
            RegisterEntity("Snow Cloud", (position, data) => new Cloud(world.ComponentManager, 0.1f, 50, 40, position) { TypeofStorm = StormType.SnowStorm });
            RegisterEntity("Rain Cloud", (position, data) => new Cloud(world.ComponentManager, 0.1f, 50, 40, position) { TypeofStorm = StormType.RainStorm });
            RegisterEntity("Storm", (position, data) =>
            {
                Weather.CreateForecast(world.Time.CurrentDate, world.ChunkManager.Bounds, world, 3);
                Weather.CreateStorm(MathFunctions.RandVector3Cube() * 10, MathFunctions.Rand(0.05f, 1.0f), world);
                return new Cloud(world.ComponentManager, 0.1f, 50, 40, position);
            });
            RegisterEntity("Chicken", (position, data) => new Chicken(position, world.ComponentManager, "Chicken"));
            RegisterEntity("MudGolem", (position, data) => new MudGolem(new CreatureStats(new MudGolemClass(), 0), "Carnivore", world.PlanService, World.Factions.Factions["Carnivore"], world.ComponentManager, "Mud Golem", position));
            RegisterEntity("Mud", (position, data) => new MudProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Grave", (position, data) => new Grave(world.ComponentManager, position));
            RegisterEntity("Coins", (position, data) => new CoinPileFixture(world.ComponentManager, position));
        }

        private static GameComponent CreateRandomFood(WorldManager world, Vector3 position)
        {
            IEnumerable<Resource> foods = ResourceLibrary.GetResourcesByTag(Resource.ResourceTags.RawFood);

            Resource randresource = ResourceLibrary.CreateMeal(Datastructures.SelectRandom(foods).Type,
                Datastructures.SelectRandom(foods).Type);
            return new ResourceEntity(world.ComponentManager, new ResourceAmount(randresource.Type), position);
        }


        public static ResourceEntity CreateRandomTrinket(WorldManager world, Vector3 pos)
        {
            Resource randResource = ResourceLibrary.GenerateTrinket(Datastructures.SelectRandom(ResourceLibrary.Resources.Where(r => r.Value.Tags.Contains(Resource.ResourceTags.Material))).Key, MathFunctions.Rand(0.1f, 3.5f));

            if (MathFunctions.RandEvent(0.5f))
            {
                randResource = ResourceLibrary.EncrustTrinket(randResource.Type, Datastructures.SelectRandom(ResourceLibrary.Resources.Where(r => r.Value.Tags.Contains(Resource.ResourceTags.Gem))).Key);
            }

            return new ResourceEntity(world.ComponentManager, new ResourceAmount(randResource.Type), pos);
        }

        public static void RegisterEntity<T>(string id, Func<Vector3, Blackboard, T> function) where T : GameComponent
        {
            if (EntityFuncs == null)
            {
                EntityFuncs = new Dictionary<string, Func<Vector3, Blackboard, GameComponent>>();
            }
            EntityFuncs[id] = function;
        }

        // Create an entity and make it a transparent ghost object that doesn't interact with anything.
        // This is for displaying stuff, for example in tools.
        public static Body CreateGhostedEntity<T>(string id, Vector3 location, Color tint, Blackboard data = null) where T : Body
        {
            var ent = CreateEntity<T>(id, location, data);
            ent.SetFlagRecursive(GameComponent.Flag.Active, false);
            var tinters = ent.EnumerateAll().OfType<Tinter>();

            foreach (var tinter in tinters)
            {
                tinter.VertexColorTint = tint;
            }
            return ent;
        }

        public static T CreateEntity<T>(string id, Vector3 location, Blackboard data = null) where T : GameComponent
        {
            if (data == null) data = new Blackboard();
            if (EntityFuncs.ContainsKey(id))
            {
                var r = EntityFuncs[id].Invoke(location, data);
                // Todo: This is a hack. Creatures create a physics component and add themselves to it. 
                // Instead heirarchy should be creature -> physics -> everything else.
                Components.RootComponent.AddChild(r.Parent == null ? r : r.Parent);
                return r as T;
            }
            else
            {
                string err = id ?? "null";
                throw new KeyNotFoundException("Unable to create entity of type " + err);
            }
        }

        public static void DoLazy(Action action)
        {
            LazyActions.Add(action);
        }

        public static Body CreateBalloon(Vector3 target, Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ShipmentOrder order, Faction master)
        {
            var balloon = new Body(componentManager, "Balloon",
                Matrix.CreateTranslation(position), new Vector3(0.5f, 1, 0.5f), new Vector3(0, -2, 0));

            SpriteSheet tex = new SpriteSheet(ContentPaths.Entities.Balloon.Sprites.balloon);
            List<Point> points = new List<Point>
            {
                new Point(0, 0)
            };
            Animation balloonAnimation = new Animation(graphics, new SpriteSheet(ContentPaths.Entities.Balloon.Sprites.balloon), "balloon", points, false, Color.White, 0.001f, false);
            Sprite sprite = balloon.AddChild(new Sprite(componentManager, "sprite", Matrix.Identity, tex, false)
            {
                OrientationType = Sprite.OrientMode.Spherical
            }) as Sprite;
            sprite.AddAnimation(balloonAnimation);

            Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
            balloon.AddChild(new Shadow(componentManager, "shadow", shadowTransform, new SpriteSheet(ContentPaths.Effects.shadowcircle)));
            balloon.AddChild(new BalloonAI(componentManager, target, order, master));
            balloon.AddChild(new MinimapIcon(componentManager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 2, 0)));

            return balloon;
        }

        public static Body GenerateSkeleton(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ChunkManager chunks, Camera camera, Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats(new SkeletonClass(), 0);
            return new Skeleton(stats, allies, planService, faction, componentManager, "Skeleton", position).Physics;
        }


        public static Body GenerateNecromancer(Vector3 position, ComponentManager componentManager, ContentManager content, GraphicsDevice graphics, ChunkManager chunks, Camera camera, Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats(new NecromancerClass(), 0);
            return new Necromancer(stats, allies, planService, faction, componentManager, "Necromancer", position).Physics;
        }

        public static Body GenerateElf(WorldManager worldManger, Vector3 position, Faction faction, string allies)
        {
            CreatureStats stats = new CreatureStats(new ElfClass(), 0);
            return new Elf(stats, allies, worldManger.PlanService, faction, worldManger.ComponentManager, "Elf", position).Physics;
        }


        public static Body GenerateDemon(WorldManager worldManager, Vector3 position, Faction faction, string allies)
        {
            CreatureStats stats = new CreatureStats(new DemonClass(), 0);
            return new Demon(stats, allies, worldManager.PlanService, faction, worldManager.ComponentManager, "Demon", position).Physics;
        }

        public static Body GenerateGoblin(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager, Camera camera,
            Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats(new SwordGoblinClass(), 0);
            return new Goblin(stats, allies, planService, faction, componentManager, "Goblin",  position).Physics;
        }

        public static Body GenerateMoleman(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager, Camera camera,
            Faction faction, PlanService planService, string allies)
        {
            CreatureStats stats = new CreatureStats(new MolemanMinerClass(), 0);
            return new Moleman(stats, allies, planService, faction, componentManager, "Moleman", position).Physics;
        }


        public static GameComponent GenerateDwarf(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunkManager, Camera camera,
            Faction faction, PlanService planService, string allies, EmployeeClass dwarfClass, int level)
        {
            CreatureStats stats = new CreatureStats(dwarfClass, level);
            Dwarf toReturn = new Dwarf(componentManager, stats, allies, planService, faction, "Dwarf", dwarfClass, position);
            toReturn.AI.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, componentManager.World.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        public static GameComponent GenerateSnake(Vector3 position,
            ComponentManager componentManager,
            ContentManager content,
            GraphicsDevice graphics,
            ChunkManager chunks)
        {
            return new Snake(new SpriteSheet(ContentPaths.Entities.Animals.Snake.snake, 32),
                position, componentManager, "Snake").Physics;
        }
    }
}
