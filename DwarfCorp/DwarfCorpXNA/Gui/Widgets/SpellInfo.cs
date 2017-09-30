using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    /// <summary>
    /// A properly framed Icon for use in an icon tray.
    /// </summary>
    public class SpellInfo : Widget
    {
        public SpellTree.Node Spell;
        public GameMaster Master;

        public override void Construct()
        {
            Border = "border-fancy";

            var builder = new StringBuilder();
            builder.AppendLine(Spell.Spell.Name);
            builder.AppendLine(Spell.Spell.Description);
            if (Spell.IsResearched)
            {
                builder.AppendLine("\n-- Click to cast");
            }
            else if (Spell.Children.Count > 0)
            {
                if (Spell.ResearchProgress > 0)
                {
                    builder.AppendLine(string.Format("{0} % researched", (int) (Spell.ResearchProgress*100)));
                }
                builder.AppendLine("Unlocks:");
                builder.AppendLine(TextGenerator.GetListString(Spell.Children.Select(child => child.Spell.Name)));
            }
            Font = "font8";
            Text = builder.ToString();
        }
    }
}
