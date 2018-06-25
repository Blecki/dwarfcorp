using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class SetPortrait
    {
        [YarnCommand("set_portrait", "STRING", "NUMBER", "NUMBER", ArgumentTypeBehavior = YarnCommandAttribute.ArgumentTypeBehaviors.LastIsVaridic)]
        private static void _set_portrait(YarnState State, Ancora.AstNode Arguments, Yarn.MemoryVariableStore Memory)
        {
            var frames = new List<int>();
            for (var i = 2; i < Arguments.Children.Count; ++i)
                frames.Add((int)((float)Arguments.Children[i].Value));
            State.SetPortrait((string)Arguments.Children[0].Value, (float)Arguments.Children[1].Value, frames);
        }
    }
}
