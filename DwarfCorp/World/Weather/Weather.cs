using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Weather
    {
        public List<Storm> Forecast { get; set; }
        public Vector3 CurrentWind { get; set; }

        public Weather()
        {
            Forecast= new List<Storm>();
            CurrentWind = new Vector3(0, 0, 0);
        }

        public bool IsRaining()
        {
            return Forecast.Any(storm => storm.IsInitialized && storm.TypeofStorm == StormType.RainStorm);
        }

        public bool IsSnowing()
        {
            return Forecast.Any(storm => storm.IsInitialized && storm.TypeofStorm == StormType.SnowStorm);
        }

        public void Update(DateTime currentDate, WorldManager world)
        {
            CurrentWind = new Vector3((float)Math.Sin(world.Time.GetTotalSeconds() * 0.001f), 0, (float)Math.Cos(world.Time.GetTotalSeconds() * 0.0015f));
            CurrentWind.Normalize();

            foreach (Storm storm in Forecast)
            {
                if (!storm.IsInitialized && currentDate > storm.Date)
                {
                    storm.Start();
                }

                if (storm.IsInitialized && !storm.IsDone() && currentDate > storm.Date)
                {
                    CurrentWind += storm.WindSpeed * 0.01f;
                }
            }

            Forecast.RemoveAll(storm => storm.IsDone());

            if (Forecast.Count == 0)
            {
                Forecast = CreateForecast(currentDate, world.ChunkManager.Bounds, world, 5);
            }
        }

        public static Storm CreateStorm(Vector3 windSpeed, float intensity, WorldManager world)
        {
            windSpeed.Y = 0;
            Storm storm = new Storm(world)
            {
                WindSpeed = windSpeed,
                Intensity = intensity,
            };
            storm.Start();
            return storm;
        }

        public static List<Storm> CreateForecast(DateTime date, BoundingBox bounds, WorldManager world, int days)
        {
            List<Storm> foreCast = new List<Storm>();
  
            for (int i = 0; i < days; i++)
            {
                // Each day, a storm could originate from a randomly selected biome
                Vector3 randomSample = MathFunctions.RandVector3Box(bounds);
                float rain = world.Settings.Map.GetValueAt(randomSample, OverworldField.Rainfall, world.Settings.InstanceSettings.Origin);
                float temperature = world.Settings.Map.GetValueAt(randomSample, OverworldField.Temperature, world.Settings.InstanceSettings.Origin);
                // Generate storms according to the rainfall in the biome. Up to 4 storms per day.
                int numStorms = (int) MathFunctions.Rand(0, rain*4);

                // Space out the storms by a few hours
                int stormHour = MathFunctions.RandInt(0, 6);
                for (int j = 0; j < numStorms; j++)
                {
                    bool isSnow = MathFunctions.RandEvent(1.0f - temperature);
                    Storm storm = new Storm(world)
                    {
                        WindSpeed = MathFunctions.RandVector3Cube()*5,
                        Intensity = MathFunctions.Rand(rain, rain*2),
                        Date = date + new TimeSpan(i, stormHour, 0, 0),
                        TypeofStorm = isSnow ? StormType.SnowStorm : StormType.RainStorm
                    };
                    storm.WindSpeed = new Vector3(storm.WindSpeed.X, 0, storm.WindSpeed.Z);
                    stormHour += MathFunctions.RandInt(1, 12);
                    foreCast.Add(storm);
                }
            }
            return foreCast;
        }
    }
}
