using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mime;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class CompositeLibrary
    {
        public static Dictionary<string, Composite> Composites = new Dictionary<string, Composite>();

        public static Composite GetComposite(String Name, Point FrameSize)
        {
            Composite r = null;
            if (Composites.TryGetValue(Name, out r))
            {
                return r;
            }
            r = new Composite();
            r.Init(FrameSize, new Point(16, 16));
            Composites.Add(Name, r);
            return r;
        }

        public static void Update()
        {
            foreach (var composite in Composites)
                composite.Value.Update();
        }

        public static void Render(GraphicsDevice device)
        {
            foreach (var composite in Composites)
                composite.Value.RenderToTarget(device);
        }
    }
}
