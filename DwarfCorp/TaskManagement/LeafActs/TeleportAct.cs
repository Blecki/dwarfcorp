using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace DwarfCorp
{
    public class TeleportAct : CreatureAct
    {
        public enum TeleportType
        {
            Jump,
            Lerp,
            Snap
        }
        public Vector3 Location { get; set; }
        public TeleportType Type { get; set; }

        public TeleportAct(CreatureAI creature) :
            base(creature)
        {
            Name = "Teleport";
            Type = TeleportType.Jump;
        }


        public override IEnumerable<Status> Run()
        {
            if ((Creature.Physics.GlobalTransform.Translation - Location).LengthSquared() < 0.5f)
            {
                yield return Act.Status.Success;
                yield break;
            }
            switch (Type)
            {
                case TeleportType.Jump:
                {
                    TossMotion motion = new TossMotion(0.6f, 0.9f, Creature.Physics.GlobalTransform, Location);
                    Creature.Physics.AnimationQueue.Add(motion);
                    SoundManager.PlaySound(ContentPaths.Audio.jump, Creature.Physics.GlobalTransform.Translation);
                    
                    while (!motion.IsDone())
                    {
                        Creature.CurrentCharacterMode = CharacterMode.Falling;
                        yield return Status.Running;
                    }
                    break;
                }    
                case TeleportType.Lerp:
                {
                    EaseMotion motion = new EaseMotion(0.6f, Creature.Physics.GlobalTransform, Location);
                    while (!motion.IsDone())
                    {
                            Creature.Physics.PropogateTransforms();
                        motion.Update(Agent.FrameDeltaTime);
                        Creature.AI.Position = motion.GetTransform().Translation;
                        yield return Status.Running;
                    }
                        Creature.Physics.IsSleeping = false;
                        break;
                }
                case TeleportType.Snap:
                    Creature.AI.Position = Location;
                    break;
            }
            
            yield return Status.Success;
        }
    }

}