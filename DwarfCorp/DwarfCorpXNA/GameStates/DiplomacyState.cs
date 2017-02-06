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
        public Timer SayTimer { get; set; }
        public Panel ActorPanel { get; set; }
        public SpeakerComponent()
        {
            
        }

        public SpeakerComponent(DwarfGUI gui, GUIComponent parent, Animation animation) :
            base(gui, parent)
        {
            SayTimer = new Timer(5.0f, false, Timer.TimerMode.Real);
            ActorSize = 256;

            ActorPanel = new Panel(GUI, this)
            {
                DrawOrder = -2,
                Mode = Panel.PanelMode.Simple
            };
           
            ScrollingAnimation anim = new ScrollingAnimation(GUI, ActorPanel)
            {
                Image = new NamedImageFrame(ContentPaths.GUI.background),
                ScrollSpeed = new Vector2(10, 0),
                Tint = Color.White,
                DrawOrder = -2,
                LocalBounds = new Rectangle(0, 0, ActorSize, ActorSize)
            };

            Actor = new AnimatedImagePanel(GUI, this, animation)
            {
                KeepAspectRatio = true
            };

            animation.Loops = true;
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
            if (Actor.Animation.IsPlaying)
            {
                SayTimer.Update(time);

                if (SayTimer.HasTriggered)
                {
                    Actor.Animation.Stop();
                }
            }
            Actor.LocalBounds = new Rectangle(0, LocalBounds.Height - ActorSize, ActorSize, ActorSize);
            ActorPanel.LocalBounds = Actor.LocalBounds;
            SpeechBubble.LocalBounds = new Rectangle(ActorSize, 0, LocalBounds.Width - ActorSize, LocalBounds.Height - ActorSize/2);
            SpeechLabel.LocalBounds = new Rectangle(10, 10, SpeechBubble.LocalBounds.Width - 10, SpeechBubble.LocalBounds.Height - 10);
            base.Update(time);
        }

        public void Say(string text)
        {
            SpeechLabel.Text = text;
            Actor.Animation.Play();
            SayTimer.Reset(0.1f * text.Length);
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
        public GUIComponent MainWindow { get; set; }
        public int EdgePadding { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }
        public WorldManager PlayState { get; set; }
        public Dictionary<string, GUIComponent> Tabs { get; set; }
        public Faction Faction { get; set; }
        public SpeakerComponent Talker { get; set; }
        public SpeechNode DialougeTree { get; set; }
        public SpeechNode CurrentNode { get; set; }
        public Faction PlayerFation
        {
            get { return DwarfGame.World.PlayerFaction; }
        }
        public Panel SpeechBubble { get; set; }
        public Label SpeechLabel { get; set; }
        public string TalkerName { get; set; }
        public TradeEvent LastEvent { get; set; }
        public ListSelector DialougeSelector { get; set; }
        public SpeechNode.SpeechAction CurrentAction { get; set; }
        public IEnumerable<SpeechNode> CurrentCoroutine { get; set; }
        public IEnumerator<SpeechNode> CurrentEnumerator { get; set; }
        public SpeechNode PreeTree { get; set; }
        public List<ResourceAmount> Resources { get; set; }
    
        public Diplomacy.Politics Politics
        {
            get { return DwarfGame.World.ComponentManager.Diplomacy.GetPolitics(DwarfGame.World.PlayerFaction, Faction); }
        }
        public Button BackButton { get; set; }
        public Faction.TradeEnvoy Envoy { get; set; }

        public DiplomacyState(DwarfGame game, GameStateManager stateManager, WorldManager play, Faction.TradeEnvoy envoy) :
            base(game, "DiplomacyState", stateManager)
        {

            EdgePadding = 128;
            Input = new InputManager();
            PlayState = play;
            EnableScreensaver = false;
            InputManager.KeyReleasedCallback += InputManager_KeyReleasedCallback;
            Faction = envoy.OwnerFaction;
            Resources = envoy.TradeGoods;
        }


        public void DoTrade(TradeEvent trade)
        {
            PlayerFation.RemoveResources(trade.GoodsSent, Vector3.Zero, false);

            foreach (ResourceAmount resource in trade.GoodsReceived)
            {
                PlayerFation.AddResources(resource);

                List<ResourceAmount> removals = new List<ResourceAmount>();
                foreach (ResourceAmount other in Resources)
                {
                    if (other.ResourceType != resource.ResourceType) continue;
                    other.NumResources -= resource.NumResources;

                    if (other.NumResources <= 0)
                    {
                        removals.Add(other);
                    }
                }

                Resources.RemoveAll(removals.Contains);
            }

            foreach (ResourceAmount other in trade.GoodsSent)
            {
                if (Resources.All(r => r.ResourceType != other.ResourceType))
                {
                    Resources.Add(other);
                }
                else
                {
                    ResourceAmount other1 = other;
                    foreach (
                        ResourceAmount r in Resources.Where(k => k.ResourceType == other1.ResourceType))
                    {
                        r.NumResources += other.NumResources;
                    }
                }
            }
            Envoy.DistributeGoods();
            PlayerFation.Economy.CurrentMoney -= trade.MoneySent;
            PlayerFation.Economy.CurrentMoney += trade.MoneyReceived;
            Envoy.TradeMoney -= trade.MoneyReceived;
            Envoy.TradeMoney += trade.MoneySent;
        }

        IEnumerable<SpeechNode> WaitForTrade()
        {
            TradeDialog dialog = TradeDialog.Popup(GUI, GUI.RootComponent, Faction, Resources);

            LastEvent = null;
            dialog.OnTraded += dialog_OnClicked;



            while (LastEvent == null && dialog.IsVisible)
            {
                yield return null;
            }

            if (LastEvent != null)
            {
                TradeEvent.Profit profit = LastEvent.GetProfit();

                if (LastEvent.IsHate() && !Politics.HasEvent("you tried to give us something offensive"))
                {
                    Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                    {
                        Change = -0.25f,
                        Description = "you tried to give us something offensive",
                        Duration = new TimeSpan(4, 0, 0, 0),
                        Time = DwarfGame.World.Time.CurrentDate
                    });
                }
                else if ((!LastEvent.IsHate() && LastEvent.IsLike()) && !Politics.HasEvent("you gave us something we liked"))
                {
                    Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                    {
                        Change = 0.25f,
                        Description = "you gave us something we liked",
                        Duration = new TimeSpan(4, 0, 0, 0),
                        Time = DwarfGame.World.Time.CurrentDate
                    });
                }

                if (profit.PercentProfit > 0.25f && !LastEvent.IsHate())
                {
                    DoTrade(LastEvent);

                    if (!Politics.HasEvent("we had profitable trade"))
                    {
                        Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                        {
                            Change = 0.25f,
                            Description = "we had profitable trade",
                            Duration = new TimeSpan(2, 0, 0, 0),
                            Time = DwarfGame.World.Time.CurrentDate
                        });
                    }

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
            Envoy.TradeMoney = Faction.TradeMoney + MathFunctions.Rand(-100.0f, 100.0f);
            Envoy.TradeMoney = Math.Max(Envoy.TradeMoney, 0.0f);
            TalkerName = TextGenerator.GenerateRandom(Datastructures.SelectRandom(Faction.Race.NameTemplates).ToArray());
            Tabs = new Dictionary<string, GUIComponent>();
            GUI = new DwarfGUI(Game, Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Default), 
                Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Title), 
                Game.Content.Load<SpriteFont>(ContentPaths.Fonts.Small), Input)
            {
                DebugDraw = false
            };
            IsInitialized = true;
            MainWindow = new GUIComponent(GUI, GUI.RootComponent)
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
                DrawButtons = true,
                DrawPanel = false,
                Label = "",
                ItemHeight = 35,
                Padding = 5
            };
            DialougeSelector.OnItemSelected += DialougeSelector_OnItemSelected;
            Layout.SetComponentPosition(DialougeSelector, 2, 3, 1, 8);

            BackButton = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.LeftArrow));
            Layout.SetComponentPosition(BackButton, 2, 10, 1, 1);
            BackButton.OnClicked += back_OnClicked;
            BackButton.IsVisible = false;
            DialougeTree = new SpeechNode()
            {
                Text = GetGreeting(),
                Actions = new List<SpeechNode.SpeechAction>()
                {
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Trade...",
                        Action = WaitForTrade
                    },
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Ask a question...",
                        Action = AskAQuestion
                    },
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Declare war!",
                        Action = DeclareWar
                    },
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Leave",
                        Action = () =>
                        {
                            BackButton.IsVisible = true;
                            if(Envoy != null)
                                Diplomacy.RecallEnvoy(Envoy);
                            return SpeechNode.Echo(new SpeechNode()
                            {
                                Text = GetFarewell(),
                                Actions = new List<SpeechNode.SpeechAction>()
                            });
                        }

                    }
                }
            };


            if (Politics.WasAtWar)
            {
                PreeTree = new SpeechNode()
                {
                    Text = Datastructures.SelectRandom(Faction.Race.Speech.PeaceDeclarations),
                    Actions = new List<SpeechNode.SpeechAction>()
                    {
                        new SpeechNode.SpeechAction()
                        {
                            Text = "Make peace with " + Faction.Name,
                            Action = () =>
                            {
                                if (!Politics.HasEvent("you made peace with us"))
                                {

                                    Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                                    {
                                        Change = 0.4f,
                                        Description = "you made peace with us",
                                        Duration = new TimeSpan(4, 0, 0, 0),
                                        Time = DwarfGame.World.Time.CurrentDate
                                    });
                                }
                                return SpeechNode.Echo(DialougeTree);
                            }
                        },
                        new SpeechNode.SpeechAction()
                        {
                            Text = "Continue the war with " + Faction.Name,
                            Action = DeclareWar
                        }
                    }
                };
                Transition(PreeTree);
                Politics.WasAtWar = false;
            }
            else
            {
                Transition(DialougeTree);                
            }


            if (!Politics.HasMet)
            {
                Politics.HasMet = true;

                Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                {
                    Change = 0.0f,
                    Description = "we just met",
                    Duration = new TimeSpan(1, 0, 0, 0),
                    Time = DwarfGame.World.Time.CurrentDate
                });
            }

            Layout.UpdateSizes();
            Talker.TweenIn(Drawer2D.Alignment.Top, 0.25f);
            DialougeSelector.TweenIn(Drawer2D.Alignment.Right, 0.25f);
        }

        private IEnumerable<SpeechNode> DeclareWar()
        {
            BackButton.IsVisible = true;
            if (!Politics.HasEvent("you declared war on us"))
            {
                Politics.RecentEvents.Add(new Diplomacy.PoliticalEvent()
                {
                    Change = -2.0f,
                    Description = "you declared war on us",
                    Duration = new TimeSpan(4, 0, 0, 0),
                    Time = DwarfGame.World.Time.CurrentDate
                });
                Politics.WasAtWar = true;
            }
            yield return new SpeechNode()
            {
                Text = Datastructures.SelectRandom(Faction.Race.Speech.WarDeclarations),
                Actions = new List<SpeechNode.SpeechAction>()
            };
        }

        private IEnumerable<SpeechNode> AskAQuestion()
        {
            yield return new SpeechNode()
            {
                Text = "Ask...",
                Actions = new List<SpeechNode.SpeechAction>()
                {
                    new SpeechNode.SpeechAction()
                    {
                        Text = "What do you think of us?",
                        Action = WhatDoYouThink
                    },
                    new SpeechNode.SpeechAction()
                    {
                        Text = "What's the news?",
                        Action = WhatsTheNews
                    },
                    new SpeechNode.SpeechAction()
                    {
                        Text = "What goods do you want?",
                        Action = WhatGoods
                    },
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Nevermind",
                        Action = () => SpeechNode.Echo(DialougeTree)
                    }
                }
            };
        }



        private IEnumerable<SpeechNode> WhatGoods()
        {
            string goods = "We desire ";

            List<string> goodList = Faction.Race.LikedResources.Select(tags => tags.ToString()).ToList();

            goods += TextGenerator.GetListString(goodList);
            goods += " items";

            if (Faction.Race.HatedResources.Any())
            {
                goods += ". But we find ";
                goodList = Faction.Race.HatedResources.Select(tags => tags.ToString()).ToList();
                goods += TextGenerator.GetListString(goodList);
                goods += " items offensive";
            }
            goods += ".";

            yield return new SpeechNode()
            {
                Text = goods,
                Actions = new List<SpeechNode.SpeechAction>()
                {
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Tell me more",
                        Action = () => CommonGoods()
                    },
                    new SpeechNode.SpeechAction()
                    {
                        Text = "OK",
                        Action = () => SpeechNode.Echo(DialougeTree)
                    }
                }
            };
        }


        private IEnumerable<SpeechNode> CommonGoods()
        {
            string goods = "We already have a lot of ";

            List<string> goodList = Faction.Race.CommonResources.Select(tags => tags.ToString()).ToList();

            goods += TextGenerator.GetListString(goodList);
            goods += " things";

            goods += ". We don't have many ";
            goodList = Faction.Race.RareResources.Select(tags => tags.ToString()).ToList();
            goods += TextGenerator.GetListString(goodList);
            goods += " goods";
            goods += ".";

            yield return new SpeechNode()
            {
                Text = goods,
                Actions = new List<SpeechNode.SpeechAction>()
                {
                    new SpeechNode.SpeechAction()
                    {
                        Text = "OK",
                        Action = () => SpeechNode.Echo(DialougeTree)
                    }
                }
            };
        }

        private IEnumerable<SpeechNode> WhatsTheNews()
        {
            yield return new SpeechNode()
            {
                Text = "The world is the same as ever.",
                Actions = new List<SpeechNode.SpeechAction>()
                {
                    new SpeechNode.SpeechAction()
                    {
                        Text = "Interesting",
                        Action = () => SpeechNode.Echo(DialougeTree)
                    }
                }
            };
        }

        private IEnumerable<SpeechNode> WhatDoYouThink()
        {
            Diplomacy.Politics p = DwarfGame.World.ComponentManager.Diplomacy.GetPolitics(Faction, PlayerFation);
            Relationship r = p.GetCurrentRelationship();
            string relationship = "So far, our relationship has been " + r;

            if (p.RecentEvents.Count > 0)
            {
                relationship += ", because ";
                List<string> events = p.RecentEvents.Select(e => e.Description).ToList();
                relationship += TextGenerator.GetListString(events);
            }

            relationship += ".";
            yield return new SpeechNode()
            {
                Text = relationship,
                Actions = new List<SpeechNode.SpeechAction>()
                    {
                        new SpeechNode.SpeechAction()
                        {
                            Text = "Interesting",
                            Action = () => SpeechNode.Echo(DialougeTree)
                        }
                    }
            };

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
                DialougeSelector.AddItem(action.Text);
            }
        }

        private string GetGoodTradeText()
        {
            return  Datastructures.SelectRandom(Faction.Race.Speech.GoodTrades);
        }

        private string GetBadTradeText()
        {
            string generic = Datastructures.SelectRandom(Faction.Race.Speech.BadTrades);

            if (LastEvent.IsHate())
            {
                generic += " We are offended by this trade.";
            }
            return generic;
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
            greeting += " I am " + TalkerName + " of " + Faction.Name + ".";
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
            //DwarfGame.World.GUI.RootComponent.IsVisible = false;
            //DwarfGame.World.GUI.ToolTipManager.ToolTip = "";
            DwarfGame.World.Paused = true;
            Initialize();

            base.OnEnter();
        }

        public override void OnExit()
        {
            //DwarfGame.World.GUI.RootComponent.IsVisible = true;
            DwarfGame.World.Paused = false;
            base.OnExit();
        }


        private void back_OnClicked()
        {
            //DwarfGame.World.GUI.RootComponent.IsVisible = true;
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

            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, rasterizerState);

            Drawer2D.FillRect(DwarfGame.SpriteBatch, Game.GraphicsDevice.Viewport.Bounds, new Color(0, 0, 0, 150));

            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));

            Drawer2D.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
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
