/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop PSD FileType Plugin for Paint.NET
// http://psdplugin.codeplex.com/
//
// This software is provided under the MIT License:
//   Copyright (c) 2006-2007 Frank Blumenberg
//   Copyright (c) 2010-2017 Tao Yue
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoshopFile
{
  public class VersionInfo : ImageResource
  {
    public override ResourceID ID => ResourceID.VersionInfo;

    public UInt32 Version { get; set; }

    public bool HasRealMergedData { get; set; }

    public string ReaderName { get; set; }

    public string WriterName { get; set; }

    public UInt32 FileVersion { get; set; }

    
    public VersionInfo() : base(String.Empty)
    {
    }

    public VersionInfo(PsdBinaryReader reader, string name)
      : base(name)
    {
      Version = reader.ReadUInt32();
      HasRealMergedData = reader.ReadBoolean();
      ReaderName = reader.ReadUnicodeString();
      WriterName = reader.ReadUnicodeString();
      FileVersion = reader.ReadUInt32();
    }

    protected override void WriteData(PsdBinaryWriter writer)
    {
      writer.Write(Version);
      writer.Write(HasRealMergedData);
      writer.WriteUnicodeString(ReaderName);
      writer.WriteUnicodeString(WriterName);
      writer.Write(FileVersion);
    }
  }
}
