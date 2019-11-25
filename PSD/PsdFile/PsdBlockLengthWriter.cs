/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop PSD FileType Plugin for Paint.NET
// http://psdplugin.codeplex.com/
//
// This software is provided under the MIT License:
//   Copyright (c) 2006-2007 Frank Blumenberg
//   Copyright (c) 2010-2014 Tao Yue
//
// Portions of this file are provided under the BSD 3-clause License:
//   Copyright (c) 2006, Jonas Beckeman
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;
using System.Text;

namespace PhotoshopFile
{
  /// <summary>
  /// Writes the actual length in front of the data block upon disposal.
  /// </summary>
  class PsdBlockLengthWriter : IDisposable
  {
    private bool disposed = false;

    long lengthPosition;
    long startPosition;
    bool hasLongLength;
    PsdBinaryWriter writer;

    public PsdBlockLengthWriter(PsdBinaryWriter writer)
      : this(writer, false)
    {
    }

    public PsdBlockLengthWriter(PsdBinaryWriter writer, bool hasLongLength)
    {
      this.writer = writer;
      this.hasLongLength = hasLongLength;

      // Store position so that we can return to it when the length is known.
      lengthPosition = writer.BaseStream.Position;

      // Write a sentinel value as a placeholder for the length.
      writer.Write((UInt32)0xFEEDFEED);
      if (hasLongLength)
      {
        writer.Write((UInt32)0xFEEDFEED);
      }

      // Store the start position of the data block so that we can calculate
      // its length when we're done writing.
      startPosition = writer.BaseStream.Position;
    }

    public void Write()
    {
      var endPosition = writer.BaseStream.Position;

      writer.BaseStream.Position = lengthPosition;
      long length = endPosition - startPosition;
      if (hasLongLength)
      {
        writer.Write(length);
      }
      else
      { 
        writer.Write((UInt32)length);
      }

      writer.BaseStream.Position = endPosition;
    }

    public void Dispose()
    {
      if (!this.disposed)
      {
        Write();
        this.disposed = true;
      }
    }
  }

}