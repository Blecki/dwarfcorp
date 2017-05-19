using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class GoalInfo : Widget
    {
        private Goals.Goal _goal;
        public Goals.Goal Goal
        {
            get { return _goal; }
            set { _goal = value; _goal.BuildCustomGUI(this); Invalidate(); }
        }

        private Widget Description;
        private Widget ActivateButton;        
        public Action<Widget> OnActivateClicked;

        public override void Construct()
        {
            Description = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop
            });

            ActivateButton = AddChild(new Widget
            {
                Text = "Activate!",
                Font = "font-hires",
                Border = "border-button",
                AutoLayout = AutoLayout.FloatBottomRight,
                OnClick = (sender, args) =>
                {
                    Root.SafeCall(OnActivateClicked, this);
                }
            });

            base.Construct();
        }

        protected override Gum.Mesh Redraw()
        {
            if (Goal != null)
            {
                ActivateButton.Hidden = !(Goal.State == Goals.GoalState.Available);
            } 
            
            return base.Redraw();
        }        
    }
}
