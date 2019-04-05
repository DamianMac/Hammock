// 
//  EntitySerializer.cs
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
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace RedBranch.Hammock
{
    public class EntitySerializer
    {
        Session _sx;
        JsonSerializer _jsonSerializer;
        
        public EntitySerializer(Session sx)
        {
            _sx = sx;
            
            _jsonSerializer = new JsonSerializer();
            _jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            _jsonSerializer.ContractResolver = new _ContractResolver(_sx);
        }
      
        protected JsonSerializer GetJsonSerializer()
        {
            return _jsonSerializer;
        }
        
        class _ContractResolver : DefaultContractResolver
        {
            Session _sx;
            ReferenceConverter _referenceConverter;
            ByteArrayConverter _bufferConverter;
            
            public _ContractResolver(Session sx)
            {
                _sx = sx;
            }

            protected override JsonConverter ResolveContractConverter(Type objectType)
            {
                if (typeof(IReference).IsAssignableFrom(objectType) ||
					(objectType.IsGenericType && typeof(Lazy<>) == objectType.GetGenericTypeDefinition()))
                {
                    return _referenceConverter ?? (_referenceConverter = new ReferenceConverter(_sx));
                }
                if (typeof(byte[]) == objectType)
                {
                    return _bufferConverter ?? (_bufferConverter = new ByteArrayConverter());
                }
                return base.ResolveContractConverter(objectType);
            }
        }
        
        public TEntity Read<TEntity>(
            JToken data,
            ref Document d)
        {
            d.Id = (string) data["_id"];
            d.Revision = (string) data["_rev"];
            var serializer = GetJsonSerializer();
            var e = (TEntity)serializer.Deserialize(new JTokenReader(data), typeof(TEntity));

            // if the entity subclasses document, use the entity itself
            // as the document.
            var docsubclass = e as Document;
            if (null != docsubclass)
            {
                docsubclass.Id = d.Id;
                docsubclass.Revision = d.Revision;
                docsubclass.Session = d.Session;
                d = docsubclass;
            }

            // fill the document property if the entity implements IHasDocument
            var icanhasdoc = e as IHasDocument;
            if (null != icanhasdoc)
            {
                icanhasdoc.Document = d;
            }

            // read attachments
            Attachments attachments = null;
            if (null != data["_attachments"])
            {
                attachments = (Attachments)serializer.Deserialize(new JTokenReader(data["_attachments"]), typeof (Attachments));
            }
            var ihasattachments = e as IHasAttachments;
            if (null != ihasattachments)
            {
                ihasattachments.Attachments = attachments;
                
                // fill non-serialized properties
                if (null != attachments)
                {
                    foreach (var a in attachments)
                    {
                        a.Value.Name = a.Key;
                        a.Value.Session = d.Session;
                        a.Value.Document = d;
                    }
                }
            }

            return e;
        }

        public JToken WriteFragment(object o)
        {
            // serialize the entity
            var serializer = GetJsonSerializer();
            var jwriter = new JTokenWriter();
            serializer.Serialize(jwriter, o);
            return jwriter.Token;
        }

        public JToken Write<TEntity>(
            TEntity e,
            Document d)
        {
            var o = (JObject)WriteFragment(e);
            
            // add document info
            var icanhasdoc = e as IHasDocument;
            if (null != icanhasdoc)
            {
                d = d ?? icanhasdoc.Document;
            }
            if (null != d)
            {
                if (!String.IsNullOrEmpty(d.Revision)) o.AddFirst(new JProperty("_rev", d.Revision));
                if (!String.IsNullOrEmpty(d.Id))       o.AddFirst(new JProperty("_id", d.Id));
            }

            // add attachments
            var ihasattachments = e as IHasAttachments;
            if (null != ihasattachments && null != ihasattachments.Attachments)
            {
                o.Add("_attachments", WriteFragment(ihasattachments.Attachments));
            }

            return o;
        }
    }
}