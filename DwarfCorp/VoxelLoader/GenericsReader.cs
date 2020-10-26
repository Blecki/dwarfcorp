using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CsharpVoxReader
{
  public class GenericsReader {

    public static byte[] ReadByteArray(BinaryReader br, ref int readsize) {
      Int32 numChars = br.ReadInt32();
      readsize += sizeof(Int32) + numChars;

      return br.ReadBytes(numChars);
    }

    public static Dictionary<string, byte[]> ReadDict(BinaryReader br, ref int readsize) {
      Dictionary<string, byte[]> result = new Dictionary<string, byte[]>();

      Int32 numElements = br.ReadInt32();
      readsize += sizeof(Int32);

      for (int i=0; i < numElements; i++) {
        string key = Encoding.UTF8.GetString(ReadByteArray(br, ref readsize));
        byte[] value = ReadByteArray(br, ref readsize);

        result[key] = value;
      }

      return result;
    }

    public static int[] ReadRotation(BinaryReader br, ref int readsize) {
      byte rot = br.ReadByte();
      readsize += 1;

      int r0v = ((rot & 8) == 0)?1:-1;
      int r1v = ((rot & 16) == 0)?1:-1;
      int r2v = ((rot & 32) == 0)?1:-1;

      int r0i = rot & 3;
      int r1i = (rot & 12) >> 2;
      /*
      Truth table for the third index
        r0| 0 | 1 | 2 |
      r1--+---+---+---+
       0  | X | 2 | 1 |
      ----+---+---+---+
       1  | 2 | X | 0 |
      ----+---+---+---+
       2  | 1 | 0 | X |
      ----+---+---+---+

      Derived function
      f(r0, r1) = 3 - r0 - r1
      */
      int r2i = 3 - r0i - r1i;

      int[] result = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

      result[r0i] = r0v;
      result[r1i + 3] = r1v;
      result[r2i + 6] = r2v;

      return result;
    }

  }
}
