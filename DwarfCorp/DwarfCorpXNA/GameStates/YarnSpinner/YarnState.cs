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
        private Animation IdleAnimation;
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
        public WorldManager World;
        public Color TestBackground = Color.Transparent;

        public YarnState(
            WorldManager world,
            String ConversationFile,
            String StartNode,
            Yarn.MemoryVariableStore Memory) :
            base(Game, "YarnState", GameState.Game.StateManager)
        {
            World = world;
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
            ShowOutput();
            _Output?.ClearText();
            var colon = S.IndexOf(":");
            if (colon != -1)
            {
                var name = S.Substring(0, colon + 1);
                S = S.Substring(colon + 1);
                //_Output?.AppendText(name+"\n");
                TimeSinceOutput = 0.0f;

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

        public void StopSpeaking()
        {
            if (IdleAnimation != null)
            {
                if (SpeakerAnimationPlayer?.GetCurrentAnimation() == IdleAnimation)
                {
                    SpeakerAnimationPlayer?.ChangeAnimation(IdleAnimation, AnimationPlayer.ChangeAnimationOptions.Play);
                }
                else
                {
                    SpeakerAnimationPlayer?.ChangeAnimation(IdleAnimation, AnimationPlayer.ChangeAnimationOptions.ResetAndPlay);
                }
            }
            else
            {
                SpeakerAnimationPlayer?.Stop();
            }
        }

        public void StartSpeaking()
        {
            _hideEverything = false;
            if (IdleAnimation != null)
            {
                if (SpeakerAnimationPlayer?.GetCurrentAnimation() == SpeakerAnimation)
                {
                    SpeakerAnimationPlayer?.ChangeAnimation(SpeakerAnimation, AnimationPlayer.ChangeAnimationOptions.Play);
                }
                else
                {
                    SpeakerAnimationPlayer?.ChangeAnimation(SpeakerAnimation, AnimationPlayer.ChangeAnimationOptions.ResetAndPlay);
                }
            }
            else
            {
                SpeakerAnimationPlayer?.Play();
            }
        }
        private bool _hideEverything = false;
        public bool AdvanceSpeech(DwarfTime gameTime)
        {
            if (!SkipNextLine)
            {
                if (CurrentSpeach.MoveNext())
                {
                    SpeakerAnimationPlayer?.Update(gameTime, false, Timer.TimerMode.Real);
                    if (CurrentSpeach.Current == "" ||  CurrentSpeach.Current == "#EOL")
                    {
                        StopSpeaking();
                        if (CurrentSpeach.Current == "#EOL" && _currentEaseOut > _targetEase * 1.1f)
                        {
                            _currentEaseOut = 0.0f;
                            _currentEase = 1.0f;
                        }
                    }
                    else if (CurrentSpeach.Current == "#DIE")
                    {
                        _hideEverything = true;
                    }
                    else
                    {
                        _hideEverything = false;
                        Output(CurrentSpeach.Current);
                        StartSpeaking();
                    }
                    return true;
                }
                else
                {
                    StopSpeaking();
                    return false;
                }
            }
            else
            {
                Language.IsSkipping = true;
                while (CurrentSpeach.MoveNext())
                    Output(CurrentSpeach.Current);
                StopSpeaking();
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

        public void SetIdle(String Gfx, int FrameWidth, int FrameHeight, float Speed, List<int> Frames)
        {
            IdleAnimation = AnimationLibrary.CreateAnimation(new Animation.SimpleDescriptor
            {
                AssetName = Gfx,
                Speed = 1.0f / Speed,
                Frames = Frames,
                Width = FrameWidth,
                Height = FrameHeight
            });

            IdleAnimation.Loops = true;
            StopSpeaking();
        }

        public void SetPortrait(String Gfx, int FrameWidth, int FrameHeight, float Speed, List<int> Frames)
        {
            IdleAnimation = null;
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
            StartSpeaking();
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
            ShowOutput();
            if (GuiRoot == null)
            {
                GuiRoot = new Gui.Root(DwarfGame.GuiSkin);
                GuiRoot.MousePointer = new Gui.MousePointer("mouse", 4, 0);
                GuiRoot.RootItem.Font = "font8";

                if (TestBackground != Color.Transparent)
                {
                    GuiRoot.RootItem.AddChild(new Widget()
                    {
                        Background = new TileReference("basic", 0),
                        BackgroundColor = TestBackground.ToVector4(),
                        Rect = GuiRoot.RenderData.VirtualScreen
                    });
                }

                _Output = GuiRoot.RootItem.AddChild(new Gui.Widgets.TextBox
                {
                    Border = "speech-bubble-reverse",
                    TextSize = 1,
                    Font = "font16",
                    TextVerticalAlign = VerticalAlign.Center
                }) as Gui.Widgets.TextBox;


                ChoicePanel = GuiRoot.RootItem.AddChild(new Widget
                {
                    Border = null,
                    TextSize = 1,
                    Font = GameSettings.Default.GuiScale == 1 ? "font16" : "font10",
                    Transparent = true
                });

                SpeakerBorder = GuiRoot.RootItem.AddChild(new Widget
                {
                    //Border = "border-dark",
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

        private Rectangle _endRect = new Rectangle();
        private Rectangle _startRect = new Rectangle();
        private float _targetEase = 0.5f;
        private float _currentEase = 0.0f;
        private float _currentEaseOut = 1.0f;
        private Rectangle _startSpeakerRect = new Rectangle();
        private Rectangle _targetSpeakerRect = new Rectangle();

        private void ShowOutput()
        {
            _startRect = _endRect.Interior(50, 20, 20, 50);
            _currentEase = 0.0f;
            _currentEaseOut = 1.0f;
            _hideEverything = false;
        }

        private void PositionItems()
        {
            int w = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 700);
            int h = System.Math.Min(GuiRoot.RenderData.VirtualScreen.Height - 256, 150);
            int x = GuiRoot.RenderData.VirtualScreen.Width / 2 - w / 2;
            int y = System.Math.Max(GuiRoot.RenderData.VirtualScreen.Height / 2 - h / 2, 280);

            _Output.Rect = new Rectangle(x - 64, y - h - h/2, w, h);
            ChoicePanel.Rect = new Rectangle(x, y, w, h);

                int inset = 32;
            SpeakerBorder.Rect = new Rectangle(x - w / 2 + 50 + inset, y - 260 + inset - 32, 256 - inset, 256 - inset);

            if (SpeakerBorder.Rect.X < 0)
            {
                _Output.Rect.X += -SpeakerBorder.Rect.X;
                SpeakerBorder.Rect.X = 0;
            }

            SpeakerRectangle = Gui.Mesh.Quad().Scale(SpeakerBorder.Rect.Width, SpeakerBorder.Rect.Height).Translate(SpeakerBorder.Rect.X, SpeakerBorder.Rect.Y);
            _targetSpeakerRect = SpeakerBorder.Rect;
            _startSpeakerRect = new Rectangle(_targetSpeakerRect.X, _targetSpeakerRect.Y - 128, _targetSpeakerRect.Width, _targetSpeakerRect.Height);
            _Output.Invalidate();
            SpeakerBorder.Invalidate();
            _endRect = _Output.Rect;
            _Output.Rect = _endRect.Interior(50, 20, 20, 50);
            _Output.Hidden = true;
        }

        private Rectangle EaseInRect(Rectangle start, Rectangle end, float t, float maxT)
        {
            float w = Easing.CubeInOut(t, start.Width, end.Width, maxT);
            float h = Easing.CubeInOut(t, start.Height, end.Height, maxT);
            float x = end.Center.X - w / 4;
            float y = end.Center.Y - h / 4;
            return new Rectangle((int)(x), (int)(y), (int)(w * 0.5f), (int)(h * 0.5f));
        }

        private Rectangle EaseOutRect(Rectangle start, Rectangle end, float t, float maxT)
        {
            float w = Easing.CubeInOut(t, end.Width, start.Width, maxT);
            float h = Easing.CubeInOut(t, end.Height, start.Height, maxT);
            float x = end.Center.X - w / 4;
            float y = end.Center.Y - h / 4;
            return new Rectangle((int)(x), (int)(y), (int)(w * 0.5f), (int)(h * 0.5f));
        }

        public override void Update(DwarfTime gameTime)
        {
            float dt = (float)gameTime.ElapsedRealTime.TotalSeconds;
            TimeSinceOutput += dt;
            bool hiding = false;
            if (_currentEaseOut < _targetEase)
            {
                _Output.ClearText();
                _Output.Rect = EaseInRect(_startRect, _endRect, _targetEase - _currentEaseOut, _targetEase);
                _Output.Hidden = _currentEaseOut > _targetEase * 0.9f;
                _Output.Invalidate();
                _currentEaseOut += dt;
                hiding = _Output.Hidden;
            }
            else if (_currentEase < _targetEase)
            {
                _Output.Rect = EaseInRect(_startRect, _endRect, _currentEase, _targetEase);
                _currentEase += dt;
                _Output.Hidden = _currentEase < _targetEase * 0.1f;
                _Output.Invalidate();
                hiding = _Output.Hidden;
            }
            else
            {
                _Output.Hidden = false;
            }
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
            else if (!hiding)
            {
                _Output.Hidden = false;
                SpeakerBorder.Hidden = false;
                GuiRoot.RootItem.Invalidate();
            }

            GuiRoot.Update(gameTime.ToRealTime());
            if (World != null)
                World.TutorialManager.Update(GuiRoot);

            YarnEngine.Update(gameTime);
        }
    
        public override void Render(DwarfTime gameTime)
        {
            if (_hideEverything)
            {
                _Output.Hidden = true;
            }
            GuiRoot.Draw();

            if ((!_hideEverything) && SpeakerVisible && SpeakerAnimationPlayer != null)
            {
                var sheet = SpeakerAnimationPlayer.GetCurrentAnimation().SpriteSheet;
                var frame = SpeakerAnimationPlayer.GetCurrentAnimation().Frames[SpeakerAnimationPlayer.CurrentFrame];
                SpeakerRectangle.ResetQuadTexture();
                SpeakerRectangle.Texture(sheet.TileMatrix(frame.X, frame.Y));
                var mesh = SpeakerRectangle.Copy();
                if (_currentEaseOut < _targetEase * 1.1f)
                {
                    var trans = Easing.BackEaseOut(_targetEase - _currentEaseOut, 0, 128, _targetEase);
                    GuiRoot.DrawMesh(mesh.Translate(0, (int)(128 - trans)), sheet.GetTexture());
                }
                else if (_currentEase < _targetEase * 1.1f)
                {
                    var trans = Easing.BackEaseOut(_currentEase, 0, 128, _targetEase);
                    GuiRoot.DrawMesh(mesh.Translate(0, (int)(128 - trans)), sheet.GetTexture());
                }
                else
                {
                    GuiRoot.DrawMesh(mesh, sheet.GetTexture());
                }
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
            PlayerFaction.World.Tutorial("trade_screen");
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
            _Output.Rect = new Rectangle(256, GuiRoot.RenderData.VirtualScreen.Height - 260, System.Math.Min(GuiRoot.RenderData.VirtualScreen.Width - 256, 800), 180);

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
