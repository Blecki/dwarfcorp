using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ApplicantInfo : Widget
    {
        private Applicant _applicant;
        public Applicant Applicant
        {
            get { return _applicant; }
            set { _applicant = value;  Invalidate(); }
        }

        private Widget NameLabel;
        private Widget ClassLabel;
        private Widget StartingWageLabel;
        private Widget SigningBonusLabel;
        private Widget Resume;
        private Widget LastJobLabel;
        private Widget LastJobLocation;
        private Widget Biography;
        private EmployeePortrait Portrait;

        public override void Construct()
        {
            Font = "font10";

            Widget topWidget = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(48 * 2, 40 * 2)
            });

            Portrait = topWidget.AddChild(new EmployeePortrait()
            {
                MinimumSize = new Point(48 * 2, 40 * 2),
                AutoLayout = AutoLayout.DockLeft
            }) as EmployeePortrait;

            NameLabel = topWidget.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 40 * 2),
                Font = "font16",
                TextVerticalAlign = VerticalAlign.Bottom
            });

            ClassLabel = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            StartingWageLabel = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Tooltip = "You must pay the dwarf this much per day."
            });

            SigningBonusLabel = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Tooltip = "When hiring the dwarf, you have to pay this much."
            });

            LastJobLabel = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            LastJobLocation = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            Biography = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(30, 30)
            });

            Resume = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(30, 30)
            });

            Border = "border-one";
            base.Construct();
        }
        
        protected override Gui.Mesh Redraw()
        {
            if (Applicant != null)
            {
                // Todo: Just use one widget for all the text.
                NameLabel.Text = Applicant.Name;
                ClassLabel.Text = Applicant.Loadout.HasValue(out var loadout) ? loadout.Name : "Dunno";
                StartingWageLabel.Text = String.Format("Starts at {0}/day", Applicant.BasePay);
                SigningBonusLabel.Text = String.Format("{0} signing bonus", Applicant.SigningBonus);
                LastJobLabel.Text = String.Format("Last job - {0}", Applicant.FormerProfession);
                LastJobLocation.Text = String.Format("Home town - {0}", Applicant.HomeTown);
                Biography.Text = Applicant.Biography;
                Resume.Text = Applicant.CoverLetter;

                //var idx = EmployeePanel.GetIconIndex(Applicant.Class.Name);
                //Portrait.Background = idx >= 0 ? new TileReference("dwarves", idx) : null;
                Portrait.Sprite = Applicant.GetLayers();
                Portrait.AnimationPlayer = Applicant.GetAnimationPlayer(Portrait.Sprite);
                Portrait.Invalidate();
                Portrait.Sprite.Update(GameStates.GameState.Game.GraphicsDevice);

            }

            return base.Redraw();
        }
        
    }
}
