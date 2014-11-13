using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature attacks a voxel until it is destroyed.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
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
            switch (Type)
            {
                case TeleportType.Jump:
                {
                    TossMotion motion = new TossMotion(0.6f, 0.9f, Creature.Physics.GlobalTransform, Location);
                    Creature.AI.Jump(LastTime);
                    
                    while (!motion.IsDone())
                    {
                        Creature.Physics.IsSleeping = true;
                        motion.Update(LastTime);
                        Creature.AI.Position = motion.GetTransform().Translation;
                        Creature.CurrentCharacterMode = Creature.CharacterMode.Falling;
                        yield return Status.Running;
                    }
                    break;
                }    
                case TeleportType.Lerp:
                {
                    EaseMotion motion = new EaseMotion(0.6f, Creature.Physics.GlobalTransform, Location);
                    while (!motion.IsDone())
                    {
                        motion.Update(LastTime);
                        Creature.AI.Position = motion.GetTransform().Translation;
                        yield return Status.Running;
                    }
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