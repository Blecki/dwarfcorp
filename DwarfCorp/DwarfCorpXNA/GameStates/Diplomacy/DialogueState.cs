using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
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
        private Gui.Root GuiRoot;
        private DialogueContext DialogueContext;
        private Animation SpeakerAnimation;
        private WorldManager World;
        private Gui.Widget SpeakerWidget;
        private int prevFrame = 0;
        public DialogueState(
            DwarfGame Game, 
            GameStateManager StateManager,
            TradeEnvoy Envoy, 
            Faction PlayerFaction,
            WorldManager World) :
            base(Game, "GuiStateTemplate", StateManager)
        {
            this.World = World;

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

            GuiRoot = new Gui.Root(DwarfGame.GumSkin);
            GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);

            int w = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 550);
            int h = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Height - 256, 300);
            int x = GuiRoot.RenderData.VirtualScreen.Width / 2 - w / 2;
            int y = System.Math.Max(GuiRoot.RenderData.VirtualScreen.Height / 2 - h / 2, 280);

            int bgx = x - 258;
            int bgy = y - 128;

            DialogueContext.SpeechBubble = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = new Rectangle(bgx + 258, bgy, w + 50, 128),
                Border = "speech-bubble-reverse",
                Font = "font-hires",
                TextColor = Color.Black.ToVector4()
            });

            var bg = GuiRoot.RootItem.AddChild(new Widget()
            {
                Border = "border-dark",
                Rect = new Rectangle(bgx, bgy, 258, 258)
            });


            DialogueContext.ChoicePanel = GuiRoot.RootItem.AddChild(new Gui.Widget
            {
                Rect = new Rectangle(x, y, w, h),
                Border = "border-fancy",
                AutoLayout = AutoLayout.DockFill
            });

            SpeakerAnimation = new Animation(DialogueContext.Envoy.OwnerFaction.Race.TalkAnimation);
            DialogueContext.SpeakerAnimation = SpeakerAnimation;
            DialogueContext.SpeakerAnimation.Loops = false;


            SpeakerWidget = bg.AddChild(new Widget()
            {
                Background = new TileReference(SpeakerAnimation.SpriteSheet.AssetName, 0),
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(256, 256),
                Rect = new Rectangle(bgx, bgy - 5, 256, 256)
            });

            DialogueContext.Politics = World.Diplomacy.GetPolitics(
                DialogueContext.PlayerFaction, DialogueContext.Envoy.OwnerFaction);
            DialogueContext.World = World;

            if (!DialogueContext.Politics.HasMet)
            {
                DialogueContext.Politics.HasMet = true;

                DialogueContext.Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                {
                    Change = 0.0f,
                    Description = "we just met",
                    Duration = new TimeSpan(1, 0, 0, 0),
                    Time = World.Time.CurrentDate
                });
            }

            DialogueContext.EnvoyName = TextGenerator.GenerateRandom(Datastructures.SelectRandom(DialogueContext.Envoy.OwnerFaction.Race.NameTemplates).ToArray());
            
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
                    if (@event.Args.KeyValue > 0)
                    {
                        DialogueContext.Skip();
                    }
                    // Pass event to game...
                }
            }
            SoundManager.Update(gameTime, World.Camera, World.Time);
            GuiRoot.Update(gameTime.ToGameTime());
            DialogueContext.Update(gameTime);
            World.TutorialManager.Update(GuiRoot);
            World.Paused = true;
            IsInitialized = true;
            base.OnEnter();
        }

        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();
            
            if (prevFrame != SpeakerAnimation.Frames[SpeakerAnimation.CurrentFrame].X)
            {
                SpeakerWidget.Background = new TileReference(SpeakerAnimation.SpriteSheet.AssetName, SpeakerAnimation.Frames[SpeakerAnimation.CurrentFrame].X);
                prevFrame = SpeakerAnimation.Frames[SpeakerAnimation.CurrentFrame].X;
                SpeakerWidget.Invalidate();
            }
            base.Render(gameTime);
        }
    }

}