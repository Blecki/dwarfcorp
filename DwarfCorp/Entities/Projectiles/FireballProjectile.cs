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
    public class FireballProjectile : Projectile
    {
        [EntityFactory("Fireball")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new FireballProjectile(
                Manager,
                Position,
                Data.GetData("Velocity", Vector3.Up * 10 * MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)),
                Data.GetData<GameComponent>("Target", null));
        }

        public FireballProjectile()
        {
            
        }

        public FireballProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, GameComponent target) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 15.0f, DamageType = Health.DamageType.Fire }, 0.25f, ContentPaths.Particles.fireball, "flame", ContentPaths.Audio.Oscar.sfx_ic_demon_fire_hit_1, target)
        {
            Sprite.LightsWithVoxels = false;
            Sprite2.LightsWithVoxels = false;

            HitAnimation = Library.CreateSimpleAnimation(ContentPaths.Effects.pierce);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);
            HitAnimation = Library.CreateSimpleAnimation(ContentPaths.Effects.pierce);
        }
    }
}
