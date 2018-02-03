// ResourceAmount.cs
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
using System.Diagnostics;

namespace DwarfCorp
{
    /// <summary>
    /// This is just a struct of two things: a resource, and a number of that resource.
    /// This is used instead of a list, since there is nothing distinguishing resources from each other.
    /// </summary>
    public class ResourceAmount : Quantitiy<ResourceType>
    {

        public ResourceAmount(ResourceAmount amount)
        {
            ResourceType = amount.ResourceType;
            NumResources = amount.NumResources;
        }

        public ResourceAmount(ResourceType type)
        {
            ResourceType = type;
            NumResources = 1;
        }

        public ResourceAmount(Resource resource)
        {
            ResourceType = resource.Name;
            NumResources = 1;
        }

        public ResourceAmount(string resource)
        {
            ResourceType = resource;
            NumResources = 1;
        }

        public ResourceAmount(Body component)
        {
            // Assume that the first tag of the body is
            // the name of the resource.
            ResourceType = component.Tags[0];
            NumResources = 1;
        }

        public ResourceAmount(Resource resourceType, int numResources)
        {
            ResourceType = resourceType.Name;
            NumResources = numResources;
        }

        public ResourceAmount(ResourceType type, int num) :
            this(ResourceLibrary.Resources[type], num)
        {
            
        }

        public ResourceAmount()
        {
            
        }

        public ResourceAmount CloneResource()
        {
            return new ResourceAmount(ResourceType, NumResources);
        }
    }

}