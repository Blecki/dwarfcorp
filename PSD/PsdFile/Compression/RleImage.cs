/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop PSD FileType Plugin for Paint.NET
// http://psdplugin.codeplex.com/
//
// This software is ptortorovided under the MIT License:
//   Copyright (c) 2006-2007 Frank Blumenberg
//   Copyright (c) 2010-2017 Tao Yue
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PhotoshopFile.Compression
{
  internal class RleImage : ImageData
  {
    private byte[] rleData;
    private RleRowLengths rleRowLengths;

    protected override bool AltersWrittenData => false;

    public RleImage(byte[] rleData, RleRowLengths rleRowLengths,
      Size size, int bitDepth)
      : base(size, bitDepth)
    {
      this.rleData = rleData;
      this.rleRowLengths = rleRowLengths;
    }

    internal override void Read(byte[] buffer)
    {
      var rleStream = new MemoryStream(rleData);
      var rleReader = new RleReader(rleStream);
      var bufferIndex = 0;
      for (int i = 0; i < Size.Height; i++)
      {
        var bytesRead = rleReader.Read(buffer, bufferIndex, BytesPerRow);
        if (bytesRead != BytesPerRow)
        {
          throw new Exception("RLE row decompressed to unexpected length.");
        }
        bufferIndex += bytesRead;
      }
    }

    public override byte[] ReadCompressed()
    {
      return rleData;
    }

    internal override void WriteInternal(byte[] array)
    {
      if (rleData != null)
      {
        throw new Exception(
          "Cannot write to RLE image in Decompress mode.");
      }

      using (var dataStream = new MemoryStream())
      {
        var rleWriter = new RleWriter(dataStream);
        for (int row = 0; row < Size.Height; row++)
        {
          int rowIndex = row * BytesPerRow;
          rleRowLengths[row] = rleWriter.Write(
            array, rowIndex, BytesPerRow);
        }

        // Save compressed data
        dataStream.Flush();
        rleData = dataStream.ToArray();
        Debug.Assert(rleRowLengths.Total == rleData.Length,
          "RLE row lengths do not sum to the compressed data length.");
      }
    }
  }
}
