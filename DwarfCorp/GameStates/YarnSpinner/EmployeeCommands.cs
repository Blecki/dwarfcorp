using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.GameStates.YarnSpinner
{
    static class EmployeeCommands
    {
        [YarnCommand("pay_employee_bonus", ArgumentTypeBehavior = YarnCommandAttribute.ArgumentTypeBehaviors.Strict)]
        private static void _pay_employee(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var employee = Memory.GetValue("$employee");
            var creature = employee.AsObject as CreatureAI;
            if (creature == null)
                return;
            var bonus = Memory.GetValue("$employee_bonus").AsNumber;
            creature.AddMoney((decimal)(float)bonus);
            creature.Faction.AddMoney(-(decimal)(float)bonus);
        }

        [YarnCommand("add_thought", "STRING", "NUMBER", "NUMBER", ArgumentTypeBehavior = YarnCommandAttribute.ArgumentTypeBehaviors.Strict)]
        private static void _add_thought(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var employee = Memory.GetValue("$employee");
            var creature = employee.AsObject as CreatureAI;
            if (creature == null)
                return;
            creature.Creature.AddThought((string)Arguments[0].Value, new TimeSpan((int)(float)Arguments[2].Value, 0, 0), (float)Arguments[1].Value);
        }

        [YarnCommand("end_strike", ArgumentTypeBehavior = YarnCommandAttribute.ArgumentTypeBehaviors.Strict)]
        private static void _end_strike(YarnEngine State, List<Ancora.AstNode> Arguments, Yarn.MemoryVariableStore Memory)
        {
            var employee = Memory.GetValue("$employee");
            var creature = employee.AsObject as CreatureAI;
            if (creature == null)
                return;
            creature.Stats.IsOnStrike = false;

            if (creature.GetRoot().GetComponent<DwarfThoughts>().HasValue(out var thoughts))
            {
                thoughts.Thoughts.RemoveAll(t => t.Description.Contains("paid"));
                creature.Creature.AddThought("I got paid recently.", new TimeSpan(1, 0, 0, 0), 10.0f);
            }

            if (creature is DwarfAI dorf)
                dorf.OnPaid();
        }
    }
}
