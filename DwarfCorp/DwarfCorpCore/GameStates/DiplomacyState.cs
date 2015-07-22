using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{

    public class SpeakerComponent : GUIComponent
    {
        public AnimatedImagePanel Actor { get; set; }
        public Panel SpeechBubble { get; set; }
        public Label SpeechLabel { get; set; }
        public int ActorSize { get; set; }

        public SpeakerComponent()
        {
            
        }

        public SpeakerComponent(DwarfGUI gui, GUIComponent parent, Animation animation) :
            base(gui, parent)
        {
            ActorSize = 256;
            Actor = new AnimatedImagePanel(GUI, this, animation)
            {
                KeepAspectRatio = true
            };

            SpeechBubble = new Panel(GUI, this)
            {
                Mode = Panel.PanelMode.SpeechBubble
            };

            SpeechLabel = new Label(GUI, SpeechBubble, "", GUI.DefaultFont)
            {
                Alignment = Drawer2D.Alignment.Center,
                WordWrap = true
            };

        }

        public override void Update(DwarfTime time)
        {
            Actor.LocalBounds = new Rectangle(0, LocalBounds.Height - ActorSize, ActorSize, ActorSize);
            SpeechBubble.LocalBounds = new Rectangle(ActorSize, 0, LocalBounds.Width - ActorSize, LocalBounds.Height - ActorSize/2);
            SpeechLabel.LocalBounds = new Rectangle(10, 10, SpeechBubble.LocalBounds.Width - 10, SpeechBubble.LocalBounds.Height - 10);
            base.Update(time);
        }

        public void Say(string text)
        {
            SpeechLabel.Text = text;
            Actor.Animation.Play();
        }

        public void Stop()
        {
            SpeechLabel.Text = "";
            SpeechLabel.IsVisible = false;
            SpeechBubble.IsVisible = false;
            Actor.Animation.Stop();
        }


    }

    /// <summary>
    /// A node in a dialouge tree.
    /// </summary>
    public class SpeechNode
    {
        /// <summary>
        /// A link between speech nodes. Uses an arbitrary function as a link.
        /// </summary>
        public class SpeechAction
        {
            public string Text { get; set; }
            public Func<IEnumerable<SpeechNode> > Action { get; set; } 
        }

        /// <summary>
        /// The text to display during the dialouge.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// A list of outgoing links to other nodes.
        /// </summary>
        public List<SpeechAction> Actions { get; set; }

        public static IEnumerable<SpeechNode> Echo(SpeechNode other)
        {
            yield return other;
            yield break;
        }
    }

    /// <summary>
    /// This game state allows the player to buy/sell goods from a balloon with a drag/drop interface.
    /// </summary>
    public class DiplomacyState : GameState
    {
        public DwarfGUI GUI { get; set; }
        public Drawer2D Drawer { get; set; }
        public Panel MainWindow { get; set; }
        public int EdgePadding { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }
        public PlayState PlayState { get; set; }
        public Dictionary<string, GUIComponent> Tabs { get; set; }
        public Faction Faction { get; set; }
        public SpeakerComponent Talker { get; set; }
        public SpeechNode DialougeTree { get; set; }
        public SpeechNode CurrentNode { get; set; }
        public Faction PlayerFation
        {
            get { return PlayState.PlayerFaction; }
        }
        public Panel SpeechBubble { get; set; }
        public Label SpeechLabel { get; set; }
        public string TalkerName { get; set; }
        public TradeEvent LastEvent { get; set; }
        public ListSelector DialougeSelector { get; set; }
        public SpeechNode.SpeechAction CurrentAction { get; set; }
        public IEnumerable<SpeechNode> CurrentCoroutine { get; set; }
        public IEnumerator<SpeechNode> CurrentEnumerator { get; set; }
 
        public Diplomacy.Politics Politics
        {
            get { return PlayState.Diplomacy.GetPolitics(PlayState.PlayerFaction, Faction); }
        }
        public DiplomacyState(DwarfGame game, GameStateManager stateManager, PlayState play, Faction faction) :
            base(game, "DiplomacyState", stateManager)
        {

            EdgePadding = 32;
            Input = new InputManager();
            PlayState = play;
            EnableScreensaver = false;
            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;
            Faction = faction;
              
        }

        public void DoTrade(TradeEvent trade)
        {
            PlayerFation.RemoveResources(trade.GoodsSent, Vector3.Zero, false);

            foreach (ResourceAmount resource in trade.GoodsReceived)
            {
                PlayerFation.AddResources(resource);
            }
        }

        IEnumerable<SpeechNode> WaitForTrade()
        {
            TradeDialog dialog = TradeDialog.Popup(GUI, Layout, Faction);

            LastEvent = null;
            dialog.OnTraded += dialog_OnClicked;
            while (LastEvent == null && dialog.IsVisible)
            {
                yield return null;
            }

            if (LastEvent != null)
            {
                TradeEvent.Profit profit = LastEvent.GetProfit();

                if (profit.PercentProfit > 0.25f)
                {
                    DoTrade(LastEvent);
                    yield return new SpeechNode()
                    {
                        Text = GetGoodTradeText(),
                        Actions = new List<SpeechNode.SpeechAction>()
                        {
                            new SpeechNode.SpeechAction()
                            {
                                Text = "Ok",
                                Action = () => SpeechNode.Echo(DialougeTree)
                            }
                        }
                    };
                }
                else
                {
                    yield return new SpeechNode()
                    {
                        Text = GetBadTradeText(),
                        Actions = new List<SpeechNode.SpeechAction>()
                        {
                            new SpeechNode.SpeechAction()
                            {
                                Text = "Sorry.",
                                Action = () => SpeechNode.Echo(DialougeTree)
                            }
                        }
                    };
                }
                yield break;
            }
            else yield return DialougeTree;
            yield break;
        }

        void Initialize()
        {
            TalkerName = TextGenerator.GenerateRandom(Datastructures.SelectRandom(Faction.Race.NameTemplates).ToArray());
            Tabs = new Dictionary<string, GUIComponent>();
            GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default), 
                Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), 
                Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input)
            {
                DebugDraw = false
            };
            IsInitialized = true;
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            MainWindow = new Panel(GUI, GUI.RootComponent)
            {
                LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2)
            };

            Layout = new GridLayout(GUI, MainWindow, 11, 4);
            Layout.UpdateSizes();

            Talker = new SpeakerComponent(GUI, Layout, new Animation(Faction.Race.TalkAnimation));
            Layout.SetComponentPosition(Talker, 0, 0, 4, 4);

            DialougeSelector = new ListSelector(GUI, Layout)
            {
                Mode = ListItem.SelectionMode.ButtonList,
                DrawPanel = false,
                Label = ""
            };
            DialougeSelector.OnItemSelected += DialougeSelector_OnItemSelected;
            Layout.SetComponentPosition(DialougeSelector, 2, 2, 2, 10);

            Button back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow));
            Layout.SetComponentPosition(back, 0, 10, 1, 1);
            back.OnClicked += back_OnClicked;


            DialougeTree = new SpeechNode()
            {
                Text = GetGreeting(),
                Actions = new List<SpeechNode.SpeechAction>()
                {
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Let's trade.",
                        Action = WaitForTrade
                    },
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Goodbye.",
                        Action = () => SpeechNode.Echo(new SpeechNode()
                        {
                            Text = GetFarewell(),
                            Actions = new List<SpeechNode.SpeechAction>()
                        })
                    }
                }
            };

            Transition(DialougeTree);
            Politics.HasMet = true;

            PlayerFation.AddResources(new ResourceAmount(ResourceLibrary.ResourceType.Mana, 64));
        }

        void dialog_OnClicked(TradeEvent e)
        {
            LastEvent = e;
        }

        void DialougeSelector_OnItemSelected(int index, ListItem item)
        {
            if (CurrentNode != null)
            {
                CurrentAction = CurrentNode.Actions[index];
            }
        }

        public void Transition(SpeechNode node)
        {
            CurrentNode = node;
            Talker.Say(node.Text);
            DialougeSelector.ClearItems();
            foreach (SpeechNode.SpeechAction action in node.Actions)
            {
                DialougeSelector.AddItem("- " + action.Text);
            }
        }

        private string GetGoodTradeText()
        {
            return  Datastructures.SelectRandom(Faction.Race.Speech.GoodTrades);
        }

        private string GetBadTradeText()
        {
            return Datastructures.SelectRandom(Faction.Race.Speech.BadTrades);
        }


        private string GetFarewell()
        {
            string greeting = "";
            greeting = Datastructures.SelectRandom(Faction.Race.Speech.Farewells);
            return greeting;
        }

        private string GetGreeting()
        {
            string greeting = "";
            greeting = Datastructures.SelectRandom(Faction.Race.Speech.Greetings);
            greeting += " I am " + TalkerName + " of " + Faction.Name + ". We have come to trade.";
            return greeting;
        }

        void InputManager_KeyReleasedCallback(Microsoft.Xna.Framework.Input.Keys key)
        {
            if (!IsActiveState)
            {
                return;
            }

            if (key == Keys.Escape)
            {
                back_OnClicked();
            }
        }


        public override void OnEnter()
        {
            PlayState.GUI.ToolTipManager.ToolTip = "";
            PlayState.Paused = true;
            Initialize();
            
            base.OnEnter();
        }

        public override void OnExit()
        {
            PlayState.Paused = false;
            base.OnExit();
        }


        private void back_OnClicked()
        {
            PlayState.GUI.RootComponent.IsVisible = true;
            StateManager.PopState();
        }

        public override void Update(DwarfTime gameTime)
        {
            CompositeLibrary.Update();
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Input.Update();
            GUI.Update(gameTime);

            if (CurrentAction != null)
            {
                if (CurrentCoroutine == null)
                {
                    CurrentCoroutine = CurrentAction.Action();
                    CurrentEnumerator = CurrentCoroutine.GetEnumerator();
                }

                if (CurrentCoroutine != null)
                {
                    SpeechNode node = CurrentEnumerator.Current;
                   
                    if (node != null)
                    {
                        Transition(node);
                        CurrentCoroutine = null;
                        CurrentAction = null;
                    }
                    else
                    {
                        if (!CurrentEnumerator.MoveNext())
                        {
                            CurrentCoroutine = null;
                            CurrentAction = null;
                            CurrentEnumerator = null;
                            Transition(DialougeTree);
                        }
                    }
                }
            }

            base.Update(gameTime);
        }


        private void DrawGUI(DwarfTime gameTime, float dx)
        {
            RasterizerState rasterizerState = new RasterizerState()
            {
                ScissorTestEnable = true
            };

            GUI.PreRender(gameTime, DwarfGame.SpriteBatch);

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizerState);

            Drawer2D.FillRect(DwarfGame.SpriteBatch, Game.GraphicsDevice.Viewport.Bounds, new Color(0, 0, 0, 200));

            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));

            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.PostRender(gameTime);
            DwarfGame.SpriteBatch.End();
        }

        public override void Render(DwarfTime gameTime)
        {
            DrawGUI(gameTime, 0);
            base.Render(gameTime);
        }
    }

}
