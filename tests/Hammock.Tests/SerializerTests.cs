// 
//  SerializerTests.cs
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
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Hammock.Tests
{
    
    public class SerializerTests
    {
        public class EntityWithByteArray
        {
            public byte[] PasswordHash { get; set; }
        }

        [Fact]
        public void Entity_serializer_can_deserialize_byte_array_as_json_array_of_bytes()
        {
            var serializer = new EntitySerializer(null);
            var obj = JObject.Parse(@"{ _id: ""foo"", PasswordHash: [1,2,3,4] }");
            var doc = new Document();
            var ent = serializer.Read<EntityWithByteArray>(obj, ref doc);
            Assert.Equal(4, ent.PasswordHash.Length);
        }
    }
}
