using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DesignationDrawer
    {
        public DesignationDrawer()
        {
        }

        public void DrawHilites(
            WorldManager World,
            DesignationSet Set,
            Action<Vector3, Vector3, Color, float, bool> DrawBoxCallback,
            Action<Vector3, VoxelType> DrawPhantomCallback)
        {
            // Todo: Can this be drawn by the entity, allowing it to be properly frustrum culled?
            // - Need to add a 'gestating' entity state to the alive/dead/active mess.

            foreach (var entity in Set.EnumerateEntityDesignations())
            {
                if ((entity.Type & World.Renderer.PersistentSettings.VisibleTypes) == entity.Type)
                {
                    var props = Library.GetDesignationTypeProperties(entity.Type);

                    // Todo: More consistent drawing?
                    if (entity.Type == DesignationType.Craft)
                    {
                        entity.Body.SetFlagRecursive(GameComponent.Flag.Visible, true);
                        if (!entity.Body.Active)
                            entity.Body.SetVertexColorRecursive(props.Color);
                    }
                    else
                    {
                        var box = entity.Body.GetBoundingBox();
                        DrawBoxCallback(box.Min, box.Max - box.Min, props.Color, props.LineWidth, false);
                        entity.Body.SetVertexColorRecursive(props.Color);
                    }
                }
                else if (entity.Type == DesignationType.Craft) // Make the ghost object invisible if these designations are turned off.
                    entity.Body.SetFlagRecursive(GameComponent.Flag.Visible, false);
            }
        }
    }
}
