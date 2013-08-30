using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
namespace DwarfCorp
{
    public class CompanyMakerState : GameState
    {
        public SillyGUI GUI { get; set; }
        public SpriteFont DefaultFont { get; set; }
        public Drawer2D Drawer { get; set; }
        public Panel MainWindow { get; set; }
        public int EdgePadding { get; set; }
        public GridLayout Layout { get; set; }
        public InputManager Input { get; set; }

        public string CompanyName { get; set; }
        public Texture2D CompanyLogo { get; set; }
        public ImagePanel CompanyLogoPanel { get; set; }
        public string CompanyMotto { get; set; }

        LineEdit CompanyNameEdit { get; set; }
        LineEdit CompanyMottoEdit { get; set; }
        TextureLoadDialog Loader { get; set; }

        public TextGenerator TextGenerator { get; set; }


        public CompanyMakerState(DwarfGame game, GameStateManager stateManager) :
            base(game, "CompanyMakerState", stateManager)
        {
            CompanyName = PlayerSettings.Default.CompanyName;
            CompanyMotto = PlayerSettings.Default.CompanyMotto;
            CompanyLogo = TextureManager.GetTexture("CompanyLogo");
            EdgePadding = 32;
            Input = new InputManager();
            Drawer = new Drawer2D(Game.Content, Game.GraphicsDevice);
            TextGenerator = new TextGenerator();

            
        }

        public override void OnEnter()
        {

            IsInitialized = true;
            DefaultFont = Game.Content.Load<SpriteFont>("Default");
            GUI = new SillyGUI(Game, DefaultFont, Game.Content.Load<SpriteFont>("Title"),  Game.Content.Load<SpriteFont>("Small"), Input);
            MainWindow = new Panel(GUI, GUI.RootComponent);
            MainWindow.LocalBounds = new Rectangle(EdgePadding, EdgePadding, Game.GraphicsDevice.Viewport.Width - EdgePadding * 2, Game.GraphicsDevice.Viewport.Height - EdgePadding * 2);
            Layout = new GridLayout(GUI, MainWindow, 10, 4);

            Label title = new Label(GUI, Layout, "Create a Company", GUI.TitleFont);
            Layout.SetComponentPosition(title, 0, 0, 1, 1);

            Label companyNameLabel = new Label(GUI, Layout, "Name", GUI.DefaultFont);
            Layout.SetComponentPosition(companyNameLabel, 0, 1, 1, 1);

            CompanyNameEdit = new LineEdit(GUI, Layout, CompanyName);
            Layout.SetComponentPosition(CompanyNameEdit, 1, 1, 1, 1);

            Button randomButton = new Button(GUI, Layout, "Random", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(randomButton, 2, 1, 1, 1);
            randomButton.OnClicked += new ClickedDelegate(randomButton_OnClicked);

            Label companyMottoLabel = new Label(GUI, Layout, "Motto", GUI.DefaultFont);
            Layout.SetComponentPosition(companyMottoLabel, 0, 2, 1, 1);

            CompanyMottoEdit = new LineEdit(GUI, Layout, CompanyMotto);
            Layout.SetComponentPosition(CompanyMottoEdit, 1, 2, 1, 1);
            CompanyMottoEdit.OnTextModified += new LineEdit.Modified(companyMottoEdit_OnTextModified);

            CompanyNameEdit.OnTextModified += new LineEdit.Modified(companyNameEdit_OnTextModified);

            Button randomButton2 = new Button(GUI, Layout, "Random", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(randomButton2, 2, 2, 1, 1);
            randomButton2.OnClicked += new ClickedDelegate(randomButton2_OnClicked);

            Label companyLogoLabel = new Label(GUI, Layout, "Logo", GUI.DefaultFont);
            Layout.SetComponentPosition(companyLogoLabel, 0, 3, 1, 1);

            CompanyLogoPanel = new ImagePanel(GUI, Layout, CompanyLogo);
            CompanyLogoPanel.KeepAspectRatio = true;
            Layout.SetComponentPosition(CompanyLogoPanel, 1, 3, 1, 1);


            Button selectorButton = new Button(GUI, Layout, "Select", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(selectorButton, 2, 3, 1, 1);
            selectorButton.OnClicked += new ClickedDelegate(selectorButton_OnClicked);

            Loader = new TextureLoadDialog(GUI, Layout, "CompanyLogo", CompanyLogo);
            Layout.SetComponentPosition(Loader, 0, 4, 6, 4);
            Loader.OnTextureSelected += new TextureLoadDialog.TextureSelected(Loader_OnTextureSelected);
            Loader.IsVisible = false;

            Button apply = new Button(GUI, Layout, "Apply", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(apply, 2, 9, 1, 1);

            apply.OnClicked += new ClickedDelegate(apply_OnClicked);

            Button back = new Button(GUI, Layout, "Back", GUI.DefaultFont, Button.ButtonMode.PushButton, null);
            Layout.SetComponentPosition(back, 3, 9, 1, 1);

            back.OnClicked += new ClickedDelegate(back_onClicked);

            base.OnEnter();
        }

        void selectorButton_OnClicked()
        {
            Loader.IsVisible = true;
        }

        void Loader_OnTextureSelected(TextureLoader.TextureFile arg)
        {
            PlayerSettings.Default.CompanyLogo = arg.File;
            CompanyLogo = arg.Texture;
            CompanyLogoPanel.Image = new ImageFrame(arg.Texture, new Rectangle(0, 0, arg.Texture.Width, arg.Texture.Height));
            Loader.IsVisible = false;
        }

        void randomButton2_OnClicked()
        {
            List<string[]> templates = new List<string[]>();
            string[] adverbweverb = { "$Adverb", ", we ", "$Verb", "!"};
            string[] thing2 = { "$Interjection", ", the ", "$Color", " ", "$Animal", "s ", "$Verb", "!" };
            string[] thing3 = { "$Verb", " my ", "$Bodypart", ", my ", "$Family", "!" };
            string[] thing4 = { "$Verb", "!" };
            string[] thing5 = { "You can't ", "$Verb", " until you ", "$Verb", "!" };
            string[] thing6 = { "$Interjection", " ... the ", "$Material", " ", "$Place", "!" };
            templates.Add(adverbweverb);
            templates.Add(thing2);
            templates.Add(thing3);
            templates.Add(thing5);
            templates.Add(thing4);
            templates.Add(thing6);
            CompanyMotto = TextGenerator.GenerateRandom(templates[PlayState.random.Next(templates.Count)]);
            PlayerSettings.Default.CompanyMotto = CompanyMotto;
            CompanyMottoEdit.Text = CompanyMotto;
        }

        void randomButton_OnClicked()
        {
            List<string[]> templates = new List<string[]>();
            string[] partners = {"$MaleName"," ", "&", " ", "$MaleName", ",", " ", "$Corp"};
            string[] animalCorp = { "$Animal", " ", "$Corp" };
            string[] animalPart = { "$Animal", " ", "$Bodypart" };
            string[] nameAndSons = { "$MaleName", " ", "&", " ", "$Family", "s"};
            string[] colorPart = { "$Color", " ", "$Bodypart", " ", "&", " ", "$Family", "s" };
            string[] colorPlace = { "$Color", " ", "$Place", " ", "$Corp" };
            string[] colorAnimal = { "$Color", " ", "$Animal", " ", "$Corp" };
            string[] materialAnimal = { "$Material", " ", "$Animal", " ", "$Corp" };
            string[] materialBody = { "$Material", " ", "$Bodypart", " ", "$Corp" };
            string[] reversed = { "$Corp", " of the ", "$Material", " ", "$Place", "s"};
            templates.Add(partners);
            templates.Add(animalCorp);
            templates.Add(animalPart);
            templates.Add(nameAndSons);
            templates.Add(colorPart);
            templates.Add(colorPlace);
            templates.Add(colorAnimal);
            templates.Add(materialAnimal);
            templates.Add(materialBody);
            templates.Add(reversed);
            CompanyName = TextGenerator.GenerateRandom(templates[PlayState.random.Next(templates.Count)]);
            PlayerSettings.Default.CompanyName = CompanyName;
            CompanyNameEdit.Text = CompanyName;
        }

        void companyMottoEdit_OnTextModified(string arg)
        {
            CompanyMotto = arg;
            PlayerSettings.Default.CompanyMotto = arg;
        }

        void apply_OnClicked()
        {
            PlayerSettings.Default.Save();
            StateManager.PopState();
        }

        void back_onClicked()
        {
            StateManager.PopState();
        }

        void companyNameEdit_OnTextModified(string arg)
        {
            CompanyName = arg;
            PlayerSettings.Default.CompanyName = arg;
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

            if (Transitioning == TransitionMode.Running)
            {
                Game.GraphicsDevice.Clear(Color.Black);
                Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                DrawGUI(gameTime, 0);
            }
            else if (Transitioning == TransitionMode.Entering)
            {
                float dx = Easing.CubeInOut(TransitionValue, -Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }
            else if (Transitioning == TransitionMode.Exiting)
            {
                float dx = Easing.CubeInOut(TransitionValue, 0, Game.GraphicsDevice.Viewport.Width, 1.0f);
                DrawGUI(gameTime, dx);
            }



            base.Render(gameTime);
        }
    }


}
