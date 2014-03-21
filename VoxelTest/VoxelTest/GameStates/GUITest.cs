using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.GameStates
{

    /// <summary>
    /// This is a debug state for testing new GUI stuff. 
    /// </summary>
    public class GUITest : GameState
    {
        public DwarfGUI GUI { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public Drawer2D Drawer { get; set; }
        public Panel MainWindow { get; set; }
        public int EdgePadding { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }

        public GUITest(DwarfGame game, GameStateManager stateManager) :
            base(game, "GUITest", stateManager)
        {
            EdgePadding = 32;
            Input = new InputManager();
        }

        public override void OnEnter()
        {
            DefaultFont = Game.Content.Load<SpriteFont>("Default");
            GUI = new DwarfGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>("Title"), Game.Content.Load<SpriteFont>("Small"), Input);
            IsInitialized = true;
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            MainWindow = new Panel(GUI, GUI.RootComponent);
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Layout = new GridLayout(GUI, MainWindow, 10, 4);
            Label label = new Label(GUI, Layout, "GUI Elements", GUI.TitleFont);
            Layout.SetComponentPosition(label, 0, 0, 1, 1);

            Checkbox check = new Checkbox(GUI, Layout, "Check 1", GUI.DefaultFont, true);
            Layout.SetComponentPosition(check, 0, 1, 1, 1);

            Checkbox check2 = new Checkbox(GUI, Layout, "Check 2", GUI.DefaultFont, true);
            Layout.SetComponentPosition(check2, 0, 2, 1, 1);

            Button apply = new Button(GUI, Layout, "Apply", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(apply, 2, 9, 1, 1);

            Button back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(back, 3, 9, 1, 1);

            Label sliderLabel = new Label(GUI, Layout, "Slider", GUI.DefaultFont);
            Layout.SetComponentPosition(sliderLabel, 0, 3, 1, 1);
            sliderLabel.Alignment = Drawer2D.Alignment.Right;

            Slider slider = new Slider(GUI, Layout, "Slider", 0, -1000, 1000, Slider.SliderMode.Integer);
            Layout.SetComponentPosition(slider, 1, 3, 1, 1);

            Label comboLabel = new Label(GUI, Layout, "Combobox", GUI.DefaultFont);
            comboLabel.Alignment = Drawer2D.Alignment.Right;

            Layout.SetComponentPosition(comboLabel, 0, 4, 1, 1);

            ComboBox combo = new ComboBox(GUI, Layout);
            combo.AddValue("Foo");
            combo.AddValue("Bar");
            combo.AddValue("Baz");
            combo.AddValue("Quz");
            combo.CurrentValue = "Foo";
            Layout.SetComponentPosition(combo, 1, 4, 1, 1);

            back.OnClicked += back_OnClicked;

            GroupBox groupBox = new GroupBox(GUI, Layout, "");
            Layout.SetComponentPosition(groupBox, 2, 1, 2, 6);
            Layout.UpdateSizes();

            /*
            Texture2D Image = Game.Content.Load<Texture2D>("pine");
            string[] tags = {""};
            DraggableItem image = new DraggableItem(GUI, groupBox,new GItem("Item", new ImageFrame(Image, Image.Bounds), 0, 1, 1, tags));
            image.LocalBounds = new Rectangle(50, 50, Image.Width, Image.Height);

            Label imageLabel = new Label(GUI, image, "Image Panel", GUI.DefaultFont);
            imageLabel.LocalBounds = new Rectangle(0, 0, Image.Width, Image.Height);
            imageLabel.Alignment = Drawer2D.Alignment.Top | Drawer2D.Alignment.Left;
            */

            GridLayout groupLayout = new GridLayout(GUI, groupBox, 1, 2);


            DragManager dragManager = new DragManager();

            DragGrid dragGrid = new DragGrid(GUI, groupLayout, dragManager, 32, 32);
            DragGrid dragGrid2 = new DragGrid(GUI, groupLayout, dragManager, 32, 32);

            groupLayout.SetComponentPosition(dragGrid, 0, 0, 1, 1);
            groupLayout.SetComponentPosition(dragGrid2, 1, 0, 1, 1);
            Layout.UpdateSizes();
            groupLayout.UpdateSizes();
            dragGrid.SetupLayout();
            dragGrid2.SetupLayout();


            foreach(Resource r in ResourceLibrary.Resources.Values)
            {
                if(r.ResourceName != "Container")
                {
                    GItem gitem = new GItem(r.ResourceName, r.Image, 0, 32, 2, 1, r.Tags);
                    gitem.CurrentAmount = 2;
                    dragGrid.AddItem(gitem);
                }
            }

            ProgressBar progress = new ProgressBar(GUI, Layout, 0.7f);
            Label progressLabel = new Label(GUI, Layout, "Progress Bar", GUI.DefaultFont);
            progressLabel.Alignment = Drawer2D.Alignment.Right;

            Layout.SetComponentPosition(progressLabel, 0, 5, 1, 1);
            Layout.SetComponentPosition(progress, 1, 5, 1, 1);


            LineEdit line = new LineEdit(GUI, Layout, "");
            Label lineLabel = new Label(GUI, Layout, "Line Edit", GUI.DefaultFont);
            lineLabel.Alignment = Drawer2D.Alignment.Right;

            Layout.SetComponentPosition(lineLabel, 0, 6, 1, 1);
            Layout.SetComponentPosition(line, 1, 6, 1, 1);

            base.OnEnter();
        }

        private void back_OnClicked()
        {
            StateManager.PopState();
        }

        public override void Update(GameTime gameTime)
        {
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Input.Update();
            GUI.Update(gameTime);
            base.Update(gameTime);
        }


        private void DrawGUI(GameTime gameTime, float dx)
        {
            DwarfGame.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            Drawer.Render(DwarfGame.SpriteBatch, null, Game.GraphicsDevice.Viewport);
            GUI.Render(gameTime, DwarfGame.SpriteBatch, new Vector2(dx, 0));

            DwarfGame.SpriteBatch.End();
        }

        public override void Render(GameTime gameTime)
        {
            if(Transitioning == TransitionMode.Running)
            {
                Game.GraphicsDevice.Clear(Color.Black);
                Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                DrawGUI(gameTime, 0);
            }
            else if(Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }
            else if(Transitioning == TransitionMode.Exiting)
            {
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }


            base.Render(gameTime);
        }
    }

}