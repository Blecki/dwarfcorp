using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.IO;

namespace DwarfCorp.GameStates
{
    public class CheckMegaWorldState : MenuState
    {
        public CheckMegaWorldState(DwarfGame game) :
            base(game)
        {
       
        }

        public void MakeMenu()
        {
            var frame = CreateMenu("Are you sure?\nThis will create a very large world that\nonly the most powerful machines can handle.\nReally. It's big. Like super big.");

            CreateMenuItem(frame, "I'M SURE", "If my computer catches on fire it's my own fault!",
                (sender, args) =>
                {
                    DwarfGame.LogSentryBreadcrumb("Menu", "User generating a random world.");

                    var overworldSettings = Overworld.Create();
                    overworldSettings.InstanceSettings.Cell = new ColonyCell { Bounds = new Rectangle(0, 0, 64, 64), Faction = overworldSettings.ColonyCells.GetCellAt(0, 0).Faction };
                    overworldSettings.InstanceSettings.InitalEmbarkment = new Embarkment(overworldSettings);
                    overworldSettings.InstanceSettings.InitalEmbarkment.Funds = 1000u;
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Crafter", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Manager", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Miner", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Wizard", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Soldier", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Musketeer", overworldSettings.Company));

                    GameStateManager.PushState(new LoadState(Game, overworldSettings, LoadTypes.GenerateOverworld));
                }).AutoLayout = AutoLayout.DockBottom;

            CreateMenuItem(frame, "NEVERMIND!", "This was probably a bad idea.",
                (sender, args) => GameStateManager.PopState(false)).AutoLayout = AutoLayout.DockBottom;

            FinishMenu();
        }

        public override void OnEnter()
        {
            base.OnEnter();

            MakeMenu();
            IsInitialized = true;

            DwarfTime.LastTime.Speed = 1.0f;
            SoundManager.PlayMusic("menu_music");
            SoundManager.StopAmbience();
        }

        public override void Update(DwarfTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            base.Render(gameTime);
        }
    }

}
