using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace RedBranch.Hammock.Test
{
    [TestFixture]
    public class SerializerTests
    {
        public class EntityWithByteArray
        {
            public byte[] PasswordHash { get; set; }
        }

        [Test]
        public void Entity_serializer_can_deserialize_byte_array_as_json_array_of_bytes()
        {
            var serializer = new EntitySerializer(null);
            var obj = JObject.Parse(@"{ _id: ""foo"", PasswordHash: [1,2,3,4] }");
            var doc = new Document();
            var ent = serializer.Read<EntityWithByteArray>(obj, ref doc);
            Assert.That(ent.PasswordHash.Length, Is.EqualTo(4));
        }
    }
}
