using System.Runtime.Serialization;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DwarfCorp
{
    public enum TaskPriority
    {
        NotSet = -1,
        Eventually = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }

    public enum TaskCategory
    {
        None = 0,
        Other = 1,
        Dig = 2,
        Chop = 4,
        Harvest = 8,
        Attack = 16,
        Hunt = 32,
        Research = 64,
        BuildBlock = 128,
        BuildObject = 256,
        BuildZone = 512,
        CraftItem = 1024,
        Cook = 2048,
        TillSoil = 4096,
        Gather = 8192,
        Guard = 16384,
        Wrangle = 32768,
        Plant = 65536
    }

    public enum Feasibility
    {
        Feasible,
        Infeasible,
        Unknown
    }
}