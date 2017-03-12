using System.Collections.Generic;
using System.Linq;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using System;

namespace DwarfCorp.Dialogue
{
    public class DialogueState : GameState
    {
        private Gum.Root GuiRoot;
        private DialogueContext DialogueContext;

        public DialogueState(
            DwarfGame Game, 
            GameStateManager StateManager,
            Faction.TradeEnvoy Envoy, 
            Faction PlayerFaction) :
            base(Game, "GuiStateTemplate", StateManager)
        {
            DialogueContext = new DialogueContext
            {
                Envoy = Envoy,
                PlayerFaction = PlayerFaction
            };
        }

        public override void OnEnter()
        {
            // Clear the input queue... cause other states aren't using it and it's been filling up.
            DwarfGame.GumInputMapper.GetInputQueue();

            GuiRoot = new Gum.Root(new Point(640, 480), DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gum.MousePointer("mouse", 4, 0);

            var dialoguePanel = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                Font = "outline-font",
                TextColor = new Vector4(1,1,1,1),
                MinimumSize = new Point(256, 0),
                MaximumSize = new Point(256, Int32.MaxValue),
                AutoLayout = Gum.AutoLayout.DockRight,
                Border = "border-fancy",
                InteriorMargin = new Gum.Margin(128,0,0,0)
            });

            GuiRoot.RootItem.Layout();

            DialogueContext.Panel = dialoguePanel;
            DialogueContext.Transition(DialogueTree.ConversationRoot);

            IsInitialized = true;
            base.OnEnter();
        }

        public override void Update(DwarfTime gameTime)
        {
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled)
                {
                    // Pass event to game...
                }
            }

            DialogueContext.Update();
            GuiRoot.Update(gameTime.ToGameTime());
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}