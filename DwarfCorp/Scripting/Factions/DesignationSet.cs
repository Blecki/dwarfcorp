// Faction.cs
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
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using LibNoise;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DesignationSet
    {
        public class VoxelDesignation
        {
            public bool Visible = true;
            public VoxelHandle Voxel;
            public DesignationType Type;
            public Object Tag;
            public Task Task;
        }

        public class EntityDesignation
        {
            public Body Body;
            public DesignationType Type;
            public Object Tag;
            public Task Task;
        }

        public enum AddDesignationResult
        {
            AlreadyExisted,
            Added
        }

        public enum RemoveDesignationResult
        {
            DidntExist,
            Removed
        }

        [JsonProperty]
        private Dictionary<ulong, List<VoxelDesignation>> VoxelDesignations = new Dictionary<ulong, List<VoxelDesignation>>();

        [JsonProperty] // Todo: Replace with more effecient data structure?
        private List<EntityDesignation> EntityDesignations = new List<EntityDesignation>();

        private object designationLock = new object();

        private static bool TypeSet(DesignationType DesType, DesignationType FilterType)
        {
            return (FilterType & DesType) != 0;
        }

        public AddDesignationResult AddVoxelDesignation(VoxelHandle Voxel, DesignationType Type, Object Tag, Task Task)
        {
            lock (designationLock)
            {
                var key = VoxelHelpers.GetVoxelQuickCompare(Voxel);

                List<VoxelDesignation> list = null;
                if (VoxelDesignations.ContainsKey(key))
                    list = VoxelDesignations[key];
                else
                {
                    list = new List<VoxelDesignation>();
                    VoxelDesignations.Add(key, list);
                }

                var existingEntry = list.FirstOrDefault(d => d.Type == Type);

                if (existingEntry != null)
                {
                    existingEntry.Tag = Tag;
                    return AddDesignationResult.AlreadyExisted;
                }
                else
                {
                    list.Add(new VoxelDesignation
                    {
                        Voxel = Voxel,
                        Type = Type,
                        Tag = Tag,
                        Task = Task
                    });
                    Voxel.Invalidate();
                    return AddDesignationResult.Added;
                }
            }
        }

        public RemoveDesignationResult RemoveVoxelDesignation(VoxelHandle Voxel, DesignationType Type)
        {
            lock (designationLock)
            {
                var key = VoxelHelpers.GetVoxelQuickCompare(Voxel);
                if (!VoxelDesignations.ContainsKey(key)) return RemoveDesignationResult.DidntExist;
                var list = VoxelDesignations[key];
                var r = list.RemoveAll(d => TypeSet(d.Type, Type)) == 0 ? RemoveDesignationResult.DidntExist : RemoveDesignationResult.Removed;
                if (list.Count == 0)
                    VoxelDesignations.Remove(key);
                Voxel.Invalidate();
                return r;
            }
        }

        public VoxelDesignation GetVoxelDesignation(VoxelHandle Voxel, DesignationType Type)
        {
            lock (designationLock)
            {
                var key = VoxelHelpers.GetVoxelQuickCompare(Voxel);
                if (!VoxelDesignations.ContainsKey(key)) return null;
                var r = VoxelDesignations[key].FirstOrDefault(d => TypeSet(d.Type, Type));
                if (r != null) return r;
                return null;
            }
        }

        public bool IsVoxelDesignation(VoxelHandle Voxel, DesignationType Type)
        {
            lock (designationLock)
            {
                var key = VoxelHelpers.GetVoxelQuickCompare(Voxel);
                if (!VoxelDesignations.ContainsKey(key)) return false;
                return VoxelDesignations[key].Any(d => TypeSet(d.Type, Type));
            }
        }

        public IEnumerable<VoxelDesignation> EnumerateDesignations(DesignationType Type)
        {
            lock (designationLock)
            {
                foreach (var key in VoxelDesignations)
                    foreach (var d in key.Value)
                        if (TypeSet(d.Type, Type))
                            yield return d;
            }
        }

        public IEnumerable<VoxelDesignation> EnumerateDesignations()
        {
            lock (designationLock)
            {
                foreach (var key in VoxelDesignations)
                    foreach (var d in key.Value)
                        yield return d;
            }
        }

        public IEnumerable<VoxelDesignation> EnumerateDesignations(VoxelHandle Voxel)
        {
            lock (designationLock)
            {
                var key = VoxelHelpers.GetVoxelQuickCompare(Voxel);
                if (VoxelDesignations.ContainsKey(key))
                    foreach (var des in VoxelDesignations[key])
                        yield return des;
            }
        }

        // Todo: Probably a more effecient way to do this.
        public void CleanupDesignations()
        {
            lock (designationLock)
            {
                EntityDesignations.RemoveAll(b => b.Body.IsDead);
            }
        }

        public AddDesignationResult AddEntityDesignation(Body Entity, DesignationType Type, Object Tag, Task Task)
        {
            lock (designationLock)
            {
                if (EntityDesignations.Count(e => Object.ReferenceEquals(e.Body, Entity) && e.Type == Type) == 0)
                {
                    EntityDesignations.Add(new EntityDesignation
                    {
                        Body = Entity,
                        Type = Type,
                        Tag = Tag,
                        Task = Task
                    });

                    return AddDesignationResult.Added;
                }

                return AddDesignationResult.AlreadyExisted;
            }
        }

        public RemoveDesignationResult RemoveEntityDesignation(Body Entity, DesignationType Type)
        {
            lock (designationLock)
            {
                if (EntityDesignations.RemoveAll(e => Object.ReferenceEquals(e.Body, Entity) && TypeSet(e.Type, Type)) != 0)
                    return RemoveDesignationResult.Removed;

                return RemoveDesignationResult.DidntExist;
            }
        }

        public bool IsDesignation(Body Entity, DesignationType Type)
        {
            lock (designationLock)
            {
                return EntityDesignations.Count(e => Object.ReferenceEquals(e.Body, Entity) && TypeSet(e.Type, Type)) != 0;
            }
        }

        public IEnumerable<EntityDesignation> EnumerateEntityDesignations(DesignationType Type)
        {
            lock (designationLock)
            {
                return EntityDesignations.Where(e => TypeSet(e.Type, Type));
            }
        }

        public IEnumerable<EntityDesignation> EnumerateEntityDesignations(Body Entity)
        {
            lock (designationLock)
            {
                return EntityDesignations.Where(e => Object.ReferenceEquals(Entity, e.Body));
            }
        }

        public IEnumerable<EntityDesignation> EnumerateEntityDesignations()
        {
            lock (designationLock)
            {
                return EntityDesignations;
            }
        }

        public EntityDesignation GetEntityDesignation(Body Entity, DesignationType Type)
        {
            foreach (var des in EnumerateEntityDesignations(Type))
                if (Object.ReferenceEquals(des.Body, Entity))
                    return des;
            return null;
        }
    }
}
