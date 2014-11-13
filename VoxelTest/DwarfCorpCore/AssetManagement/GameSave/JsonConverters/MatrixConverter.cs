using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{

    /// <summary>
    /// Serializes and deserializes Matrix objects to JSON.
    /// </summary>
    public class MatrixConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }



        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JValue jObject = serializer.Deserialize<JValue>(reader);
            List<string> rows = jObject.Value.ToString().Split('{', '}').ToList();
            List<string> nonEmptyRows = rows.Where(row => row != "" && row != " ").ToList();
            List<string[]> values = nonEmptyRows.Select(row => row.Split(' ')).ToList();

            float[,] vs = new float[4, 4];

            for(int r = 0; r < 4; r++)
            {
                for(int c = 0; c < 4; c++)
                {
                    vs[r, c] = float.Parse(values[r][c].Split(':')[1]);
                }
            }
            Matrix toReturn = new Matrix(vs[0, 0], vs[0, 1], vs[0, 2], vs[0, 3], vs[1, 0], vs[1, 1], vs[1, 2], vs[1, 3], vs[2, 0], vs[2, 1], vs[2, 2], vs[2, 3], vs[3, 0], vs[3, 1], vs[3, 2], vs[3, 3]);
            return toReturn;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Matrix);
        }
    }

}