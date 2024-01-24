using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    /// <summary>
    /// This component follows its parent at a specified radius;
    /// </summary>
    public class Follower : GameComponent
    {
        public float FollowRadius { get; set;  }
        public Vector3 TargetPos { get; set; }
        public float FollowRate { get; set; }

        public Follower()
        {

        }

        public Follower(ComponentManager Manager) :
            base(Manager, "Follower", Matrix.Identity, Vector3.One, Vector3.Zero)
        {
            FollowRadius = 1.5f;
            FollowRate = 0.1f;
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (Parent.HasValue(out var body))
            {
                Vector3 parentCurrentPos = body.Position;
                if ((parentCurrentPos - TargetPos).Length() > FollowRadius)
                {
                    TargetPos = parentCurrentPos;
                }
                Vector3 newPos = (Position * (1.0f - FollowRate) + TargetPos * (FollowRate));
                Matrix newTransform = GlobalTransform;
                newTransform.Translation = newPos;
                newTransform = newTransform * Matrix.Invert(body.GlobalTransform);
                LocalTransform = newTransform;
            }
        }
    }

}