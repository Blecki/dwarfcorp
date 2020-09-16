using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class WebProjectile : Projectile
    {
        [EntityFactory("Web")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new WebProjectile(
                Manager,
                Position,
                Data.GetData("Velocity", Vector3.Up * 10 * MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)),
                Data.GetData<GameComponent>("Target", null),
                Data.GetData<Creature>("Shooter", null));
        }

        public WebProjectile()
        {

        }

        public WebProjectile(ComponentManager manager, Vector3 position, Vector3 initialVelocity, GameComponent target, GameComponent Shooter) :
            base(manager, position, initialVelocity, new Health.DamageAmount() { Amount = 10.0f, DamageType = Health.DamageType.Acid }, 0.25f, ContentPaths.Entities.Animals.Spider.webshot, "puff", ContentPaths.Audio.whoosh, target, Shooter)
        {
            HitAnimation = Library.CreateSimpleAnimation(ContentPaths.Entities.Animals.Spider.webstick);
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);
            HitAnimation = Library.CreateSimpleAnimation(ContentPaths.Entities.Animals.Spider.webstick);
        }
    }
}
