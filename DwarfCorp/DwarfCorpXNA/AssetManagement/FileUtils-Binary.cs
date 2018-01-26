// FileUtils.cs
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using DwarfCorp.GameStates;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace DwarfCorp
{
    public static partial class FileUtils
    {
        /// <summary>
        /// Loads a serialized binary object from the given file.
        /// </summary>
        /// <typeparam name="T">The type to deserialize</typeparam>
        /// <param name="filepath">The filepath.</param>
        /// <returns>A deserialized object of type T if it could be deserialized, exception otherwise.</returns>
        public static T LoadBinary<T>(string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            T toReturn = default(T);
            using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.None))
            { 
                try
                {
                    stream.Position = 0;
                    toReturn = (T) formatter.Deserialize(stream);
                }
                catch (InvalidCastException e)
                {
                    Console.Error.WriteLine(e);
                }

                stream.Flush();
            }
            return toReturn;
        }

        /// <summary>
        /// Serializes and saves an object to binary file path.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="filepath">The filepath.</param>
        /// <returns>True if the object could be saved.</returns>
        public static bool SaveBinary<T>(T obj, string filepath)
        {
            IFormatter formatter = new BinaryFormatter();
            using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                formatter.Serialize(stream, obj);
            }
            return true;
        }
    }
}
