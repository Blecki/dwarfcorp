using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using System;

namespace DwarfCorp.Scripting.Factions.Trading
{
    public class TradeGameState : GameState
    {
        private Gui.Root GuiRoot;
        private WorldManager World;
        private TradeEnvoy Envoy;
        private Faction PlayerFaction;
        public Gui.Widgets.TradePanel TradePanel;
        public Action CallWhenDone = null;

        public TradeGameState(
            DwarfGame Game, 
            GameStateManager StateManager,
            TradeEnvoy Envoy, 
            Faction PlayerFaction,
            WorldManager World) :
            base(Game, StateManager)
        {
            this.World = World;
            this.Envoy = Envoy;
            this.PlayerFaction = PlayerFaction;
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();
            World.Tutorial("trade_screen");
            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            GuiRoot.RootItem.Font = "font8";

            TradePanel = GuiRoot.ConstructWidget(new Gui.Widgets.TradePanel
            {
                Rect = GuiRoot.RenderData.VirtualScreen,
                Envoy = new Trade.EnvoyTradeEntity(Envoy),
                Player = new Trade.PlayerTradeEntity(PlayerFaction),
            }) as Gui.Widgets.TradePanel;

            TradePanel.Layout();

            GuiRoot.ShowDialog(TradePanel);

            IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
                GuiRoot.HandleInput(@event.Message, @event.Args);
            GuiRoot.Update(gameTime.ToRealTime());
            World.TutorialManager.Update(GuiRoot);

            if(TradePanel.Result == Gui.Widgets.TradeDialogResult.Pending)
                return;

            StateManager.PopState();
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            base.Render(gameTime);
        }

        public override void OnPopped()
        {
            base.OnPopped();
            CallWhenDone?.Invoke();
        }
    }

}