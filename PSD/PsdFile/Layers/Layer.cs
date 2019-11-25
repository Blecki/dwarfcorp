/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop PSD FileType Plugin for Paint.NET
// http://psdplugin.codeplex.com/
//
// This software is provided under the MIT License:
//   Copyright (c) 2006-2007 Frank Blumenberg
//   Copyright (c) 2010-2019 Tao Yue
//
// Portions of this file are provided under the BSD 3-clause License:
//   Copyright (c) 2006, Jonas Beckeman
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace PhotoshopFile
{

  [DebuggerDisplay("Name = {Name}")]
  public class Layer
  {
    internal PsdFile PsdFile { get; private set; }

    /// <summary>
    /// The rectangle containing the contents of the layer.
    /// </summary>
    public Rectangle Rect { get; set; }

    /// <summary>
    /// Image channels.
    /// </summary>
    public ChannelList Channels { get; private set; }

    /// <summary>
    /// Returns alpha channel if it exists, otherwise null.
    /// </summary>
    public Channel AlphaChannel => Channels.SingleOrDefault(x => x.ID == -1);

    private string blendModeKey;
    /// <summary>
    /// Photoshop blend mode key for the layer
    /// </summary>
    public string BlendModeKey
    {
      get => blendModeKey;
      set
      {
        if (value.Length != 4)
        {
          throw new ArgumentException(
            $"{nameof(BlendModeKey)} must be 4 characters in length.");
        }
        blendModeKey = value;
      }
    }

    /// <summary>
    /// 0 = transparent ... 255 = opaque
    /// </summary>
    public byte Opacity { get; set; }

    /// <summary>
    /// false = base, true = non-base
    /// </summary>
    public bool Clipping { get; set; }

    private static int protectTransBit = BitVector32.CreateMask();
    private static int visibleBit = BitVector32.CreateMask(protectTransBit);
    BitVector32 flags = new BitVector32();

    /// <summary>
    /// If true, the layer is visible.
    /// </summary>
    public bool Visible
    {
      get => !flags[visibleBit];
      set => flags[visibleBit] = !value;
    }

    /// <summary>
    /// Protect the transparency
    /// </summary>
    public bool ProtectTrans
    {
      get => flags[protectTransBit];
      set => flags[protectTransBit] = value;
    }

    /// <summary>
    /// The descriptive layer name
    /// </summary>
    public string Name { get; set; }

    public BlendingRanges BlendingRangesData { get; set; }

    public MaskInfo Masks { get; set; }

    public List<LayerInfo> AdditionalInfo { get; set; }

    ///////////////////////////////////////////////////////////////////////////

    public Layer(PsdFile psdFile)
    {
      PsdFile = psdFile;
      Rect = Rectangle.Empty;
      Channels = new ChannelList();
      BlendModeKey = PsdBlendMode.Normal;
      AdditionalInfo = new List<LayerInfo>();
    }

    public Layer(PsdBinaryReader reader, PsdFile psdFile)
      : this(psdFile)
    {
      Util.DebugMessage(reader.BaseStream, "Load, Begin, Layer");

      Rect = reader.ReadRectangle();

      //-----------------------------------------------------------------------
      // Read channel headers.  Image data comes later, after the layer header.

      int numberOfChannels = reader.ReadUInt16();
      for (int channel = 0; channel < numberOfChannels; channel++)
      {
        var ch = new Channel(reader, this);
        Channels.Add(ch);
      }

      //-----------------------------------------------------------------------
      // 

      var signature = reader.ReadAsciiChars(4);
      if (signature != "8BIM")
        throw (new PsdInvalidException("Invalid signature in layer header."));

      BlendModeKey = reader.ReadAsciiChars(4);
      Opacity = reader.ReadByte();
      Clipping = reader.ReadBoolean();

      var flagsByte = reader.ReadByte();
      flags = new BitVector32(flagsByte);
      reader.ReadByte(); //padding

      //-----------------------------------------------------------------------

      // This is the total size of the MaskData, the BlendingRangesData, the 
      // Name and the AdjustmentLayerInfo.
      var extraDataSize = reader.ReadUInt32();
      var extraDataStartPosition = reader.BaseStream.Position;

      Masks = new MaskInfo(reader, this);
      BlendingRangesData = new BlendingRanges(reader, this);
      Name = reader.ReadPascalString(4);

      //-----------------------------------------------------------------------
      // Process Additional Layer Information

      long adjustmentLayerEndPos = extraDataStartPosition + extraDataSize;
      while (reader.BaseStream.Position < adjustmentLayerEndPos)
      {
        var layerInfo = LayerInfoFactory.Load(reader,
          psdFile: this.PsdFile,
          globalLayerInfo: false);
        AdditionalInfo.Add(layerInfo);
      }

      foreach (var adjustmentInfo in AdditionalInfo)
      {
        switch (adjustmentInfo.Key)
        {
          case "luni":
            Name = ((LayerUnicodeName)adjustmentInfo).Name;
            break;
        }
      }

      Util.DebugMessage(reader.BaseStream, $"Load, End, Layer, {Name}");

      PsdFile.LoadContext.OnLoadLayerHeader(this);
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create ImageData for any missing channels.
    /// </summary>
    public void CreateMissingChannels()
    {
      var channelCount = this.PsdFile.ColorMode.MinChannelCount();
      for (short id = 0; id < channelCount; id++)
      {
        if (!this.Channels.ContainsId(id))
        {
          var size = this.Rect.Height * this.Rect.Width;

          var ch = new Channel(id, this);
          ch.ImageData = new byte[size];

          if (size > 0)
          {
            unsafe
            {
              fixed (byte* ptr = &ch.ImageData[0])
              {
                Util.Fill(ptr, ptr + size, (byte)255);
              }
            }
          }

          this.Channels.Add(ch);
        }
      }
    }

    ///////////////////////////////////////////////////////////////////////////

    public void PrepareSave()
    {
      foreach (var ch in Channels)
      {
        ch.CompressImageData();
      }

      // Create or update the Unicode layer name to be consistent with the
      // ANSI layer name.
      var layerUnicodeNames = AdditionalInfo.Where(x => x is LayerUnicodeName);
      if (layerUnicodeNames.Count() > 1)
      {
        throw new PsdInvalidException(
          $"{nameof(Layer)} can only have one {nameof(LayerUnicodeName)}.");
      }

      var layerUnicodeName = (LayerUnicodeName) layerUnicodeNames.FirstOrDefault();
      if (layerUnicodeName == null)
      {
        layerUnicodeName = new LayerUnicodeName(Name);
        AdditionalInfo.Add(layerUnicodeName);
      }
      else if (layerUnicodeName.Name != Name)
      {
        layerUnicodeName.Name = Name;
      }
    }

    public void Save(PsdBinaryWriter writer)
    {
      Util.DebugMessage(writer.BaseStream, "Save, Begin, Layer");

      writer.Write(Rect);

      //-----------------------------------------------------------------------

      writer.Write((short)Channels.Count);
      foreach (var ch in Channels)
        ch.Save(writer);

      //-----------------------------------------------------------------------

      writer.WriteAsciiChars("8BIM");
      writer.WriteAsciiChars(BlendModeKey);
      writer.Write(Opacity);
      writer.Write(Clipping);

      writer.Write((byte)flags.Data);
      writer.Write((byte)0);

      //-----------------------------------------------------------------------

      using (new PsdBlockLengthWriter(writer))
      {
        Masks.Save(writer);
        BlendingRangesData.Save(writer);

        var namePosition = writer.BaseStream.Position;

        // Legacy layer name is limited to 31 bytes.  Unicode layer name
        // can be much longer.
        writer.WritePascalString(Name, 4, 31);

        foreach (LayerInfo info in AdditionalInfo)
        {
          info.Save(writer,
            globalLayerInfo: false,
            isLargeDocument: PsdFile.IsLargeDocument);
        }
      }

      Util.DebugMessage(writer.BaseStream, $"Save, End, Layer, {Name}");
    }
  }
}
