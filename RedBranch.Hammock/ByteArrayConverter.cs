// 
//  ByteArrayConverter.cs
//  
//  Author:
//       Nick Nystrom <nnystrom@gmail.com>
//  
//  Copyright (c) 2009-2011 Nicholas J. Nystrom
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RedBranch.Hammock
{
    class ByteArrayConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var buf = value as byte[];
            if (null == buf)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteStartArray();
            foreach (var b in buf)
            {
                writer.WriteValue(b);
            }
            writer.WriteEndArray();    
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType == JsonToken.StartArray)
            {
                var buf = new List<byte>(128);
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    if (reader.TokenType != JsonToken.Integer)
                    {
                        throw new Exception("Invalid token type, expecting integer, found " + reader.TokenType);
                    }
                    buf.Add((byte)(long)reader.Value);
                }
                return buf.ToArray();
            }
            if (reader.TokenType == JsonToken.String)
            {
                throw new Exception("String values for byte[] not yet supported.");
            }
            throw new Exception("Expected an array for byte[], found " + reader.TokenType);
        }

        public override bool CanConvert(Type objectType)
        {
            var retval =  objectType == typeof (byte[]);
            return retval;
        }
    }
}
