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
        public static Dictionary<string, Composite> Composites { get; set; }

        public static bool IsInitialized = false;

        public static string Dwarf = "Dwarf";
        public static string Goblin = "Goblin";
        public static string Skeleton = "Skeleton";
        public static string Elf = "Elf";
        public static string Demon = "Demon";

        public static void Initialize()
        {
            if (IsInitialized) return;
            Composites = new Dictionary<string, Composite>()
            {
                {
                    Dwarf,
                    new Composite()
                    {
                        FrameSize = new Point(48, 40),
                        TargetSizeFrames = new Point(8, 8)
                    }
                },
                {
                    Goblin,
                    new Composite()
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
                {
                    Elf,
                    new Composite()
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
                {
                    Demon,
                    new Composite()
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
                {
                    Skeleton,
                    new Composite()
                    {
                        FrameSize = new Point(48, 48),
                        TargetSizeFrames = new Point(4, 4)
                    }
                },
            };

            foreach (var composite in Composites)
            {
                composite.Value.Initialize();
            }

            IsInitialized = true;
        }

        public static void Update()
        {
            foreach (var composite in Composites)
            {
                composite.Value.Update();
            }
        }

        public static void Render(GraphicsDevice device)
        {
            foreach (var composite in Composites)
            {
                composite.Value.RenderToTarget(device);
            }
        }
    }
}
