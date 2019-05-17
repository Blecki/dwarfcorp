using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.Gui.Input
{
    public enum KeyBindingType
    {
        Pressed,
        Held
    }

    public class Input
    {
        private GumInputMapper Mapper;

        public Input(GumInputMapper Mapper)
        {
            this.Mapper = Mapper;
        }

        public void FireActions(Gui.Root Gui, Action<Gui.InputEvents, Gui.InputEventArgs> externalHandler)
        {
            if (!GameState.Game.IsActive)
                return;

            var queue = Mapper.GetInputQueue();
            foreach (var @event in queue)
            {
                if (Gui != null)
                    Gui.HandleInput(@event.Message, @event.Args);

                if (!@event.Args.Handled)
                {
                    if (externalHandler != null)
                        externalHandler(@event.Message, @event.Args);
                }
            }
        }

        public void FireKeyboardActionsOnly(Gui.Root Gui)
        {
            if (!GameState.Game.IsActive)
                return;

            var queue = Mapper.GetInputQueue();
            foreach (var @event in queue)
            {
                if (@event.Message == InputEvents.KeyDown || @event.Message == InputEvents.KeyPress || @event.Message == InputEvents.KeyUp)
                    Gui.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    Mapper.QueueLock.WaitOne();
                    Mapper.Queued.Add(@event);
                    Mapper.QueueLock.ReleaseMutex();
                }

            }
        }
    }
}
