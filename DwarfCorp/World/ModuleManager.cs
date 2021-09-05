using DwarfCorp.GameStates;
using DwarfCorp.Gui.Widgets;
using DwarfCorp.Tutorial;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace DwarfCorp
{
    public class ModuleManager
    {
        public enum UpdateTypes
        {
            None = 0,
            ComponentCreated = 1,
            ComponentDestroyed = 2,
            Update = 4,
            Render = 8,
            Shutdown = 16,
            VoxelChange = 32
        }

        [JsonIgnore] private List<EngineModule> UpdateSystems = new List<EngineModule>();
        [JsonIgnore] private Dictionary<UpdateTypes, List<EngineModule>> ModuleByUpdateType = new Dictionary<UpdateTypes, List<EngineModule>>();

        public MaybeNull<T> GetModule<T>() where T: EngineModule
        {
            return UpdateSystems.FirstOrDefault(s => s is T) as T;
        }

        private void InitializeModuleUpdateTypeList(UpdateTypes UpdateType)
        {
            ModuleByUpdateType[UpdateType] = UpdateSystems.Where(m => (m.UpdatesWanted & UpdateType) == UpdateType).ToList();
        }

        public ModuleManager(WorldManager World)
        {
            foreach (var updateSystemFactory in AssetManager.EnumerateModHooks(typeof(UpdateSystemFactoryAttribute), typeof(EngineModule), new Type[] { typeof(WorldManager) }))
                UpdateSystems.Add(updateSystemFactory.Invoke(null, new Object[] { World }) as EngineModule);

            InitializeModuleUpdateTypeList(UpdateTypes.ComponentCreated);
            InitializeModuleUpdateTypeList(UpdateTypes.ComponentDestroyed);
            InitializeModuleUpdateTypeList(UpdateTypes.Update);
            InitializeModuleUpdateTypeList(UpdateTypes.Render);
            InitializeModuleUpdateTypeList(UpdateTypes.Shutdown);
            InitializeModuleUpdateTypeList(UpdateTypes.VoxelChange);
        }

        private List<EngineModule> GetModulesThatWantUpdates(UpdateTypes UpdateType)
        {
            return ModuleByUpdateType[UpdateType];
        }

        public void DebugOutput(DwarfConsole ConsoleTile)
        {
            ConsoleTile.Lines.Clear();

            ConsoleTile.Lines.Add("Modules");
            foreach (var module in UpdateSystems)
                ConsoleTile.Lines.Add(module.GetType().Name);
            ConsoleTile.Invalidate();
        }

        // Todo: Protect against exceptions
        public void Update(DwarfTime GameTime, WorldManager World)
        {
            foreach (var module in GetModulesThatWantUpdates(UpdateTypes.Update))
                module.Update(GameTime, World);
        }

        // Todo: send a list instead
        public void ComponentCreated(GameComponent C)
        {
            foreach (var module in GetModulesThatWantUpdates(UpdateTypes.ComponentCreated))
                module.ComponentCreated(C);
        }

        public virtual void ComponentDestroyed(GameComponent C)
        {
            foreach (var module in GetModulesThatWantUpdates(UpdateTypes.ComponentDestroyed))
                module.ComponentDestroyed(C);
        }

        public virtual void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            foreach (var module in GetModulesThatWantUpdates(UpdateTypes.Render))
                module.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect);
        }

        public virtual void Shutdown()
        {
            foreach (var module in GetModulesThatWantUpdates(UpdateTypes.Shutdown))
                module.Shutdown();
        }

        public virtual void VoxelChange(List<VoxelEvent> Events, WorldManager World)
        {
            foreach (var module in GetModulesThatWantUpdates(UpdateTypes.VoxelChange))
                module.VoxelEvent(Events, World);
        }
    }
}
