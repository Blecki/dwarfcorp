using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace Gem
{
    public class Input
    {
        public enum KeyBindingType
        {
            Pressed,
            Held
        }

        public class InputAction
        {
            public List<Keys> Keys = new List<Keys>();
            public KeyBindingType Type;
            public Action Handler;
        }

        private GumInputMapper Mapper;
        private Dictionary<String, InputAction> InputActions = new Dictionary<String, InputAction>();

        public IEnumerable<KeyValuePair<String, InputAction>> EnumerateBindableActions()
        {
            foreach (var binding in InputActions)
                yield return binding;
        }

        public Input(GumInputMapper Mapper)
        {
            this.Mapper = Mapper;
        }

        public void AddAction(String Action, KeyBindingType BindingType)
        {
            if (InputActions.ContainsKey(Action)) return;// throw new InvalidOperationException();

            InputActions.Add(Action, new InputAction
                {
                    Type = BindingType
                });
        }

        public void BindKey(String Action, Keys Key)
        {
            if (!InputActions.ContainsKey(Action)) throw new InvalidOperationException();
            InputActions[Action].Keys.Add(Key);
        }

        public void ClearKeyBindings(String Action)
        {
            if (!InputActions.ContainsKey(Action)) throw new InvalidOperationException();
            InputActions[Action].Keys.Clear();
        }

        public void BindHandler(String Action, Action Handler)
        {
            if (!InputActions.ContainsKey(Action)) throw new InvalidOperationException();
            InputActions[Action].Handler += Handler;
        }

        public void ClearHandlers(String Action)
        {
            if (!InputActions.ContainsKey(Action)) throw new InvalidOperationException();
            InputActions[Action].Handler = null;
        }

        public void ClearAllHandlers()
        {
            foreach (var action in InputActions)
                action.Value.Handler = null;
        }

        public void FireActions(Gum.Root Gui, Action<Gum.InputEvents, Gum.InputEventArgs> MouseHandler)
        {
            var queue = Mapper.GetInputQueue();
            foreach (var @event in queue)
            {
                if (Gui != null) 
                    Gui.HandleInput(@event.Message, @event.Args);
                
                if (!@event.Args.Handled)
                {
                    if (@event.Message == Gum.InputEvents.MouseClick ||
                        @event.Message == Gum.InputEvents.MouseMove)
                    {
                        if (MouseHandler != null) MouseHandler(@event.Message, @event.Args);
                    }
                    else if (@event.Message == Gum.InputEvents.KeyUp)
                    {
                        foreach (var binding in InputActions.Where(ia => ia.Value.Keys.Contains((Keys)@event.Args.KeyValue) && ia.Value.Type == KeyBindingType.Pressed))
                            if (binding.Value.Handler != null)
                                binding.Value.Handler();
                    }
                }
            }

            // Check 'Held' actions
            var kbState = Keyboard.GetState();
            foreach (var binding in InputActions.Where(ia => ia.Value.Type == KeyBindingType.Held))
                if (binding.Value.Keys.Count(k => kbState.IsKeyDown(k)) > 0 && binding.Value.Handler != null)
                    binding.Value.Handler();
        }
    }
}
