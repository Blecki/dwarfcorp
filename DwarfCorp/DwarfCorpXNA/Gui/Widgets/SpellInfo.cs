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
        private int _lastProgress = -1;
        public override void Construct()
        {
            this.Root.RegisterForUpdate(this);
            Border = "border-fancy";

            Font = "font8";
            Update();
            OnUpdate = (widget, time) => this.Update();
        }

        public void Update()
        {
            if (Hidden)
            {
                return;
            }
            int progress = (int)(100 * Spell.ResearchProgress / Spell.ResearchTime);
            if (progress != _lastProgress)
            {
                _lastProgress = progress;
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
                        builder.AppendLine(string.Format("{0} % researched", (int)(100 * Spell.ResearchProgress / Spell.ResearchTime)));
                    }
                    builder.AppendLine("Unlocks:");
                    builder.AppendLine(TextGenerator.GetListString(Spell.Children.Select(child => child.Spell.Name)));
                }
                Text = builder.ToString();
            }
        }
    }
}
