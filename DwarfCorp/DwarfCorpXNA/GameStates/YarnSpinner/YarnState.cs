using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using System;

namespace DwarfCorp
{
    public class YarnState : GameState, IYarnPlayerInterface
    {
        private YarnEngine YarnEngine;
        private Gui.Root GuiRoot;
        private Gui.Widgets.TextBox _Output;
        private Widget ChoicePanel;
        private AnimationPlayer SpeakerAnimationPlayer;
        private Animation SpeakerAnimation;
        private bool SpeakerVisible = false;
        private Gui.Mesh SpeakerRectangle = null;
        private Widget SpeakerBorder = null;
        private SpeechSynthesizer Language;
        private IEnumerator<String> CurrentSpeach;
        public bool SkipNextLine = false;
        private Gui.Widgets.TradePanel TradePanel;
        private bool AutoHideBubble = false;
        private float TimeSinceOutput = 0.0f;
        private DwarfCorp.Gui.Widgets.EmployeePortrait Icon = null;
        private CreatureAI _employee = null;
        private float _voicePitch = 0.0f;
        public YarnState(
            String ConversationFile,
            String StartNode,
            Yarn.MemoryVariableStore Memory) :
            base(Game, "YarnState", GameState.Game.StateManager)
        {
            YarnEngine = new YarnEngine(ConversationFile, StartNode, Memory, this);
        }

        public void SetVoicePitch(float pitch)
        {
            _voicePitch = pitch;
        }

        public void AddEmployeePortrait(CreatureAI creature)
        {
            _employee = creature;
        }

        private void setupPortrait()
        {
            Icon = GuiRoot.RootItem.AddChild(new Gui.Widgets.EmployeePortrait()
            {
                Rect = SpeakerBorder.Rect
            }) as Gui.Widgets.EmployeePortrait;

            var sprite = _employee.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();
            if (sprite != null)
            {
                Icon.Sprite = sprite.GetLayers();
                Icon.AnimationPlayer = new AnimationPlayer(sprite.GetAnimation(_employee.Creature.CurrentCharacterMode.ToString() + "FORWARD"));
            }
            else
            {
                Icon.Sprite = null;
                Icon.AnimationPlayer = null;
            }

            var label = Icon.AddChild(new Widget()
            {
                Text = _employee.Stats.FullName + "\n(" + (_employee.Stats.Title ?? _employee.Stats.CurrentClass.Name) + ")",
                Font = "font10",
                TextColor = Color.White.ToVector4(),
                TextVerticalAlign = VerticalAlign.Center,
                TextHorizontalAlign = HorizontalAlign.Center,
                AutoLayout = AutoLayout.DockBottom
            });
            Icon.Layout();
        }

        public void Output(String S)
        {
            if (_Output != null)
                _Output.AppendText(S);
            TimeSinceOutput = 0.0f;
        }

        public void Speak(String S)
        {
            _Output?.ClearText();
            var colon = S.IndexOf(":");
            if (colon != -1)
            {
                var name = S.Substring(0, colon + 1);
                S = S.Substring(colon + 1);
                _Output?.AppendText(name+"\n");
                TimeSinceOutput = 0.0f;

                SpeakerAnimationPlayer?.Play();

                if (Language != null)
                {
                    CurrentSpeach = Language.Say(S).GetEnumerator();
                    YarnEngine.EnterSpeakState();
                }
                else
                {
                    _Output?.AppendText(S);
                    TimeSinceOutput = 0.0f;
                }
            }
            else
            {
                _Output?.AppendText(S);
                TimeSinceOutput = 0.0f;
            }
        }

        public bool CancelSpeech()
        {
            CurrentSpeach = null;
            return true;
        }

        public bool AdvanceSpeech(DwarfTime gameTime)
        {
            if (!SkipNextLine)
            {
                if (CurrentSpeach.MoveNext())
                {
                    SpeakerAnimationPlayer?.Update(gameTime, false, Timer.TimerMode.Real);
                    Output(CurrentSpeach.Current);
                    return true;
                }
                else
                {
                    SpeakerAnimationPlayer?.Stop();
                    return false;
                }
            }
            else
            {
                Language.IsSkipping = true;
                while (CurrentSpeach.MoveNext())
                    Output(CurrentSpeach.Current);
                SpeakerAnimationPlayer?.Stop();
                SkipNextLine = false;
                return false;
            }
        }

        public void ClearOutput()
        {
            _Output?.ClearText();
        }

        public void SetLanguage(Language Language)
        {
            // Todo: Remove the reference to Language entirely
            this.Language = new SpeechSynthesizer(Language);
            this.Language.Pitch = _voicePitch;
        }

        public void SetPortrait(String Gfx, int FrameWidth, int FrameHeight, float Speed, List<int> Frames)
        {
            SpeakerAnimation = AnimationLibrary.CreateAnimation(new Animation.SimpleDescriptor
            {
                AssetName = Gfx,
                Speed = 1.0f/Speed,
                Frames = Frames,
                Width = FrameWidth,
                Height = FrameHeight
            });

            SpeakerAnimation.Loops = true;

            SpeakerAnimationPlayer = new AnimationPlayer(SpeakerAnimation);
            SpeakerAnimationPlayer.Play();
        }

        public void ShowPortrait()
        {
            SpeakerVisible = true;
        }

        public void HidePortrait()
        {
            SpeakerVisible = false;
        }

        public void EndConversation()
        {
            StateManager.PopState();
        }

        public override void OnEnter()
        {
            DwarfGame.GumInputMapper.GetInputQueue();

            if (GuiRoot == null)
            {
                GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
                GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
                GuiRoot.RootItem.Font = "font8";

                _Output = GuiRoot.RootItem.AddChild(new Gui.Widgets.TextBox
                {
                    Border = "speech-bubble-reverse",
                    TextSize = 1,
                    Font = "font16"
                }) as Gui.Widgets.TextBox;


                ChoicePanel = GuiRoot.RootItem.AddChild(new Widget
                {
                    Border = null,
                    TextSize = 1,
                    Font = "font16"
                });

                SpeakerBorder = GuiRoot.RootItem.AddChild(new Widget
                {
                    Border = "border-dark",
                });

                PositionItems();

                if (_employee != null)
                {
                    setupPortrait();
                }
            }

            IsInitialized = true;
            base.OnEnter();
        }

        private void PositionItems()
        {
            int w = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 550);
            int h = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Height - 256, 300);
            int x = GuiRoot.RenderData.VirtualScreen.Width / 2 - w / 2;
            int y = System.Math.Max(GuiRoot.RenderData.VirtualScreen.Height / 2 - h / 2, 280);

            _Output.Rect = new Rectangle(x, y - 260, w, 260);
            ChoicePanel.Rect = new Rectangle(x, y, w, h);

                int inset = 32;
            SpeakerBorder.Rect = new Rectangle(x - w / 2 + inset / 2, y - 260 + inset, 256 - inset, 256 - inset);
            SpeakerRectangle = Gui.Mesh.Quad().Scale(256, 256).Translate(x - w / 2, y - 260);

            _Output.Invalidate();
            SpeakerBorder.Invalidate();
        }

        public override void Update(DwarfTime gameTime)
        {
            TimeSinceOutput += (float)gameTime.ElapsedRealTime.TotalSeconds;

            SoundManager.Update(gameTime, null, null);

            SkipNextLine = false;
            foreach (var @event in DwarfGame.GumInputMapper.GetInputQueue())
            {
                GuiRoot.HandleInput(@event.Message, @event.Args);
                if (!@event.Args.Handled && (@event.Message == InputEvents.KeyUp || @event.Message == InputEvents.MouseClick))
                {
                    SkipNextLine = true;
                }
            }

            if (AutoHideBubble)
            {
                _Output.Hidden = TimeSinceOutput > 2.0f;
                SpeakerBorder.Hidden = TimeSinceOutput > 2.0f;
                GuiRoot.RootItem.Invalidate();
            }
            else
            {
                _Output.Hidden = false;
                SpeakerBorder.Hidden = false;
                GuiRoot.RootItem.Invalidate();
            }

            GuiRoot.Update(gameTime.ToRealTime());

            YarnEngine.Update(gameTime);
        }
    
        public override void Render(DwarfTime gameTime)
        {
            GuiRoot.Draw();

            if (SpeakerVisible && SpeakerAnimationPlayer != null && !_Output.Hidden)
            {
                var sheet = SpeakerAnimationPlayer.GetCurrentAnimation().SpriteSheet;
                var frame = SpeakerAnimationPlayer.GetCurrentAnimation().Frames[SpeakerAnimationPlayer.CurrentFrame];
                SpeakerRectangle.ResetQuadTexture();
                SpeakerRectangle.Texture(sheet.TileMatrix(frame.X, frame.Y));
                GuiRoot.DrawMesh(SpeakerRectangle, sheet.GetTexture());
            }

            base.Render(gameTime);
        }

        public void ClearChoices()
        {
            ChoicePanel.Clear();
        }

        public void AddChoice(String Option, Action Callback)
        {

            ChoicePanel.AddChild(new Gui.Widgets.Button()
            {
                Text = Option,
                MinimumSize = new Point(0, 20),
                AutoLayout = AutoLayout.DockTop,
                ChangeColorOnHover = true,
                WrapText = true,
                OnClick = (sender, args) =>
                {
                    //Output("> " + sender.Text + "\n");
                    ChoicePanel.Clear();
                    ChoicePanel.Invalidate();

                    Callback();
                }
            });
        }

        public void DoneAddingChoices()
        {
            ChoicePanel.Layout();
        }

        public void BeginTrade(TradeEnvoy Envoy, Faction PlayerFaction)
        {
            TradePanel = GuiRoot.ConstructWidget(new Gui.Widgets.TradePanel
            {
                Rect = GuiRoot.RenderData.VirtualScreen,
                Envoy = new Trade.EnvoyTradeEntity(Envoy),
                Player = new Trade.PlayerTradeEntity(PlayerFaction),
            }) as Gui.Widgets.TradePanel;

            TradePanel.Layout();

            GuiRoot.ShowDialog(TradePanel);
            GuiRoot.RootItem.SendToBack(TradePanel);

            AutoHideBubble = true;

            SpeakerBorder.Rect = new Rectangle(16, GuiRoot.RenderData.VirtualScreen.Height - (256 - 16), 256 - 32, 256 - 32);
            SpeakerRectangle = Gui.Mesh.Quad().Scale(256, 256).Translate(0, GuiRoot.RenderData.VirtualScreen.Height - 256);
            _Output.Rect = new Rectangle(256, GuiRoot.RenderData.VirtualScreen.Height - 260, System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 550), 260);

            SpeakerBorder.Invalidate();
            _Output.Invalidate();
            GuiRoot.RootItem.Invalidate();
        }

        public void EndTrade()
        {
            TradePanel.Close();
            TradePanel = null;
            AutoHideBubble = false;
            PositionItems();
        }

        public void WaitForTrade(Action<Gui.Widgets.TradeDialogResult, Trade.TradeTransaction> Callback)
        {
            TradePanel.Reset();
            TradePanel.OnPlayerAction = (sender) => Callback(TradePanel.Result, TradePanel.Transaction);
        }
    }
}
