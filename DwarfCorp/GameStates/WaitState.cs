using System.Globalization;
using System.Threading;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using System;

namespace DwarfCorp.GameStates
{
    public class WaitStateException : Exception
    {
        public WaitStateException(string message) : base(message) { }
    }

    public class WaitState : GameState
    {
        public Thread WaitThread { get; set; }

        public event Finished OnFinished;

        protected virtual void OnOnFinished()
        {
            Finished handler = OnFinished;
            if (handler != null) handler(true, null);
        }
        public bool Done { get; protected set; }
        public delegate void Finished(bool success, WaitStateException e);

        /// <summary>
        /// the routine to be run in a thread while the wait state is active
        /// </summary>
        /// <returns>success</returns>
        public delegate bool WaitThreadRoutine();

        public WaitStateException exception;
        public bool success = false;
        private Gui.Root GuiRoot;

        public WaitState(DwarfGame game, string name, WaitThreadRoutine routine)
            : base(game)
        {
            WaitThread = new Thread(() =>
            {
                global::System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                global::System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                runRoutine(routine);
            })
            {
                IsBackground = true
            };

            OnFinished = (Boolean, Exception) => { };
            Done = false;

            GuiRoot = new Gui.Root(DwarfGame.GuiSkin)
            {
                MousePointer = new MousePointer("mouse", 15.0f, 16, 17, 18, 19, 20, 21, 22, 23)
            };

            GuiRoot.RootItem.AddChild(new Widget()
            {
                Text = name,
                Font = "font18-outline",
                AutoLayout = AutoLayout.DockFill,
                TextColor = Color.White.ToVector4(),
                TextHorizontalAlign = HorizontalAlign.Center,
                TextVerticalAlign = VerticalAlign.Center,
                Rect = GuiRoot.RenderData.VirtualScreen
            });
        }

        protected void runRoutine(WaitThreadRoutine routine)
        {
            try
            {
                success = routine();
            } catch (WaitStateException e)  // allows any exceptions we didn't throw on purpose to be thrown properly
            {
                this.exception = e;
            }
        }

        public override void OnEnter()
        {
            IsInitialized = true;
            WaitThread.Start();
            base.OnEnter();
        }

        public override void OnPopped()
        {
            OnFinished.Invoke(success, exception);
            base.OnPopped();
        }

        public override void Update(DwarfTime gameTime)
        {
            if (!Game.IsActive)
                return;

            GuiRoot.Update(gameTime.ToRealTime());
            DwarfGame.GumInput.FireActions(GuiRoot, null);
            if (!WaitThread.IsAlive && !Done)
            {
                GameStateManager.PopState();
                Done = true;
            }

            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}