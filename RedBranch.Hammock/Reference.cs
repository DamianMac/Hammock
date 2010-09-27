using System;

using Newtonsoft.Json;

namespace RedBranch.Hammock
{
    /// <summary>
    /// Provides a weak reference to another entity in the datastore.
    /// </summary>
    public abstract class Reference
    {
        protected static Document GetDocument(object e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("Entity is null.");
            }
            var doc = e as Document;
            if (null == doc)
            {
                var hasdoc = e as IHasDocument;
                if (null == hasdoc)
                {
                    throw new InvalidOperationException("Entities must either inherit from Document or implement IHasDocument to participate as references.");
                } else
                {
                    doc = hasdoc.Document;
                }
            }
            return doc;
        }

        public abstract string Id
        {
            get;
        }

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
            return new _StrongReference<TEntity>(entity);
        }
    }

    public abstract class Reference<TEntity> : Reference where TEntity : class
    {
        public abstract TEntity Value
        {
            get;
        }
    }

    class _LazyReference<TEntity> : Reference<TEntity> where TEntity : class
    {
        Session _sx;
        string _id;
        TEntity _entity;

        public _LazyReference(Session sx, string id)
        {
            _sx = sx;
            _id = id;
        }

        public override string Id
        {
            get { return _id; }
        }

        public override TEntity Value
        {
            get
            {
                if (null == _entity)
                {
                    _entity = _sx.Load<TEntity>(_id);
                }
                return _entity;
            }
        }
    }

    class _WeakReference<TEntity> : Reference<TEntity> where TEntity : class
    {
        string _id;

        public _WeakReference(string id)
        {
            _id = id;
        }

        public override string Id
        {
            get { return _id; }
        }

        public override TEntity Value
        {
            get
            {
                throw new InvalidOperationException();
            }
        }
    }

    class _StrongReference<TEntity> : Reference<TEntity> where TEntity : class
    {
        TEntity _entity;

        public _StrongReference(TEntity entity)
        {
            _entity = entity;
        }

        public override string Id
        {
            get
            {
                if (null == _entity)
                    return null;
                var doc = GetDocument(_entity);
                if (null == doc)
                    return null;
                return doc.Id;
            }
        }

        public override TEntity Value
        {
            get { return _entity; }
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
            return !objectType.IsAbstract && 
                   objectType.IsGenericType &&
                   typeof(Reference).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (null == value)
            {
                writer.WriteNull();
            } else
            {
                var reference = (Reference)value;
                var id = reference.Id;
                if (String.IsNullOrEmpty(id))
                {
                    throw new InvalidOperationException("Referenced entities must be saved before the entity that references them.");
                }
                writer.WriteValue((string)id);
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

