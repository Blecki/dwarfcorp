// GameComponent.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// This class is responsible for handling components. "Components" are one of the most important parts of the 
    /// DwarfCorp engine. Everything in the game is a collection of components. A collection of components is called an "entity".
    /// Components live in a tree-like structure, they have parents and children. Most components (called Locatable components)
    /// also have a position and orientation.
    /// 
    /// By adding and removing components to an entity, functionality can be changed.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GameComponent
    {
        /// <summary>
        /// Gets or sets the name (used mostly for debugging purposes).
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the global identifier (all GameComponents have a global ID).
        /// </summary>
        /// <value>
        /// The global identifier.
        /// </value>
        public uint GlobalID { get; set; }

        /// <summary>
        /// Gets or sets the parent. If null, this is the root component.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public GameComponent Parent { get; set; }

        /// <summary>
        /// List of GameComponent children attached to this one.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        public List<GameComponent> Children { get; set; }

        private static uint maxGlobalID = 0;
        private uint maxLocalID = 0;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is visible.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is visible; otherwise, <c>false</c>.
        /// </value>
        public bool IsVisible { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// If not active, the instance will not call Update
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is dead. If dead,
        /// it will be removed from the game in the next tick.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is dead; otherwise, <c>false</c>.
        /// </value>
        public bool IsDead { get; private set; }

        /// <summary>
        /// Gets or sets the tags. Tags are just arbitrary strings attached to objects.
        /// </summary>
        /// <value>
        /// The tags.
        /// </value>
        public List<string> Tags { get; set; }

        
        /// <summary>
        /// Gets or sets the world.
        /// </summary>
        /// <value>
        /// The world.
        /// </value>
        [JsonIgnore]
        public WorldManager World { get; set; }

        /// <summary>
        /// Gets the component manager (from world)
        /// </summary>
        /// <value>
        /// The manager.
        /// </value
        [JsonIgnore]
        public ComponentManager Manager { get { return World.ComponentManager; } }

        /// <summary>
        /// A list of the GameComponentCache type handlers this component subscribes to.
        /// </summary>
        [JsonIgnore]
        public List<GameObjectCaching.GameComponentCache> cacheTypes;

        public List<string> cacheTypeNames;

        public bool HasOwnRender;

        /// <summary>
        /// The global identifier lock. This is necessary to ensure that no two components
        /// have the same ID (they might be getting created in threads).
        /// </summary>
        private static Object globalIdLock = new object();

        public virtual void ReceiveMessageRecursive(Message messageToReceive)
        {
            foreach(GameComponent child in Children)
            {
                child.ReceiveMessageRecursive(messageToReceive);
            }
        }

        /// <summary>
        /// Resets the maximum global identifier. All new components will have an ID greate than this.
        /// </summary>
        /// <param name="value">The value.</param>
        public static void ResetMaxGlobalId(uint value)
        {
            maxGlobalID=value;
        }

        [OnDeserialized]
        void OnDeserializing(StreamingContext context)
        {
            // Assume the context passed in is a WorldManager
            World = ((WorldManager) context.Context);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class.
        /// </summary>
        public GameComponent()
        {
            World = null;
            Children = new List<GameComponent>();
            Name = "uninitialized";
            IsVisible = true;
            IsActive = true;
            Tags = new List<string>();
            HasOwnRender = GameObjectCaching.HasOwnRender(this.GetType());
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class, while adding it to the component manager.
        /// </summary>
        /// <param name="createNew">if set to <c>true</c> adds this component to the manager..</param>
        /// <param name="manager">The component manager to add the component to</param>
        public GameComponent(bool createNew, ComponentManager manager)
        {
            World = manager.World;
            lock (globalIdLock)
            {
                GlobalID = maxGlobalID;
                maxGlobalID++;
            }

            Parent = null;
            Children = new List<GameComponent>();

            Name = "uninitialized";
            IsVisible = true;
            IsActive = true;
            IsDead = false;

            if (createNew && Manager != null)
                Manager.AddComponent(this);

            Tags = new List<string>();
            HasOwnRender = GameObjectCaching.HasOwnRender(this.GetType());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class.
        /// </summary>
        /// <param name="name">The name of the component.</param>
        /// <param name="parent">The parent component.</param>
        /// <param name="manager">The component manager.</param>
        public GameComponent(string name, ComponentManager manager) :
            this(true, manager)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the next local identifier. These are relative to the siblings of the component.
        /// Increments the local identifier.
        /// </summary>
        /// <returns>The next local identifier to use.</returns>
        public uint GetNextLocalID()
        {
            return maxLocalID++;
        }

        /// <summary>
        /// Gets the first child component of the specified type from among the children.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="self">if set to <c>true</c> add this component to the list if it qualifies.</param>
        /// <returns>The first component of type T.</returns>
        public T GetComponent<T>(bool self=true) where T : GameComponent
        {
            return GetEntityRootComponent().GetChildrenOfType<T>(self).FirstOrDefault();
        }

        /// <summary>
        /// Gets all children and descendents of this component.
        /// </summary>
        /// <returns>A list of all the descendents of this component.</returns>
        public List<GameComponent> GetAllChildrenRecursive()
        {
            List<GameComponent> toReturn = new List<GameComponent>();

            toReturn.AddRange(Children);

            foreach(GameComponent child in Children)
            {
                toReturn.AddRange(child.GetAllChildrenRecursive());
            }

            return toReturn;
        }

        /// <summary>
        /// Renders the component to the selection buffer (for selecting stuff on screen).
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="chunks">The chunks.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="effect">The shader to use.</param>
        public virtual void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera,
            SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            
        }

        /// <summary>
        /// Converts the global identifier into a color for rendering to 
        /// the selection buffer.
        /// </summary>
        /// <returns></returns>
        public Color GetGlobalIDColor()
        {
            // 0xFFFFFFFF
            // 0xRRGGBBAA
            int r = (int)(GlobalID >> 6);
            int g = (int)((GlobalID >> 4) & 0x000000FF);
            int b = (int)((GlobalID >> 2) & 0x000000FF);
            int a = (int)((GlobalID) & 0x000000FF);
            //return new Color {PackedValue = GlobalID};
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Converts a packed color representation of a global ID to a global ID.
        /// </summary>
        /// <param name="color">The color (packed representaiton)</param>
        /// <returns></returns>
        public static uint GlobalIDFromColor(Color color)
        {
            // 0xFFFFFFFF
            // 0xRRGGBBAA
            //return color.PackedValue;
            uint id = 0;
            id = id | (uint) (color.R << 6);
            id = id | (uint) (color.G << 4);
            id = id | (uint) (color.B << 2);
            id = id | (uint) (color.A);
            return id;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return (int) GlobalID;
        }

        /// <summary>
        /// Sets this component and all of its descendents to be active.
        /// </summary>
        /// <param name="active">if set to <c>true</c> IsActive is also true..</param>
        public void SetActiveRecursive(bool active)
        {
            IsActive = active;

            foreach(GameComponent child in Children)
            {
                child.SetActiveRecursive(active);
            }
        }

        /// <summary>
        /// Sets this component and all of its descendents to be visible.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> IsVisible will be true.</param>
        public void SetVisibleRecursive(bool visible)
        {
            IsVisible = visible;

            foreach(GameComponent child in Children)
            {
                child.SetVisibleRecursive(visible);
            }
        }

        // Todo: Can these two functions be unified?
        /// <summary> This is intended to be used like Die(), but is garunteed only
        /// to clean up memory, without doing things like animations.</summary>
        public virtual void Delete()
        {
            if (IsDead)
                return;

            IsDead = true;

            foreach (GameComponent child in GetAllChildrenRecursive())
                child.Delete();

            if (Parent != null) Parent.RemoveChild(this);

            IsActive = false;
            IsVisible = false;
            Manager.RemoveComponent(this);
        }

        /// <summary>
        /// Deletes this instance and calls Die() on all descendents. Removes this instance from the parent.
        /// </summary>
        public virtual void Die()
        {
            if(IsDead)
                return;

            IsDead = true;

            foreach (GameComponent child in GetAllChildrenRecursive())
                child.Die();

            if (Parent != null) Parent.RemoveChild(this);

            IsActive = false;
            IsVisible = false;
            Manager.RemoveComponent(this);
        }

        /// <summary>
        /// Gets a description of this component.
        /// </summary>
        /// <returns>Descriptive text about the component</returns>
        public virtual string GetDescription()
        {
            string toReturn = "";

            if(Parent == Manager.RootComponent)
                toReturn += Name;

            foreach (GameComponent component in Children)
            {

                string componentDesc = component.GetDescription();

                if (!String.IsNullOrEmpty(componentDesc))
                {
                    toReturn += "\n ";
                    toReturn += componentDesc;
                }
            }

            return toReturn;
        }

        #region child_operators

        /// <summary>
        /// Determines whether any of the children of this component are of type T.
        /// </summary>
        /// <typeparam name="T">The type to check</typeparam>
        /// <returns>
        ///   <c>true</c> if [has child with type]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasChildWithType<T>() where T : GameComponent
        {
            return Children.OfType<T>().Any();
        }

        /// <summary>
        /// Gets the children of this component which are of type T.
        /// </summary>
        /// <typeparam name="T">The type to get</typeparam>
        /// <param name="includeSelf">if set to <c>true</c> include this component in the list.</param>
        /// <returns>A list of components of type T</returns>
        public List<T> GetChildrenOfType<T>(bool includeSelf = false) where T : GameComponent
        {
            List<T> toReturn = new List<T>();
            if (includeSelf && this is T)
            {
                toReturn.Add((T)this);
            }

            toReturn.AddRange(from child in Children
                where child is T
                select (T) child);

            return toReturn;
        }

        /// <summary>
        /// Determines whether this component has a child with the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if [has child with name] [the specified name]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasChildWithName(string name)
        {
            return Children.Any(child => child.Name == name);
        }

        /// <summary>
        /// Gets a list of children with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The list of children with the specified name.</returns>
        public List<GameComponent> GetChildrenWithName(string name)
        {
            return Children.Where(child => child.Name == name).ToList();
        }

        /// <summary>
        /// Gets the child with the given global identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The child with the given identifier, or null if it does not exist.</returns>
        public GameComponent GetChildWithGlobalID(uint id)
        {
            return Children.FirstOrDefault(child => child.GlobalID == id);
        }

        /// <summary>
        /// Adds the child if it has not been added already.
        /// </summary>
        /// <param name="child">The child.</param>
        public GameComponent AddChild(GameComponent child)
        {
            lock (Children)
            {
                if (GetChildWithGlobalID(child.GlobalID) != null)
                    return child;

                Children.Add(child);
                child.Parent = this;
                child.AddToCache(child);
            }

            return child;
        }

        /// <summary>
        /// Removes the child if it exists.
        /// </summary>
        /// <param name="child">The child.</param>
        public void RemoveChild(GameComponent child)
        {
            lock (Children)
            {
                if (GetChildWithGlobalID(child.GlobalID) == null)
                    return;

                Children.Remove(child);
                child.RemoveFromCache(child);
            }
        }

        #endregion

        #region recursive_child_operators

        /// <summary>
        /// Determines whether this component has any descendant with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if [has child with name recursive] [the specified name]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasChildWithNameRecursive(string name)
        {
            return Children.Any(child => child.Name == name || child.HasChildWithNameRecursive(name));
        }

        /// <summary>
        /// Gets the anscestor of this component which has no parent.
        /// </summary>
        /// <returns>The anscestor of this component with no parent.</returns>
        public GameComponent GetEntityRootComponent()
        {
            var p = this;

            while(p.Parent != null && !Object.ReferenceEquals(p.Parent, Manager.RootComponent))
                p = p.Parent;

            return p;
        }

        /// <summary>
        /// Gets a list of children and descendents with the given name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A list containing descendants of this instance with the given name.</returns>
        public List<GameComponent> GetChildrenWithNameRecursive(string name)
        {
            List<GameComponent> toReturn = new List<GameComponent>();
            foreach(GameComponent child in Children)
            {
                if(child.Name == name)
                {
                    toReturn.Add(child);
                }

                List<GameComponent> childList = child.GetChildrenWithNameRecursive(name);
                toReturn.AddRange(childList);
            }

            return toReturn;
        }

        /// <summary>
        /// Determines whether any descendant of this gamecomponent has the given global identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        ///   <c>true</c> if [has child with global identifier recursive] [the specified identifier]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasChildWithGlobalIDRecursive(uint id)
        {
            return Children.Any(child => child.GlobalID == id || child.HasChildWithGlobalIDRecursive(id));
        }

        /// <summary>
        /// Gets a child having the given global id from among the descendants of this component.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>A component having the given identifier if it exists. Null otherwise.</returns>
        public GameComponent GetChildWithGlobalIDRecursive(uint id)
        {
            foreach(GameComponent child in Children)
            {
                if(child.GlobalID == id)
                {
                    return child;
                }

                GameComponent grandChild = child.GetChildWithGlobalIDRecursive(id);

                if(grandChild != null)
                {
                    return grandChild;
                }
            }

            return null;
        }


        /// <summary>
        /// Determines whether this component has any children or descendants of the given type.
        /// </summary>
        /// <typeparam name="T">The type to check</typeparam>
        /// <returns>
        ///   <c>true</c> if [has child with type recursive]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasChildWithTypeRecursive<T>() where T : GameComponent
        {
            return Children.Any(child => child is T || child.HasChildWithTypeRecursive<T>());
        }

        /// <summary>
        /// Gets a list of children and descendants with the given type.
        /// </summary>
        /// <typeparam name="T">The type to check</typeparam>
        /// <returns>A list of descendants with the given type.</returns>
        public List<T> GetChildrenOfTypeRecursive<T>() where T : GameComponent
        {
            List<T> toReturn = new List<T>();
            foreach (GameComponent child in Children)
            {
                if(child is T)
                {
                    toReturn.Add((T) child);
                }

                toReturn.AddRange(child.GetChildrenOfType<T>());
            }
            return toReturn;
        }

        #endregion

        #region cacheType functions
        public void AddCacheType(string cacheName)
        {
            GameObjectCaching.GameComponentCache cache = GameObjectCaching.GetCacheByName(cacheName);
            if (cache == null) return;

            if (cacheTypeNames == null)
            {
                cacheTypeNames = new List<string>();
                cacheTypes = new List<GameObjectCaching.GameComponentCache>();
            } else if (cacheTypeNames.Contains(cacheName)) return;

            cacheTypeNames.Add(cacheName);
            cacheTypes.Add(cache);

            // Sadly, when we call this in the type constructor of a derived type we've already called AddChild for that type
            // in the GameComponent constructor so we can't put AddToCache there.
            cache.addFunction(this);
        }
        
        public void AddToCache(GameComponent cachedObject)
        {
            if (cacheTypes == null) return;
            for (int i = 0; i < cacheTypes.Count; i++)
            {
                cacheTypes[i].addFunction(cachedObject);
            }
        }

        public void RemoveFromCache(GameComponent cachedObject)
        {
            if (cacheTypes == null) return;
            for (int i = 0; i < cacheTypes.Count; i++)
            {
                cacheTypes[i].removeFunction(cachedObject);
            }
        }

        public void RefreshCacheTypes()
        {
            if (cacheTypeNames == null) return;
            cacheTypes = new List<GameObjectCaching.GameComponentCache>(cacheTypeNames.Count);
            for (int i = 0; i < cacheTypeNames.Count; i++)
            {
                GameObjectCaching.GameComponentCache cache = GameObjectCaching.GetCacheByName(cacheTypeNames[i]);
                if (cache == null) continue;
                cacheTypes.Add(cache);
                cache.addFunction(this);
            }
        }

        public void RefreshCacheTypesRecursive()
        {
            Stack<IEnumerator<GameComponent>> eStack = new Stack<IEnumerator<GameComponent>>();

            eStack.Push(Children.GetEnumerator());

            while(eStack.Count > 0)
            {
                IEnumerator<GameComponent> e = eStack.Pop();
                while (e.MoveNext())
                {
                    GameComponent current = e.Current;
                    current.RefreshCacheTypes();

                    if (current.Children.Count > 0)
                    {
                        eStack.Push(e);
                        e = current.Children.GetEnumerator();
                    }
                }
            }
        }
        #endregion
    }

}
