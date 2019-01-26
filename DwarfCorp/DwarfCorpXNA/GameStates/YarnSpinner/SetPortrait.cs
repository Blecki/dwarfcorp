using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class SetPortrait
    {
        [YarnCommand("set_portrait", "STRING", "NUMBER", "NUMBER", "NUMBER", "NUMBER", ArgumentTypeBehavior = YarnCommandAttribute.ArgumentTypeBehaviors.LastIsVaridic)]
        private static void _set_portrait(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var frames = new List<int>();
            for (var i = 4; i < Arguments.Count; ++i)
                frames.Add((int)((float)Arguments[i].Value));
            State.PlayerInterface.SetPortrait((string)Arguments[0].Value, (int)((float)Arguments[1].Value), (int)((float)Arguments[2].Value), (float)Arguments[3].Value, frames);
        }

        [YarnCommand("set_idle", "STRING", "NUMBER", "NUMBER", "NUMBER", "NUMBER", ArgumentTypeBehavior = YarnCommandAttribute.ArgumentTypeBehaviors.LastIsVaridic)]
        private static void _set_idle(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var frames = new List<int>();
            for (var i = 4; i < Arguments.Count; ++i)
                frames.Add((int)((float)Arguments[i].Value));
            State.PlayerInterface.SetIdle((string)Arguments[0].Value, (int)((float)Arguments[1].Value), (int)((float)Arguments[2].Value), (float)Arguments[3].Value, frames);
        }
    }
}
