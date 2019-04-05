// 
//  Attachment.cs
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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace RedBranch.Hammock
{
    public interface IHasAttachments
    {
        Attachments Attachments { get; set; }        
    }
    
    public class Attachments : Dictionary<string, Attachment>
    {
    }

    public class Attachment
    {
        [JsonIgnore] public Session Session { get; set; }
        [JsonIgnore] public Document Document { get; set; }
        [JsonIgnore] public string Name { get; set; }
        
        [JsonProperty("stub")] public bool? Stub { get; set; }
        [JsonProperty("content_type")] public string ContentType { get; set; }
        [JsonProperty("length")] public int Length { get; set; }

        public HttpWebResponse Load()
        {
            // send the attachment
            var request = (HttpWebRequest)WebRequest.Create(Document.Location + "/" + Name);
            request.Method = "GET";

            // get couch reply
            return (HttpWebResponse)request.GetResponse();
        }

        public Stream LoadStream()
        {
            return Load().GetResponseStream();
        }

        public byte[] LoadBytes()
        {
            using (var stream = LoadStream())
            {
                return SafeReadStream(stream);
            }
        }

        /// <summary>
        /// Reads a stream without relying on the stream length. This is the only safe way
        /// to read an http response stream.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static byte[] SafeReadStream(Stream s)
        {
            var x = new byte[1024];
            var ms = new MemoryStream();
            var count = 0;
            while (0 < (count = s.Read(x, 0, x.Length)))
            {
                ms.Write(x, 0, count);
            }

            return ms.ToArray();
        }
    }

    public partial class Session
    {
        public Document AttachFile<TEntity>(TEntity entity, string filename, HttpWebResponse response) where TEntity : class
        {
            using (var stream = response.GetResponseStream())
            {
                return AttachFile(entity, filename, response.ContentType, response.ContentLength, stream);
            }
        }

        public Document AttachFile<TEntity>(TEntity entity, HttpPostedFileBase file) where TEntity : class
        {
            return AttachFile(entity, file.FileName, file.ContentType, file.ContentLength, file.InputStream);
        }

        public Document AttachFile<TEntity>(TEntity entity, string filename, string contentType, Stream data) where TEntity : class
        {
            return AttachFile(entity, filename, contentType, data.CanSeek ? data.Length : -1, data);
        }

        public Document AttachFile<TEntity>(TEntity entity, string filename, string contentType, long contentLength, Stream data) where TEntity : class
        {
            var withattachments = entity as IHasAttachments;
            if (null == withattachments)
            {
                throw new NotSupportedException("An entity must implement IHasAttachments in order to attach a file to it.");
            }

            if (!IsEnrolled(entity))
            {
                throw new Exception("An entity must be enrolled in the session in order to attach files. Use Save() first, then attach the file.");
            }
            var d = _entities[entity];

            byte[] buf = null;
            if (contentLength >= 0)
            {
                buf = new byte[contentLength];
                data.Read(buf, 0, buf.Length);
            }
            else
            {
                buf = Attachment.SafeReadStream(data);
            }

            // send the attachment
            var request = (HttpWebRequest) WebRequest.Create(
                String.Format("{0}/{1}?rev={2}", d.Location, filename, d.Revision)
            );
            request.Method = "PUT";
            request.ContentType = contentType;
            request.ContentLength = buf.Length;
            using (var output = request.GetRequestStream())
            {
                output.Write(buf, 0, buf.Length);
            }

            // get couch reply
            using (var reader = request.GetCouchResponse())
            {
                var serializer = new JsonSerializer();
                var response = (__DocumentResponse)serializer.Deserialize(reader, typeof(__DocumentResponse));
                d = response.ToDocument(this);
            }

            d = UpdateEntityDocument(entity, d);

            // build the attachment object
            var a = new Attachment
                        {
                            Session = this,
                            Document = d,
                            Name = filename,
                            ContentType = contentType,
                            Length = buf.Length,
                            Stub = true,
                        };
            var attachments = (withattachments.Attachments ?? (withattachments.Attachments = new Attachments()));
            attachments[filename] = a;

            return d;
        }
    }
}
