using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.GameStates
{
    public class OverworldGenerationSettings
    {
        public int NumCivilizations = 5;
        public int NumRains = 1000;
        public int NumVolcanoes = 3;
        public float RainfallScale = 2.0f;
        public int NumFaults = 3;
        public float SeaLevel = 0.17f;
        public float TemperatureScale = 1.0f;
    }
}
