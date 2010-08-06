using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedBranch.Hammock
{
    public class EntitySerializer
    {
        public static TEntity Read<TEntity>(
            JToken data,
            ref Document d)
        {
            d.Id = (string) data["_id"];
            d.Revision = (string) data["_rev"];
            var serializer = new JsonSerializer();
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

        public static JToken WriteFragment(object o)
        {
            // serialize the entity
            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            var jwriter = new JTokenWriter();
            serializer.Serialize(jwriter, o);
            return jwriter.Token;
        }

        public static JToken Write<TEntity>(
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