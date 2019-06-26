using System;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
#if !XNA_BUILD && !GEMMONO
using SDL2;
#endif

namespace DwarfCorp
{
    public class EventLogState : GameState
    {
        public EventLog Log { get; set; }
        public DateTime Now { get; set; }
        public Gui.Root GuiRoot { get; set; }
        public EventLogViewer Viewer { get; set; }

        public EventLogState(DwarfGame game, EventLog log, DateTime now) :
            base(game)
        {
            Log = log;
            Now = now;
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void OnEnter()
        {
            GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
            Viewer = GuiRoot.RootItem.AddChild(new EventLogViewer()
            {
                Log = Log,
                Now = Now,
                Rect = GuiRoot.RenderData.VirtualScreen,
                AutoLayout = AutoLayout.DockFill,
                InteriorMargin = new Margin(32, 32, 16, 16)
            }) as EventLogViewer;
            Viewer.CloseButton.OnClick = (sender, args) =>
            {
                GameStateManager.PopState();
            };
            // Must be true or Render will not be called.
            IsInitialized = true;
            GuiRoot.RootItem.Layout();
            base.OnEnter();
        }

        public override void OnPopped()
        {
            base.OnPopped();
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }

        public override void RenderUnitialized(DwarfTime gameTime)
        {
            base.RenderUnitialized(gameTime);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void Update(DwarfTime gameTime)
        {
            DwarfGame.GumInput.FireActions(GuiRoot, (@event, args) =>
            {
                if (@event == InputEvents.KeyUp && args.KeyValue == (int)Microsoft.Xna.Framework.Input.Keys.Escape)
                    GameStateManager.PopState();
            });

            GuiRoot.Update(gameTime.ToRealTime());
            SoundManager.Update(gameTime, null, null);
            base.Update(gameTime);
        }
    }
}
