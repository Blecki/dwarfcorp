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
        private Animation SpeakerAnimation;

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

            DialogueContext.SpeechBubble = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                Rect = new Rectangle(200, 0, GuiRoot.VirtualScreen.Width - 200, 200),
                Border = "speech-bubble-reverse",
                Font = "outline-font",
                TextColor = new Vector4(1,1,1,1)
            });

            DialogueContext.ChoicePanel = GuiRoot.RootItem.AddChild(new Gum.Widget
            {
                Rect = new Rectangle(200, 200, GuiRoot.VirtualScreen.Width - 400, 
                GuiRoot.VirtualScreen.Height - 200),
                Border = "border-fancy",
            });

            SpeakerAnimation = new Animation(DialogueContext.Envoy.OwnerFaction.Race.TalkAnimation);
            DialogueContext.SpeakerAnimation = SpeakerAnimation;

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

            DialogueContext.Update(gameTime);
            GuiRoot.Update(gameTime.ToGameTime());
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            // Draw speaker anim;
            //Image.Image = Animation.SpriteSheet.GetTexture();
            //Image.SourceRect = Animation.GetCurrentFrameRect();

            var texture = SpeakerAnimation.SpriteSheet.GetTexture();
            var frame = SpeakerAnimation.GetCurrentFrameRect();

            var offset = new Vector2((float)frame.X / (float)texture.Width, (float)frame.Y / (float)texture.Height);
            var scale = new Vector2((float)frame.Width / (float)texture.Width, (float)frame.Height / (float)texture.Height);

            var quad = Gum.Mesh.Quad().Texture(Matrix.CreateScale(scale.X, scale.Y, 1.0f))
                .Texture(Matrix.CreateTranslation(offset.X, offset.Y, 0.0f))
                .Scale(200, 200);

            GuiRoot.DrawMesh(quad, texture);


            GuiRoot.Draw();
            base.Render(gameTime);
        }
    }

}