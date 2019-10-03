using System;
using System.Collections.Generic;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Flag : CraftedBody
    {
        [EntityFactory("Flag")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new DwarfCorp.Flag(Manager, Position, Manager.World.PlayerFaction.Economy.Information, Data.GetData<Resource>("Resource", null));
        }

        public CompanyInformation Logo;

        public Flag()
        {

        }

        public Flag(ComponentManager Manager, Vector3 position, CompanyInformation logo, Resource Resource) :
            base(Manager, "Flag", Matrix.CreateTranslation(position), new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new CraftDetails(Manager, Resource))
        {
            this.Logo = logo;

            Tags.Add("Flag");
            CollisionType = CollisionType.Static;
            CreateCosmeticChildren(Manager);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);

            AddChild(new SimpleSprite(Manager, "sprite", Matrix.Identity,
                new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32),
                new Point(0, 2))
            {
                OrientationType = SimpleSprite.OrientMode.YAxis
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new Banner(Manager)
            {
                Logo = Logo
            }).SetFlag(Flag.ShouldSerialize, false);

            AddChild(new GenericVoxelListener(Manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);
        }
    }
}
