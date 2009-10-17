using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace Relax
{
    public static class __EnumerableExtensions
    {
        public static IEnumerable<T> Each<T>(this IEnumerable<T> e, Action<T> a)
        {
            foreach (var x in e) a(x);
            return e;
        }
    }

    static class __HttpWebRequestExtensions
    {
        private class __CouchError
        {
            public string error { get; set; }
            public string reason { get; set; }
        }

        public static JsonReader GetCouchResponse(this HttpWebRequest request)
        {
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                return new JsonTextReader(new StreamReader(response.GetResponseStream()));
            }
            catch (WebException e)
            {
                using (var reader = new JsonTextReader(new StreamReader(e.Response.GetResponseStream())))
                {
                    var serializer = new JsonSerializer();
                    var error = (__CouchError)serializer.Deserialize(reader, typeof (__CouchError));
                    throw new CouchException((int)((HttpWebResponse)e.Response).StatusCode, error.error, error.reason); 
                }
            }
        }
    }

    public class CouchException : Exception
    {
        public int Status { get; private set; }
        public string Error { get; private set; }

        public CouchException(int status, string error, string reason) : base(reason)
        {
            Status = status;
            Error = error;
        }
    }

    public class Connection
    {
        public Uri Location { get; set; }
        public string Version { get; set; }

        public string GetDatabaseLocation(string database)
        {
            InvalidDatabaseNameException.Validate(database);
            var location = new Uri(Location, database.Replace("/", "%2F")).OriginalString;
            return location.EndsWith("/") ? location : location + "/";
        }

        public string[] ListDatabases()
        {
            var u = new Uri(Location, "_all_dbs");
            var request = (HttpWebRequest)WebRequest.Create(u);
            using (var reader = request.GetCouchResponse())
            {
                var serializer = new JsonSerializer();
                return (string[])serializer.Deserialize(reader, typeof (string[]));
            }
        }

        public void CreateDatabase(string database)
        {
            var request = (HttpWebRequest)WebRequest.Create(GetDatabaseLocation(database));
            request.Method = "PUT";
            var response = request.GetCouchResponse();
        }

        public void DeleteDatabase(string database)
        {
            var request = (HttpWebRequest)WebRequest.Create(GetDatabaseLocation(database));
            request.Method = "DELETE";
            var response = request.GetCouchResponse();
        }

        public Session CreateSession(string database)
        {
            return new Session(this, database);
        }
    }
}
