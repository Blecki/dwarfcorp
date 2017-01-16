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
        public bool IsDead { get; set; }

        /// <summary>
        /// Gets or sets the tags. Tags are just arbitrary strings attached to objects.
        /// </summary>
        /// <value>
        /// The tags.
        /// </value>
        public List<string> Tags { get; set; }


        /// <summary>
        /// Gets the component manager (statically stored in PlayState).
        /// </summary>
        /// <value>
        /// The manager.
        /// </value>
        public ComponentManager Manager { get { return PlayState.ComponentManager; }}

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

        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class.
        /// </summary>
        public GameComponent()
        {
            Children = new List<GameComponent>();
            Name = "uninitialized";
            IsVisible = true;
            IsActive = true;
            Tags = new List<string>();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class, while adding it to the component manager.
        /// </summary>
        /// <param name="createNew">if set to <c>true</c> adds this component to the manager..</param>
        public GameComponent(bool createNew)
        {
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

            if (createNew)
                Manager.AddComponent(this);

            Tags = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class.
        /// </summary>
        /// <param name="name">The name of the component.</param>
        /// <param name="parent">The parent component.</param>
        public GameComponent(string name, GameComponent parent) :
            this(true)
        {
            Name = name;
            RemoveFromParent();

            if(parent != null)
            {
                parent.AddChild(this);
            }
            else
            {
                Parent = null;
            }
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
            return GetRootComponent().GetChildrenOfType<T>(self).FirstOrDefault();
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
        /// Updates the component.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="chunks">The chunk manager.</param>
        /// <param name="camera">The camera.</param>
        public virtual void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
        }


        /// <summary>
        /// Renders the component.
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="chunks">The chunk manager.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="effect">The shader to use.</param>
        /// <param name="renderingForWater">if set to <c>true</c> rendering for water reflections.</param>
        public virtual void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
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

        /// <summary> This is intended to be used like Die(), but is garunteed only
        /// to clean up memory, without doing things like animations.</summary>
        public virtual void Delete()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            List<GameComponent> children = GetAllChildrenRecursive();

            foreach (GameComponent child in children)
            {
                child.Delete();
            }

            RemoveFromParent();
        }

        /// <summary>
        /// Deletes this instance and calls Die() on all descendents. Removes this instance from the parent.
        /// </summary>
        public virtual void Die()
        {
            if(IsDead)
            {
                return;
            }

            IsDead = true;
            List<GameComponent> children = GetAllChildrenRecursive();

            foreach (GameComponent child in children)
            {
                child.Die();
            }

            RemoveFromParent();
        }

        /// <summary>
        /// Gets a description of this component.
        /// </summary>
        /// <returns>Descriptive text about the component</returns>
        public virtual string GetDescription()
        {
            string toReturn = "";

            if(Parent == PlayState.ComponentManager.RootComponent)
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
            List<T> toReturn = (from child in Children
                where child is T
                select (T) child).ToList();

            if (includeSelf && this is T)
            {
                toReturn.Add((T)this);
            }
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
        /// Determines whether this component has a child with [the specified identifier].
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        ///   <c>true</c> if [has child with global identifier] [the specified identifier]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasChildWithGlobalID(uint id)
        {
            return Children.Any(child => child.GlobalID == id);
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
        /// Removes this component from its parent if it exists.
        /// </summary>
        public void RemoveFromParent()
        {
            if(Parent != null)
            {
                Parent.RemoveChild(this);
            }
        }

        /// <summary>
        /// Adds the child if it has not been added already.
        /// </summary>
        /// <param name="child">The child.</param>
        public void AddChild(GameComponent child)
        {
            if(HasChildWithGlobalID(child.GlobalID))
            {
                return;
            }

            lock (Children)
            {
                Children.Add(child);
                child.Parent = this;
            }
        }

        /// <summary>
        /// Removes the child if it exists.
        /// </summary>
        /// <param name="child">The child.</param>
        public void RemoveChild(GameComponent child)
        {
            if(!HasChildWithGlobalID(child.GlobalID))
            {
                return;
            }

            lock (Children)
            {
                Children.Remove(child);
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
        public GameComponent GetRootComponent()
        {
            GameComponent p = this;

            while(p != null && p.Parent != Manager.RootComponent)
            {
                p = p.Parent;
            }

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

    }

}