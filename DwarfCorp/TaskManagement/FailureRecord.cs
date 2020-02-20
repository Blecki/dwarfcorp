using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class FailureRecord
    {
        public String InfeasibleReason = "";
        public List<String> FailureReasons = new List<String>();

        public const int ReasonsToKeep = 5;

        public void AddFailureReason(CreatureAI Agent, String Reason)
        {
            FailureReasons.RemoveAll(r => r.StartsWith(Agent.Stats.FullName));
            FailureReasons.Add(String.Format("{0}: {1}", Agent.Stats.FullName, Reason));
            if (FailureReasons.Count > ReasonsToKeep)
                FailureReasons.RemoveAt(0);
        }

        public String FormatTooltip()
        {
            var r = InfeasibleReason;
            if (!String.IsNullOrEmpty(InfeasibleReason)) r += "\n";
            r += String.Join("\n", FailureReasons);
            return r;
        }
    }
}