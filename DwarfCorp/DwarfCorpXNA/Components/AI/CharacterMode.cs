using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary> Enum describing the character's current action (used for animation) </summary>
    public enum CharacterMode
    {
        Walking,
        Idle,
        Falling,
        Jumping,
        Attacking,
        Attacking01,
        Attacking02,
        Hurt,
        Sleeping,
        Swimming,
        Flying,
        Sitting,
        Climbing
    }
}
