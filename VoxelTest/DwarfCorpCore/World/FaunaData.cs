using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Fauna data describes how certain animals (such as birds) are to populate
    /// a chunk.
    /// </summary>
    public class FaunaData
    {
        public string Name { get; set; }
        public float SpawnProbability { get; set; }

        public FaunaData(string name, float spawnProbability)
        {
            Name = name;
            SpawnProbability = spawnProbability;
        }
    }

}