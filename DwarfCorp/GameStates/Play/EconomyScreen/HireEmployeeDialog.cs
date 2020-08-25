using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class HireEmployeeDialog : Widget
    {
        class GeneratedApplicant
        {
            public EmployeePortrait Portrait;
            public Applicant Applicant;
        }

        public WorldManager World;
        public CompanyInformation Company;
        private Button HireButton;
        private Dictionary<String, GeneratedApplicant> Applicants = new Dictionary<string, GeneratedApplicant>();

        public Applicant GenerateApplicant(CompanyInformation info, Loadout Loadout)
        {
            Applicant applicant = new Applicant();
            applicant.GenerateRandom(Loadout, 0, info);
            return applicant;
        }

        public HireEmployeeDialog(CompanyInformation _Company)
        {
            Company = _Company;
        }

        public override void Construct()
        {
            Root.RegisterForUpdate(this);

            Border = "border-fancy";

            int w = Root.RenderData.VirtualScreen.Width;
            int h = Root.RenderData.VirtualScreen.Height;
            Rect = new Rectangle(Root.RenderData.VirtualScreen.Center.X - w / 2, Root.RenderData.VirtualScreen.Center.Y - h/2, w, h);

            var playerClasses = Library.EnumerateLoadouts().ToList();
            foreach (var job in playerClasses)
                Applicants.Add(job.Name, new GeneratedApplicant { Applicant = GenerateApplicant(Company, job) });

            var left = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockTop,
                Padding = new Margin(5, 5, 32, 32),
                MinimumSize = new Point(48 * 2 * playerClasses.Count, 40 * 2 + 40)
            });

            var right = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockFill
            });

            var buttonRow = right.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30)
            });

            var jobDescription = right.AddChild(new Widget
            {
                MinimumSize = new Point((int)(w * 0.4f), 0),
                InteriorMargin = new Margin(16, 16, 16, 16),
                WrapWithinWords = false,
                WrapText = true,
                AutoLayout = AutoLayout.DockLeft,
                Font = "font10"
            });

            var applicantInfo = right.AddChild(new ApplicantInfo
            {
                AutoLayout = AutoLayout.DockFill,
                Font = "font10"
            }) as ApplicantInfo;
            
            applicantInfo.Hidden = true;

            left.AddChild(new Widget
            {
                Text = "Applicants",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 20),
                Font = "font16"
            });
            
            foreach (var job in playerClasses)
            {
                var frame = left.AddChild(new Widget()
                {
                    MinimumSize = new Point(48*2, 40*2 + 15),
                    AutoLayout = AutoLayout.DockLeft
                });

                var applicant = Applicants[job.Name];
                var layers = applicant.Applicant.GetLayers();

                applicant.Portrait = frame.AddChild(new EmployeePortrait()
                {
                    Tooltip = "Click to review applications for " + job.Name,
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Center,
                    TextVerticalAlign = VerticalAlign.Bottom,
                    OnClick = (sender, args) =>
                    {
                        jobDescription.Text = "\n\n" + applicant.Applicant.Loadout.Description;
                        jobDescription.Invalidate();
                        applicantInfo.Hidden = false;
                        HireButton.Hidden = false;
                        HireButton.Invalidate();
                        applicantInfo.Applicant = applicant.Applicant;
                    },
                    MinimumSize = new Point(48 * 2, 40 * 2),
                    MaximumSize = new Point(48 * 2, 40 * 2),
                    Sprite = layers,
                    AnimationPlayer = applicant.Applicant.GetAnimationPlayer(layers)
                }) as EmployeePortrait;

                frame.AddChild(new Widget()
                {
                    Text = job.Name,
                    MinimumSize = new Point(0, 15),
                    TextColor = Color.Black.ToVector4(),
                    Font = "font10",
                    AutoLayout = AutoLayout.DockTop,
                    TextHorizontalAlign = HorizontalAlign.Center
                });
            }

            buttonRow.AddChild(new Widget
            {
                Text = "Back",
                Border = "border-button",
                AutoLayout = AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                {
                    this.Close();
                }
            });

            HireButton = buttonRow.AddChild(new Button
            {
                Text = "Hire",
                Border = "border-button",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    var applicant = applicantInfo.Applicant;
                    if (applicant != null)
                    {
                        if (applicant.SigningBonus > World.PlayerFaction.Economy.Funds)
                            Root.ShowModalPopup(Root.ConstructWidget(new Gui.Widgets.Popup
                            {
                                Text = "We can't afford the signing bonus!",
                            }));
                        else if (!World.EnumerateZones().Any(r => r.Type.Name == "Balloon Port"))
                            Root.ShowModalPopup(Root.ConstructWidget(new Gui.Widgets.Popup
                            {
                                Text = "We need a balloon port to hire someone.",
                            }));
                        else
                        {
                            var date = World.Hire(applicant, 1);
                            SoundManager.PlaySound(ContentPaths.Audio.cash, 0.5f);
                            applicantInfo.Hidden = true;
                            HireButton.Hidden = true;
                            Root.ShowModalPopup(new Gui.Widgets.Popup()
                            {
                                Text = String.Format("We hired {0}, paying a signing bonus of {1}. They will arrive in about {2} hour(s).",
                                applicant.Name,
                                applicant.SigningBonus,
                                (date - World.Time.CurrentDate).Hours),
                            });

                            var newApplicant = GenerateApplicant(Company, applicant.Loadout);
                            Applicants[applicant.Loadout.Name].Applicant = newApplicant;
                            Applicants[applicant.Loadout.Name].Portrait.Sprite = newApplicant.GetLayers();
                            Applicants[applicant.Loadout.Name].Portrait.AnimationPlayer = newApplicant.GetAnimationPlayer(Applicants[applicant.Loadout.Name].Portrait.Sprite);
                        }
                    }
                },
                Hidden = true
            }) as Button;
            this.Layout();

            OnUpdate += (sender, time) =>
            {
                foreach (var applicant in Applicants)
                {
                    applicant.Value.Portrait.Invalidate();
                    applicant.Value.Portrait.Sprite.Update(GameStates.GameState.Game.GraphicsDevice);
                }
            };
        }
    }
}
