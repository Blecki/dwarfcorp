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
    public class BulletProjectile : Projectile
    {
        [EntityFactory("Bullet")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new BulletProjectile(
                Manager,
                Position,
                Data.GetData("Velocity", Vector3.Up * 10 * MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)),
                Data.GetData<GameComponent>("Target", null),
                Data.GetData<Creature>("Shooter", null));
        }

        public BulletProjectile()
        {

        }

        public BulletProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, GameComponent target, GameComponent Shooter) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 30.0f, DamageType = Health.DamageType.Normal }, 0.25f, ContentPaths.Particles.stone_particle, null, ContentPaths.Audio.Oscar.sfx_ic_dwarf_musket_bullet_explode_1, target, Shooter)
        {
            HitAnimation = Library.CreateSimpleAnimation(ContentPaths.Effects.explode);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);
            HitAnimation = Library.CreateSimpleAnimation(ContentPaths.Effects.explode);
        }
    }
}
