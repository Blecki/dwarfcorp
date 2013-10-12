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



    public class GameMaster
    {

        #region helperclasses

        public enum ToolMode
        {
            Dig,
            Build,
            SelectUnits,
            Chop,
            Guard,
            CreateStockpiles, 
            Gather
        }

        public class Designation
        {
            public Voxel vox = null;
            public int numCreaturesAssigned = 0;
        }

        #endregion

        public class ShipDesignation
        {
            public ResourceAmount Resource { get ; set;}
            public Room Port { get; set; }

            public ShipDesignation(ResourceAmount resource, Room port)
            {
                Resource = resource;
                Port = port;
            }
        }

        public Economy Economy { get; set; }

        public ComponentManager Components { get; set; }
        public ChunkManager Chunks { get; set; }
        public Camera CameraController { get; set; }
        public GraphicsDevice Graphics { get; set; }
        public VoxelLibrary Library { get; set; }
        public ToolMode CurrentTool { get; set; }

        public List<Designation> DigDesignations { get; set; }
        public List<Designation> GuardDesignations { get; set; }
        public List<LocatableComponent> ChopDesignations { get; set; }
        public List<LocatableComponent> GatherDesignations { get; set; }
        public List<Stockpile> Stockpiles { get; set; }
        public List<CreatureAIComponent> Minions { get; set; }
        public List<ShipDesignation> ShipDesignations { get; set; }

        public RoomDesignator RoomDesignator { get; set; }
        public PutDesignator PutDesignator { get; set; }

        public Color DigDesignationColor { get; set; }
        public Color UnreachableColor { get; set; }
        public Color GuardDesignationColor { get; set; }
        public float GuardDesignationGlowRate { get; set; }
        public float DigDesignationGlowRate { get; set; }

        public ContentManager Content { get; set; }

        public SillyGUI GUI { get; set; }
        public MasterControls ToolBar { get; set; }


        public GodModeController GodMode { get; set; }

        public VoxelSelector VoxSelector { get; set; }

        public AIDebugger Debugger { get; set; }

        public TaskManager TaskManager { get; set; }

        public static ImageFrame GetSubTexture(GraphicsDevice graphics, Texture2D image, Rectangle rect)
        {
            return new ImageFrame(image, rect);
        }



        public GameMaster(DwarfGame game, ComponentManager components, ChunkManager chunks, Camera camera, GraphicsDevice graphics, VoxelLibrary library, SillyGUI gui)
        {
            RoomLibrary.InitializeStatics();
            ShipDesignations = new List<ShipDesignation>(); 
            Components = components;
            Chunks = chunks;
            CameraController = camera;
            Graphics = graphics;
            Library = library;
            CurrentTool = ToolMode.Dig;
            DigDesignations = new List<Designation>();
            GatherDesignations = new List<LocatableComponent>();
            DigDesignationColor = new Color(180, 200, 30);
            GuardDesignationColor = new Color(170, 180, 255);
            UnreachableColor = new Color(200, 30, 10);
            DigDesignationGlowRate = 2.0f;
            GuardDesignationGlowRate = 1.5f;
            GuardDesignations = new List<Designation>();
            ChopDesignations = new List<LocatableComponent>();
            Stockpiles = new List<Stockpile>();
            VoxSelector = new VoxelSelector(CameraController, Graphics, Chunks);

            RoomDesignator = new RoomDesignator(this);
            PutDesignator = new PutDesignator(this, chunks.Tilemap);
            Content = game.Content;
            Texture2D imageIcons = game.Content.Load<Texture2D>("icons");


            GUI = gui;

            GodMode = new GodModeController(GUI, this);

            VoxSelector.Selected += OnSelected;
            InputManager.KeyReleasedCallback += OnKeyReleased;

            Minions = new List<CreatureAIComponent>();

            Economy = new Economy(this, 100.0f, 1.0f, 0.75f);
            TaskManager = new TaskManager(this);

        }


        public void Render(GameTime time, GraphicsDevice g)
        {

            if (this.CurrentTool == ToolMode.Dig)
            {
                foreach (Designation d in DigDesignations)
                {
                    Voxel v = d.vox;

                    BoundingBox box = v.GetBoundingBox();


                    Color drawColor = DigDesignationColor;

                    if (d.numCreaturesAssigned == 0)
                    {
                        drawColor = UnreachableColor;
                    }

                    drawColor.R = (byte)(drawColor.R * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * DigDesignationGlowRate)) + 50);
                    drawColor.G = (byte)(drawColor.G * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * DigDesignationGlowRate)) + 50);
                    drawColor.B = (byte)(drawColor.B * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * DigDesignationGlowRate)) + 50);
                    SimpleDrawing.DrawBox(box, drawColor, 0.05f, true);
                }
            }


            if (this.CurrentTool == ToolMode.Guard)
            {
                foreach (Designation d in GuardDesignations)
                {
                    Voxel v = d.vox;

                    if (v != null && v.Primitive != null)
                    {
                        BoundingBox box = v.GetBoundingBox();


                        Color drawColor = GuardDesignationColor;

                        if (d.numCreaturesAssigned == 0)
                        {
                            drawColor = UnreachableColor;
                        }

                        drawColor.R = (byte)(Math.Min(drawColor.R * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                        drawColor.G = (byte)(Math.Min(drawColor.G * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                        drawColor.B = (byte)(Math.Min(drawColor.B * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                        SimpleDrawing.DrawBox(box, drawColor, 0.05f, true);
                    }
                }
            }

            if (this.CurrentTool == ToolMode.CreateStockpiles)
            {
                foreach (Stockpile s in Stockpiles)
                {
                    BoundingBox box = s.GetBoundingBox();
                    box.Max = new Vector3(box.Max.X, box.Max.Y + 0.05f, box.Max.Z);

                    Color drawColor = new Color(150, 160, 100);

                    drawColor.R = (byte)(Math.Min(drawColor.R * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                    drawColor.G = (byte)(Math.Min(drawColor.G * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                    drawColor.B = (byte)(Math.Min(drawColor.B * Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GuardDesignationGlowRate)) + 50, 255));
                    SimpleDrawing.DrawBox(box, drawColor, 0.08f, false);

                    /*
                    foreach (KeyValuePair<Voxel, LocatableComponent> p in s.ComponentVoxels)
                    {
                        if (p.Value == null)
                        {
                            SimpleDrawing.DrawBox(p.Key.GetBoundingBox(), Color.White, 0.05f, true);
                        }
                        else
                        {
                            SimpleDrawing.DrawBox(p.Key.GetBoundingBox(), Color.Yellow, 0.05f, true);
                        }
                    }
                     */
                }
            }

            if (this.CurrentTool == ToolMode.Chop)
            {
                foreach (LocatableComponent component in ChopDesignations)
                {
                    SimpleDrawing.DrawBox(component.GetBoundingBox(), new Color(100, 255, 100), 0.05f);
                }
            }

            if (this.CurrentTool == ToolMode.Gather)
            {
                foreach (LocatableComponent component in GatherDesignations)
                {
                    SimpleDrawing.DrawBox(component.GetBoundingBox(), new Color(255, 150, 50), 0.05f);
                }
            }

            if (this.CurrentTool == ToolMode.Build)
            {
                RoomDesignator.Render(time, Graphics);
                
            }

            VoxSelector.Render();

        }

        public void OnVoxelDestroyed(Voxel v)
        {
            if (v == null)
            {
                return;
            }

            RoomDesignator.OnVoxelDestroyed(v);

            List<Stockpile> toRemove = new List<Stockpile>();
            foreach (Stockpile s in Stockpiles)
            {
                if (s.ComponentVoxels.ContainsKey(v))
                {
                    LocatableComponent componentAtStockpile = s.ComponentVoxels[v];

                    if (null != componentAtStockpile)
                    {
                        componentAtStockpile.IsStocked = false;
                        componentAtStockpile.HasMoved = true;
                    }

                    s.RemoveComponentVoxel(v);

                }

                if (s.ComponentVoxels.Keys.Count == 0)
                {
                    toRemove.Add(s);
                }
            }

            foreach (Stockpile s in toRemove)
            {
                Stockpiles.Remove(s);
            }
        }

        public void Update(DwarfGame game, GameTime time)
        {
            CurrentTool = ToolBar.CurrentMode;


            if (GameSettings.Default.EnableAIDebugger)
            {
                if (Debugger != null)
                {
                    Debugger.Update(time);
                }
            }

            Economy.Update(time);

            CameraController.Update(time, Chunks);

            UpdateInput(game, time);

            RoomDesignator.CheckRemovals();

            TaskManager.AssignTasks();
            TaskManager.ManageTasks();

            List<Designation> removals = new List<Designation>();
            foreach (Designation d in DigDesignations)
            {
                Voxel v = d.vox;

                if(v.Health <= 0.0f || v.Type.name == "empty")
                {
                    removals.Add(d);
                    v.Kill();
                }
            }

            foreach (Designation v in removals)
            {
                DigDesignations.Remove(v);
            }

            removals.Clear();
            foreach (Designation d in GuardDesignations)
            {
                Voxel v = d.vox;

                if (v == null || v.Health <= 0.0f || v.Type.name == "empty")
                {
                    removals.Add(d);

                    if (v != null)
                    {
                        v.Kill();
                    }
                }
            }

            foreach (Designation v in removals)
            {
                GuardDesignations.Remove(v);
            }

            List<LocatableComponent> treesToRemove = new List<LocatableComponent>();

            foreach (LocatableComponent tree in ChopDesignations)
            {
                if (tree.IsDead)
                {
                    treesToRemove.Add(tree);
                }
            }

            foreach (LocatableComponent tree in treesToRemove)
            {
                ChopDesignations.Remove(tree);
            }


            if (ContainerComponent.Containers != null && Stockpiles.Count > 0)
            {
                foreach (ContainerComponent container in ContainerComponent.Containers)
                {
                    if (!container.Container.UserData.IsStocked)
                    {
                        AddGatherDesignation(container.Container.UserData);
                    }
                }

            }




        }

        #region designations

        public Designation GetClosestDigDesignationTo(Vector3 position)
        {
            float closestDist = 99999;
            Designation closestVoxel = null;
            foreach (Designation designation in DigDesignations)
            {
                Voxel v = designation.vox;

                float d = (v.Position - position).LengthSquared();
                if (d < closestDist)
                {
                    closestDist = d;
                    closestVoxel = designation;
                }


            }

            return closestVoxel;
        }

        public Designation GetClosestGuardDesignationTo(Vector3 position)
        {
            float closestDist = 99999;
            Designation closestVoxel = null;
            foreach (Designation designation in GuardDesignations)
            {
                Voxel v = designation.vox;

                float d = (v.Position - position).LengthSquared();
                if (d < closestDist)
                {
                    closestDist = d;
                    closestVoxel = designation;
                }


            }

            return closestVoxel;
        }

        public Designation GetGuardDesignation(Voxel vox)
        {
            foreach (Designation d in GuardDesignations)
            {
                if (vox == d.vox)
                {
                    return d;
                }
            }

            return null;
        }

        public Designation GetDigDesignation(Voxel vox)
        {
            foreach (Designation d in DigDesignations)
            {
                if (vox == d.vox)
                {
                    return d;
                }
            }

            return null;
        }

        public bool IsDigDesignation(Voxel vox)
        {
            foreach (Designation d in DigDesignations)
            {
                if (vox == d.vox)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsGuardDesignation(VoxelRef vox)
        {
            Voxel voxel = vox.GetVoxel(Chunks, false);

            if (voxel == null)
            {
                return false;
            }
            else
            {
                return IsGuardDesignation(voxel);
            }
        }

        public bool IsGuardDesignation(Voxel vox)
        {
            foreach (Designation d in GuardDesignations)
            {
                if (vox == d.vox)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region tools

        public void DigMode(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            VoxSelector.Enabled = true;
            game.IsMouseVisible = true;
            VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
        }

        public void GuardMode(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            VoxSelector.Enabled = true;
            game.IsMouseVisible = true;

            Voxel v = Chunks.GetFirstVisibleBlockHitByMouse(mouseState, CameraController, Graphics.Viewport);
            VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

            if (mouseState.RightButton == ButtonState.Pressed)
            {
                if (IsGuardDesignation(v))
                {
                    GuardDesignations.Remove(GetGuardDesignation(v));
                }
            }

        }

        public void StockpileMode(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            VoxSelector.Enabled = true;
            game.IsMouseVisible = true;
            VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
        }

        public bool IsInStockpile(LocatableComponent resource)
        {
            foreach (Stockpile s in Stockpiles)
            {
                if (s.ComponentVoxels.ContainsValue(resource))
                {
                    return true;
                }
            }

            return false;
        }

        public Stockpile GetIntersectingStockpile(Voxel v)
        {
            foreach (Stockpile pile in Stockpiles)
            {
                if (pile.Intersects(v))
                {
                    return pile;
                }
            }

            return null;
        }

        public bool IsInStockpile(Voxel v)
        {
            foreach (Stockpile s in Stockpiles)
            {
                if (s.ComponentVoxels.ContainsKey(v))
                {
                    return true;
                }
            }

            return false;
        }

        public void GatherMode(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            VoxSelector.Enabled = false;
            game.IsMouseVisible = true;

            List<LocatableComponent> pickedByMouse = new List<LocatableComponent>();
            Components.GetComponentsUnderMouse(mouseState, CameraController, Graphics.Viewport, pickedByMouse);
            List<LocatableComponent> resourcesPickedByMouse = ComponentManager.FilterComponentsWithTag<LocatableComponent>("Resource", pickedByMouse);

            foreach (LocatableComponent resource in resourcesPickedByMouse)
            {

                if (!resource.IsActive || !resource.IsVisible || resource.Parent != Components.RootComponent || IsInStockpile(resource))
                {
                    continue;
                }

                SimpleDrawing.DrawBox(resource.BoundingBox, Color.LightGoldenrodYellow, 0.05f, true);
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    AddGatherDesignation(resource);
                }
                else if (mouseState.RightButton == ButtonState.Pressed)
                {
                    if (GatherDesignations.Contains(resource))
                    {
                        GatherDesignations.Remove(resource);
                        resource.DrawBoundingBox = false;
                        foreach (CreatureAIComponent minion in Minions)
                        {
                            minion.Goap.Goals.Remove("Gather Item: " + resource.Tags[0] + " " + resource.GlobalID);
                        }
                    }

                }
            }

        }

        public void ChopMode(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            VoxSelector.Enabled = false;
            game.IsMouseVisible = true;

            List<LocatableComponent> pickedByMouse = new List<LocatableComponent>();
            Components.GetComponentsUnderMouse(mouseState, CameraController, Graphics.Viewport, pickedByMouse);
            List<LocatableComponent> treesPickedByMouse = ComponentManager.FilterComponentsWithTag<LocatableComponent>("Tree", pickedByMouse);

            foreach (LocatableComponent tree in treesPickedByMouse)
            {

                SimpleDrawing.DrawBox(tree.BoundingBox, Color.LightGreen, 0.1f, false);
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    if (!ChopDesignations.Contains(tree))
                    {
                        ChopDesignations.Add(tree);
                        tree.DrawBoundingBox = true;

                        foreach (CreatureAIComponent component in Minions)
                        {
                            //component.Goap.AddGoal(new KillEntity(component.Goap, tree));
                        }
                    }
                }
                else if (mouseState.RightButton == ButtonState.Pressed)
                {
                    if (ChopDesignations.Contains(tree))
                    {
                        ChopDesignations.Remove(tree);
                        tree.DrawBoundingBox = false;

                        foreach (CreatureAIComponent component in Minions)
                        {
                            component.Goap.Goals.Remove("Kill Entity: " + tree.Name + " " + tree.GlobalID);
                        }
                    }

                }
            }

        }

        public void EnterGodMode(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            VoxSelector.Enabled = true;
            game.IsMouseVisible = true;
        }

        public void OnSelected(List<VoxelRef> refs, InputManager.MouseButton button)
        {
            if (refs == null)
            {
                return;
            }

            if(GodMode == null || !GodMode.IsActive)
            {
                #region nongodmode
                switch (CurrentTool)
                {
                    case ToolMode.Dig:

                        if (button == InputManager.MouseButton.Left)
                        {
                            foreach (VoxelRef r in refs)
                            {
                                if (r == null) continue;

                                Voxel v = r.GetVoxel(Chunks, false);
                                if (v != null && !IsDigDesignation(v))
                                {
                                    Designation d = new Designation();
                                    d.vox = v;
                                    DigDesignations.Add(d);

                                    foreach (CreatureAIComponent minion in Minions)
                                    {
                                        //minion.Goap.AddGoal(new KillVoxel(minion.Goap, d.vox.GetReference()));
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (VoxelRef r in refs)
                            {
                                if (r == null)
                                {
                                    continue;
                                }
                                Voxel v = r.GetVoxel(Chunks, false);
                                if (v != null)
                                {
                                    if (IsDigDesignation(v))
                                    {
                                        DigDesignations.Remove(GetDigDesignation(v));
                                    }

                                    foreach (CreatureAIComponent minion in Minions)
                                    {
                                        if (minion != null && minion.Goap != null)
                                        {
                                            minion.Goap.Goals.Remove("Kill Voxel: " + v.Position);
                                        }
                                    }
                                }

                            }
                        }
                        break;

                    case ToolMode.Guard:
                        foreach (VoxelRef r in refs)
                        {
                            if (r == null)
                            {
                                continue;
                            }

                            Voxel v = r.GetVoxel(Chunks, false);

                            if (button == InputManager.MouseButton.Left)
                            {
                                if (v != null && !IsGuardDesignation(v))
                                {
                                    Designation d = new Designation();
                                    d.vox = v;
                                    GuardDesignations.Add(d);

                                }
                            }
                            else
                            {
                                if (v!= null && IsGuardDesignation(v))
                                {
                                    GuardDesignations.Remove(GetGuardDesignation(v));

                                    foreach (CreatureAIComponent minion in Minions)
                                    {
                                        minion.Goap.Goals.Remove("Guard Voxel " + v.Position);
                                    }
                                }
                            }
                        }

                        break;

                    case ToolMode.CreateStockpiles:
                        Stockpile existingPile = null;
                        foreach (VoxelRef r in refs)
                        {
                            if (r == null)
                            {
                                continue;
                            }

                            Voxel v = r.GetVoxel(Chunks, false);

                            if (v == null || v.RampType != RampType.None)
                            {
                                continue;
                            }

                            if (button == InputManager.MouseButton.Left)
                            {
                                if (v != null && !IsInStockpile(v))
                                {
                                    Stockpile thisPile = GetIntersectingStockpile(v);

                                    if (existingPile == null)
                                    {
                                        existingPile = thisPile;
                                    }

                                    if (existingPile != null)
                                    {
                                        existingPile.AddComponentVoxel(v, Graphics);
                                    }
                                    else
                                    {
                                        Stockpile newPile = new Stockpile("Stockpile " + Stockpile.NextID());
                                        newPile.AllowedResources.Add(ResourceLibrary.Resources["Wood"]);
                                        newPile.AllowedResources.Add(ResourceLibrary.Resources["Dirt"]);
                                        newPile.AllowedResources.Add(ResourceLibrary.Resources["Stone"]);
                                        newPile.AllowedResources.Add(ResourceLibrary.Resources["Apple"]);
                                        newPile.AllowedResources.Add(ResourceLibrary.Resources["Gold"]);
                                        newPile.AllowedResources.Add(ResourceLibrary.Resources["Mana"]);
                                        newPile.AllowedResources.Add(ResourceLibrary.Resources["Iron"]);
                                        newPile.AllowedResources.Add(ResourceLibrary.Resources["Container"]);

                                        newPile.AddComponentVoxel(v, Graphics);

                                        Stockpiles.Add(newPile);
                                        existingPile = newPile;

                                    }
                                }
                            }
                            else
                            {
                                if (v != null && IsInStockpile(v))
                                {
                                    existingPile = GetIntersectingStockpile(v);

                                    if (existingPile != null)
                                    {
                                        Stockpiles.Remove(existingPile);
                                        foreach (Item i in existingPile.Items)
                                        {
                                            i.userData.IsStocked = false;
                                        }
                                        existingPile.ResetVoxelTextures();
                                    }
                                }
                            }
                        }
                        break;

                    case ToolMode.Build:

                        break;
                }

                #endregion
            }
        }


        #endregion

        public LocatableComponent GetRandomGatherDesignationWithTag(string tag)
        {
            List<LocatableComponent> des = new List<LocatableComponent>();

            foreach (LocatableComponent c in GatherDesignations)
            {
                if (c.Tags.Contains(tag))
                {
                    des.Add(c);
                }
            }

            if (des.Count == 0)
            {
                return null;
            }
            else
            {
                return des[PlayState.random.Next(0, des.Count)];
            }
        }

        #region input

        public void UpdateMouse(MouseState mouseState, KeyboardState keyState, DwarfGame game, GameTime time)
        {
            if (keyState.IsKeyDown(ControlSettings.Default.CameraMode))
            {
                game.IsMouseVisible = false;
            }
            else
            {

                VoxSelector.Update();
                
                if (GodMode.IsActive)
                {
                    EnterGodMode(mouseState, keyState, game, time);
                }
                else
                {
                    switch (CurrentTool)
                    {
                        case ToolMode.Dig:
                            DigMode(mouseState, keyState, game, time);
                            break;


                        case ToolMode.Build:
                            VoxSelector.Enabled = true;
                            game.IsMouseVisible = true;
                            RoomDesignator.Update(mouseState, keyState, game, time);
                            break;

                        case ToolMode.SelectUnits:
                            break;

                        case ToolMode.Chop:
                            ChopMode(mouseState, keyState, game, time);
                            break;

                        case ToolMode.Guard:
                            GuardMode(mouseState, keyState, game, time);
                            break;

                        case ToolMode.CreateStockpiles:
                            StockpileMode(mouseState, keyState, game, time);
                            break;

                        case ToolMode.Gather:
                            GatherMode(mouseState, keyState, game, time);
                            break;

                    }
                }



            }
        }

        public void UpdateInput(DwarfGame game, GameTime time)
        {
            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();
          

            if (!IsMouseOverGui())
            {
                UpdateMouse(Mouse.GetState(), Keyboard.GetState(), game, time);
            }

        }

        public void OnKeyPressed()
        {


        }

        public void OnKeyReleased(Keys key)
        {
            if (key == ControlSettings.Default.SliceUp)
            {
                Chunks.SetMaxViewingLevel(Chunks.MaxViewingLevel + 1, ChunkManager.SliceMode.Y);
            }

            if (key == ControlSettings.Default.SliceDown)
            {
                Chunks.SetMaxViewingLevel(Chunks.MaxViewingLevel - 1, ChunkManager.SliceMode.Y);
            }


            if (key == ControlSettings.Default.GodMode)
            {
                GodMode.IsActive = !GodMode.IsActive;
            }
            
        }

        public bool IsMouseOverGui()
        {
            return GUI.IsMouseOver();
        }

        #endregion

        public void AddShipDesignation(ResourceAmount resource, Room port)
        {
            
            List<LocatableComponent> componentsToShip = new List<LocatableComponent>();

            foreach (Stockpile s in Stockpiles)
            {
                for (int i = componentsToShip.Count; i < resource.NumResources; i++)
                {
                    LocatableComponent r = s.GetNextResourceWithTagIgnore(resource.ResourceType.ResourceName, componentsToShip);

                    if (r != null)
                    {
                        componentsToShip.Add(r);
                    }
                }
            }

            ShipDesignations.Add(new ShipDesignation(resource, port));

            foreach (LocatableComponent loc in componentsToShip)
            {
                foreach (CreatureAIComponent minion in Minions)
                {
                    //PutItemInZone put = new PutItemInZone(minion.Goap, Item.FindItem(loc), port);
                    //minion.Goap.AddGoal(put);
                }
            }
        }

        public void AddGatherDesignation(LocatableComponent resource)
        {

            if(resource.IsStocked || resource.Parent != Components.RootComponent || resource.IsDead)
            {
                return;
            }

            if (!GatherDesignations.Contains(resource))
            {
                GatherDesignations.Add(resource);

            }

            foreach (CreatureAIComponent minion in Minions)
            {
                //minion.Goap.AddGoal(new GatherItem(minion.Goap, resource));
            }
        }
    }
}
