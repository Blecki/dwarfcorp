using System.Collections.Generic;

namespace DwarfCorp
{
    /// <summary>
    /// A creature sets a particular memory location in the blackboard
    /// to the given value.
    /// </summary>
    /// <typeparam Name="TValue">The type of the value.</typeparam>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class SetBlackboardData <TValue> : CreatureAct
    {
        private string DataKey { get; set; }
        private TValue Value { get; set; }

        public SetBlackboardData(CreatureAIComponent creature, string data, TValue value) :
            base(creature)
        {
            Name = "Set " + data;
            DataKey = data;
            Value = value;
        }

        public override IEnumerable<Status> Run()
        {
            if(DataKey == null)
            {
                yield return Status.Fail;
            }
            else
            {
                Agent.Blackboard.SetData(DataKey, Value);
                yield return Status.Success;
            }
        }
    }

}