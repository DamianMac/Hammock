//
//  Reference.cs
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

using Newtonsoft.Json;

namespace RedBranch.Hammock
{
    /// <summary>
    /// Provides a weak reference to another entity in the datastore.
    /// </summary>
    public static class Reference
    {
        public static Reference<TEntity> To<TEntity>(Session sx, string id) where TEntity : class
        {
            return new _LazyReference<TEntity>(sx, id);
        }

        public static object To(Session sx, string id, Type t)
        {
            return typeof(_LazyReference<>).MakeGenericType(t).GetConstructor(new Type[] { typeof(Session), typeof(string) }).Invoke(new object[] { sx, id });
        }

        public static Reference<TEntity> To<TEntity>(string id) where TEntity : class
        {
            return new _WeakReference<TEntity>(id);
        }

        public static Reference<TEntity> To<TEntity>(TEntity entity) where TEntity : class
        {
            return new _StrongReference<TEntity>(null, entity);
        }
		
		public static Reference<TEntity> To<TEntity>(TEntity entity, Session sx) where TEntity : class
		{
			return new _StrongReference<TEntity>(sx, entity);
		}
    }
	
	public interface IReference
	{
		string Id { get; }
	}

    public abstract class Reference<TEntity> : Lazy<TEntity>, IReference where TEntity : class
    {
		protected Reference(Func<TEntity> factory) : base(factory, System.Threading.LazyThreadSafetyMode.None)
		{		
		}
		
        public abstract string Id
        {
            get;
        }
    }

    class _LazyReference<TEntity> : Reference<TEntity> where TEntity : class
    {
        string _id;

        public _LazyReference(Session sx, string id)
            : base(() => sx.Load<TEntity>(id))
        {
            _id = id;
        }

        public override string Id
        {
            get { return _id; }
        }
    }

    class _WeakReference<TEntity> : Reference<TEntity> where TEntity : class
    {
        string _id;

        public _WeakReference(string id) : base(() => { throw new InvalidOperationException(); })
        {
            _id = id;
        }

        public override string Id
        {
            get { return _id; }
        }
    }

    class _StrongReference<TEntity> : Reference<TEntity> where TEntity : class
    {
		Session _sx;
		TEntity _entity;

        public _StrongReference(Session sx, TEntity entity) : base(() => entity)
        {
			_entity = entity;
			
			_sx = sx;
			if (null == sx &&
				null != _entity &&
				null == (_entity as Document) &&
				null == (_entity as IHasDocument))
			{
				throw new ArgumentNullException("sx",
					"You must supply a Session instance when creating a strong reference if the entity does not subclass Document or implement IHasDocument.");
			}
        }

        public override string Id
        {
            get
            {
				// prefer to inspect the entity for a document
				var doc = _entity as Document;
				if (null != doc) return doc.Id;
				var hasdoc = _entity as IHasDocument;
				if (null != hasdoc && null != hasdoc.Document) return hasdoc.Document.Id;
				
				// poco, so ask the session
				doc = _sx.GetDocument(_entity);
                if (null != doc) return doc.Id;
				return null;
            }
        }
    }

    class ReferenceConverter : JsonConverter
    {
        Session _sx;

        public ReferenceConverter(Session sx)
        {
            _sx = sx;
        }

        public override bool CanConvert(Type objectType)
        {
			return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (null == value)
            {
                writer.WriteNull();
				return;
            }
			
			var t = value.GetType();
			if (typeof(_StrongReference<>).IsAssignableFrom(t))
			{
				// strong references to poco objects are more difficult to resolve	
				
			}
			if (typeof(IReference).IsAssignableFrom(t))
            {
                var reference = (IReference)value;
                var id = reference.Id;
                if (String.IsNullOrEmpty(id))
                {
                    throw new InvalidOperationException(
						"Referenced entities must be saved before the entity that references them.");
                }
                writer.WriteValue((string)id);
            }
			else if (typeof(Document).IsAssignableFrom(t))
			{
				var doc = (Document)value;
				writer.WriteValue(doc.Id);
			}
			else if (typeof(IHasDocument).IsAssignableFrom(t))
			{
				var entity = (IHasDocument)value;
				this.WriteJson(writer, entity.Document, serializer);
			}
			else if (t.IsGenericType && typeof(Lazy<>) == t.GetGenericTypeDefinition())
			{
				var entity = t.GetProperty("Value").GetValue(value, null);
				this.WriteJson(writer, entity, serializer);
			}
			else
			{
				var doc = _sx.GetDocument(value);
                if (null != doc)
				{
					this.WriteJson(writer, doc, serializer);
				}
				else
				{
					throw new InvalidOperationException(
						"Entities must either subclass Document, implement IHasDocument, or be enrolled in the Session to participate as references.");
				}
			}
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType == JsonToken.String)
            {
                var r = Reference.To(_sx, (string)reader.Value, objectType.GetGenericArguments()[0]);
                return r;
            }
            throw new Exception("References must be stored as JSON strings.");
        }
    }
}

