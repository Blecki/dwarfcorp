using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp.SteamPipes
{
    public class ElevatorTrack : CraftedFixture
    {
        [EntityFactory("Elevator Track")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var resources = Data.GetData<List<ResourceAmount>>("Resources", null);

            if (resources == null)
                resources = new List<ResourceAmount>() { new ResourceAmount(ResourceType.Wood) };

            return new ElevatorTrack(Manager, Position, resources);
        }

        protected static Dictionary<Resource.ResourceTags, Point> Sprites = new Dictionary<Resource.ResourceTags, Point>()
        {
            {
                Resource.ResourceTags.Metal,
                new Point(3, 8)
            },
            
        };

        protected static Point DefaultSprite = new Point(2, 8);


        public ElevatorTrack()
        {

        }

        public ElevatorTrack(ComponentManager manager, Vector3 position, List<ResourceAmount> resourceType) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new FixtureCraftDetails(manager)
            {
                Resources = resourceType.ConvertAll(p => new ResourceAmount(p)),
                Sprites = Sprites,
                DefaultSpriteFrame = DefaultSprite,
                CraftType = "Elevator Track"
            }, SimpleSprite.OrientMode.Fixed)
        {
            this.LocalBoundingBoxOffset = new Vector3(0, 0, 0.45f);
            this.BoundingBoxSize = new Vector3(0.7f, 1, 0.1f);
            this.SetFlag(Flag.RotateBoundingBox, true);

            Name = "Elevator Track";
            Tags.Add("Climbable");
            OrientToWalls();
            CollisionType = CollisionType.Static;
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            GetComponent<SimpleSprite>().OrientationType = SimpleSprite.OrientMode.Fixed;
            GetComponent<SimpleSprite>().LocalTransform = Matrix.CreateTranslation(new Vector3(0, 0, 0.45f)) * Matrix.CreateRotationY(0.0f);

            var sensor = GetComponent<GenericVoxelListener>();
            sensor.LocalBoundingBoxOffset = new Vector3(0.0f, 0.0f, 1.0f);
            sensor.SetFlag(Flag.RotateBoundingBox, true);
            sensor.PropogateTransforms();

            AddChild(new Flammable(manager, "Flammable"));
        }
    }

}
