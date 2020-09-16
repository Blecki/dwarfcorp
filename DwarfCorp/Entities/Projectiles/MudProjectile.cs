using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{

    public class SnowballProjectile : Projectile
    {
        [EntityFactory("Snowball")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new SnowballProjectile(
                Manager,
                Position,
                Data.GetData("Velocity", Vector3.Up * 10 * MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)),
                Data.GetData<GameComponent>("Target", null),
                Data.GetData<GameComponent>("Shooter", null));
        }

        public SnowballProjectile()
        {

        }

        public SnowballProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, GameComponent target, GameComponent Shooter) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 30.0f, DamageType = Health.DamageType.Normal }, 0.25f, "Entities\\Golems\\snowball", "snow_particle", ContentPaths.Audio.Oscar.sfx_env_voxel_snow_destroy, target, Shooter, true, true)
        {
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);
            HitAnimation = Library.CreateSimpleAnimation(ContentPaths.Effects.flash);
        }
    }

    public class MudProjectile : Projectile
    {
        [EntityFactory("Mud")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new MudProjectile(
                Manager,
                Position,
                Data.GetData("Velocity", Vector3.Up * 10 * MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)),
                Data.GetData<GameComponent>("Target", null),
                Data.GetData<Creature>("Shooter", null));
        }

        public MudProjectile()
        {

        }

        public MudProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, GameComponent target, GameComponent Shooter) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 30.0f, DamageType = Health.DamageType.Normal }, 0.25f, "Entities\\Golems\\mudball", "dirt_particle", ContentPaths.Audio.gravel, target, Shooter, true, true)
        {
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);
            HitAnimation = Library.CreateSimpleAnimation(ContentPaths.Effects.flash);
        }
    }
}
