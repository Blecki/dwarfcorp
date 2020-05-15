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
    public class EntityInspectorTool : PlayerTool
    {
        public static GameComponent SelectedEntity = null;
        public static EntityInspectionPanel InspectorGui = null;

        [ToolFactory("EntityInspector")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new EntityInspectorTool(World);
        }
        
        private List<GameComponent> underMouse = null;

        public EntityInspectorTool(WorldManager World)
        {
            this.World = World;
        }

        public override void Destroy()
        {
        }

        public override void OnBegin(Object Arguments)
        {
            SelectedEntity = null;
        }

        public override void OnEnd()
        {
            SelectedEntity = null;

            // Clean up the GUI if it exists.
            if (InspectorGui != null)
                InspectorGui.Close();
            InspectorGui = null;
        }

        public EntityInspectorTool()
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
            if (bodies.Count > 0)
                SelectedEntity = bodies[0].GetRoot();
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
            var sb = new StringBuilder();
            var bodyList = bodies.ToList();
            var first = true;

            for (int i = 0; i < bodyList.Count; i++)
            {
                if (!first)
                    sb.AppendLine();
                first = false;

                if (bodyList[i].GetComponent<Creature>().HasValue(out var dwarf))
                {
                    sb.Append(dwarf.Stats.FullName + " (" + (dwarf.Stats.Title ?? dwarf.Stats.CurrentClass.Name) + ")");

                    if (dwarf.Stats.IsAsleep)
                        sb.Append(" UNCONSCIOUS ");

                    if (dwarf.Stats.IsOnStrike)
                        sb.Append(" ON STRIKE");
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


            // If no gui bit exists, go ahead and create it. If it does, update the selected entity part of it.
            if (InspectorGui == null)
            {
                InspectorGui = World.UserInterface.Gui.ConstructWidget(new EntityInspectionPanel()) as EntityInspectionPanel;
                InspectorGui.Rect = new Rectangle(0, 0, 256, 512);
                InspectorGui.Layout();
                World.UserInterface.Gui.RootItem.AddChild(InspectorGui);
                InspectorGui.OnClose = (sender) =>
                {
                    InspectorGui = null;
                    World.UserInterface.ChangeTool("SelectUnits");
                };
            }
            InspectorGui.SelectedEntity = SelectedEntity;
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
            var entity = World.UserInterface.BodySelector.CurrentBodies.FirstOrDefault();
            if (entity != null)
                Drawer2D.DrawRect(DwarfGame.SpriteBatch, GetScreenRect(entity.BoundingBox, World.Renderer.Camera), Color.White, 1.0f);
            
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
