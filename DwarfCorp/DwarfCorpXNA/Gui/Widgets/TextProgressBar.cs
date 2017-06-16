using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class TextProgressBar : Widget
    {
        private float _percentage = 0.0f;
        public float Percentage
        {
            get { return _percentage; }
            set
            {
                _percentage = value;

                var builder = new StringBuilder();
                builder.Append(Label);
                builder.Append(" ");
                
                for (var i = 0; i < SegmentCount; ++i)
                {
                    var segPercent = (float)i / (float)SegmentCount;
                    if (_percentage > segPercent) builder.Append("|");
                    else builder.Append(".");
                }

                builder.Append(" ");

                if (PercentageLabels != null)
                {
                    builder.Append("(");
                    var labelIndex = (int)(_percentage * PercentageLabels.Length);
                    if (labelIndex < 0) labelIndex = 0;
                    if (labelIndex >= PercentageLabels.Length) labelIndex = PercentageLabels.Length - 1;
                    builder.Append(PercentageLabels[labelIndex]);
                    builder.Append(")");
                }

                Text = builder.ToString();
            }
        }

        public String Label;
        public int SegmentCount = 10;
        public String[] PercentageLabels;

        public override Point GetBestSize()
        {
            var baseBest = base.GetBestSize();
            baseBest.Y = 32;
            return baseBest;
        }
    }
}
