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
        public string Name { get; set; }

        public uint GlobalID { get; set; }

        public GameComponent Parent { get; set; }

        public List<GameComponent> Children { get; set; }

        private static uint maxGlobalID = 0;
        private uint maxLocalID = 0;

        public bool IsVisible { get; set; }
        public bool IsActive { get; set; }
        public bool IsDead { get; set; }

        public List<string> Tags { get; set; }

        public ComponentManager Manager { get { return PlayState.ComponentManager; }}

        private static Object globalIdLock = new object();

        public virtual void ReceiveMessageRecursive(Message messageToReceive)
        {
            foreach(GameComponent child in Children)
            {
                child.ReceiveMessageRecursive(messageToReceive);
            }
        }


        public static void ResetMaxGlobalId(uint value)
        {
            maxGlobalID=value;
        }


        public GameComponent()
        {
            Children = new List<GameComponent>();
            Name = "uninitialized";
            IsVisible = true;
            IsActive = true;
            Tags = new List<string>();
        }

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

            Manager.AddComponent(this);

            Tags = new List<string>();
        }

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


        public uint GetNextLocalID()
        {
            return maxLocalID++;
        }


        public T GetComponent<T>(bool self=true) where T : GameComponent
        {
            return GetRootComponent().GetChildrenOfType<T>(self).FirstOrDefault();
        }

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


        public virtual void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
        }


        public virtual void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
        }

        public override int GetHashCode()
        {
            return (int) GlobalID;
        }

        public void SetActiveRecursive(bool active)
        {
            IsActive = active;

            foreach(GameComponent child in Children)
            {
                child.SetActiveRecursive(active);
            }
        }

        public void SetVisibleRecursive(bool visible)
        {
            IsVisible = visible;

            foreach(GameComponent child in Children)
            {
                child.SetVisibleRecursive(visible);
            }
        }

        // This is intended to be used like Die(), but is garunteed only
        // to clean up memory, without doing things like animations.
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

        public bool HasChildWithType<T>() where T : GameComponent
        {
            return Children.OfType<T>().Any();
        }

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

        public bool HasChildWithName(string name)
        {
            return Children.Any(child => child.Name == name);
        }

        public List<GameComponent> GetChildrenWithName(string name)
        {
            return Children.Where(child => child.Name == name).ToList();
        }

        public bool HasChildWithGlobalID(uint id)
        {
            return Children.Any(child => child.GlobalID == id);
        }

        public GameComponent GetChildWithGlobalID(uint id)
        {
            return Children.FirstOrDefault(child => child.GlobalID == id);
        }


        public void RemoveFromParent()
        {
            if(Parent != null)
            {
                Parent.RemoveChild(this);
            }
        }

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

        public bool HasChildWithNameRecursive(string name)
        {
            return Children.Any(child => child.Name == name || child.HasChildWithNameRecursive(name));
        }

        public GameComponent GetRootComponent()
        {
            GameComponent p = this;

            while(p != null && p.Parent != Manager.RootComponent)
            {
                p = p.Parent;
            }

            return p;
        }

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

        public bool HasChildWithGlobalIDRecursive(uint id)
        {
            return Children.Any(child => child.GlobalID == id || child.HasChildWithGlobalIDRecursive(id));
        }

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


        public bool HasChildWithTypeRecursive<T>() where T : GameComponent
        {
            return Children.Any(child => child is T || child.HasChildWithTypeRecursive<T>());
        }

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