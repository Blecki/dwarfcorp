using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DwarfSelectorTool : PlayerTool
    {
        [ToolFactory("SelectUnits")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new DwarfSelectorTool(World);
        }
        
        public Func<GameComponent, bool> DrawSelectionRect = (b) => true;
        private List<GameComponent> underMouse = null;

        public DwarfSelectorTool(WorldManager World)
        {
            this.World = World;
            InputManager.MouseClickedCallback += InputManager_MouseClickedCallback;
        }

        public override void Destroy()
        {
            InputManager.MouseClickedCallback -= InputManager_MouseClickedCallback;
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }

        void InputManager_MouseClickedCallback(InputManager.MouseButton button)
        {
            if(button != InputManager.MouseButton.Right || World.UserInterface.CurrentTool != this || KeyManager.RotationEnabled(World.Renderer.Camera))
                return;

            var mouseState = KeyManager.TrueMousePos;

            var vox = VoxelHelpers.FindFirstVisibleVoxelOnScreenRay(
                World.ChunkManager,
                mouseState.X, mouseState.Y,
                World.Renderer.Camera, 
                GameState.Game.GraphicsDevice.Viewport,
                150.0f, 
                false,
                voxel => voxel.IsValid && (!voxel.IsEmpty || voxel.LiquidLevel > 0));

            if (!vox.IsValid)
                return;

            foreach(CreatureAI minion in World.PersistentData.SelectedMinions)
            {
                if (minion.Creature.Stats.IsAsleep) continue;
                if(minion.CurrentTask != null)
                    minion.AssignTask(minion.CurrentTask);

                var above = VoxelHelpers.GetVoxelAbove(vox);
                minion.Blackboard.SetData("MoveTarget", above);

                minion.ChangeTask(new GoToNamedVoxelAct("MoveTarget", PlanAct.PlanType.Adjacent, minion).AsTask());
                minion.CurrentTask.AutoRetry = false;
                minion.CurrentTask.Priority = TaskPriority.Urgent;
            }
            OnConfirm(World.PersistentData.SelectedMinions);

            if (World.PersistentData.SelectedMinions.Count > 0)
                IndicatorManager.DrawIndicator(IndicatorManager.StandardIndicators.DownArrow, vox.WorldPosition + Vector3.One * 0.5f, 0.5f, 2.0f, new Vector2(0, -50), Color.LightGreen);
        }

        public DwarfSelectorTool()
        {
            
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
          
        }

        bool IsNotSelectedDwarf(GameComponent body)
        {
            if (body == null)
                return true;

            var dwarves = body.EnumerateAll().OfType<Creature>().ToList();

            if (dwarves.Count <= 0)
                return false;

            Creature dwarf = dwarves[0];
            return dwarf.Faction == World.PlayerFaction && !World.PersistentData.SelectedMinions.Contains(dwarf.AI);
        }

        bool IsDwarf(GameComponent body)
        {
            if (body == null)
                return false;

            var dwarves = body.EnumerateAll().OfType<Creature>().ToList();

            if (dwarves.Count <= 0)
                return false;

            return dwarves[0].Faction == World.PlayerFaction;
        }

        protected void SelectDwarves(List<GameComponent> bodies)
        {
            KeyboardState keyState = Keyboard.GetState();

            if(!keyState.IsKeyDown(Keys.LeftShift))
                World.PersistentData.SelectedMinions.Clear();

            var newDwarves = new List<CreatureAI>();

            foreach (GameComponent body in bodies)
            {
                if (IsNotSelectedDwarf(body))
                {
                    World.PersistentData.SelectedMinions.Add(body.GetRoot().GetComponent<CreatureAI>());
                    newDwarves.Add(body.GetRoot().GetComponent<CreatureAI>());

                    World.Tutorial("dwarf selected");
                }
            }

            OnConfirm(newDwarves);
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            switch(button)
            {
                case InputManager.MouseButton.Left:
                    SelectDwarves(bodies);
                    break;
            }
        }

        public static string GetMouseOverText(IEnumerable<GameComponent> bodies)
        {
            StringBuilder sb = new StringBuilder();

            List<GameComponent> bodyList = bodies.ToList();
            for (int i = 0; i < bodyList.Count; i++)
            {
                Creature dwarf = bodyList[i].GetComponent<Creature>();
                if (dwarf != null)
                {
                    sb.Append(dwarf.Stats.FullName + " (" + (dwarf.Stats.Title ?? dwarf.Stats.CurrentClass.Name) + ")");
                    if (dwarf.Stats.IsAsleep)
                        sb.Append(" UNCONSCIOUS ");

                    if (dwarf.Stats.IsOnStrike)
                        sb.Append(" ON STRIKE");

                    if (i < bodyList.Count - 1)
                        sb.Append("\n");
                }
                else
                    sb.Append(bodyList[i].GetDescription());
            }

            return sb.ToString();
        }

        public override void DefaultOnMouseOver(IEnumerable<GameComponent> bodies)
        {
            World.UserInterface.ShowTooltip(GetMouseOverText(bodies));
            underMouse = bodies.ToList();
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            DefaultOnMouseOver(bodies);
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            World.UserInterface.VoxSelector.Enabled = false;
            World.UserInterface.BodySelector.Enabled = true;
            World.UserInterface.BodySelector.AllowRightClickSelection = false;

            World.UserInterface.SetMouse(World.UserInterface.MousePointer);
        }

        public Rectangle GetScreenRect(BoundingBox Box, Camera Camera)
        {
            Vector3 ext = (Box.Max - Box.Min);
            Vector3 center = Box.Center();

            Vector3 p1 = Camera.Project(Box.Min);
            Vector3 p2 = Camera.Project(Box.Max);
            Vector3 p3 = Camera.Project(Box.Min + new Vector3(ext.X, 0, 0));
            Vector3 p4 = Camera.Project(Box.Min + new Vector3(0, ext.Y, 0));
            Vector3 p5 = Camera.Project(Box.Min + new Vector3(0, 0, ext.Z));
            Vector3 p6 = Camera.Project(Box.Min + new Vector3(ext.X, ext.Y, 0));

            Vector3 min = MathFunctions.Min(p1, p2, p3, p4, p5, p6);
            Vector3 max = MathFunctions.Max(p1, p2, p3, p4, p5, p6);

            return new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
            DwarfGame.SpriteBatch.Begin();

            foreach (var body in World.UserInterface.BodySelector.CurrentBodies.Where(DrawSelectionRect))
            {
                Drawer2D.DrawRect(DwarfGame.SpriteBatch, GetScreenRect(body.BoundingBox, World.Renderer.Camera), Color.White, 1.0f);
            }
            if (underMouse != null)
                foreach (var body in underMouse.Where(DrawSelectionRect))
                    Drawer2D.DrawRect(DwarfGame.SpriteBatch, GetScreenRect(body.BoundingBox, World.Renderer.Camera), Color.White, 1.0f);

            DwarfGame.SpriteBatch.End();
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }


        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }
    }
}
