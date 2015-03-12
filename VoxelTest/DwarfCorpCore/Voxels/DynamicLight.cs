﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Lights nearby voxels with torch lights.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class DynamicLight
    {
        public float Range { get; set; }
        public float Intensity { get; set; }
        public Vector3 Position { get; set; }

        public static List<DynamicLight> Lights = new List<DynamicLight>();
        public static List<DynamicLight> TempLights = new List<DynamicLight>(); 

        public DynamicLight()
        {
            
        }

        public DynamicLight(float range, float intensity, bool add = true)
        {
            Range = range;
            Intensity = intensity;

            if (add)
            {
                Lights.Add(this);
            }
        }

        public void Destroy()
        {
            Lights.Remove(this);
        }
    }

}