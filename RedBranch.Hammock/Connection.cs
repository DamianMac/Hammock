using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Newtonsoft.Json;

namespace RedBranch.Hammock
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
                if (e.Response != null)
                {
                    using (var reader = new JsonTextReader(new StreamReader(e.Response.GetResponseStream())))
                    {
                        var serializer = new JsonSerializer();
                        var error = (__CouchError) serializer.Deserialize(reader, typeof (__CouchError));
                        throw new CouchException((int) ((HttpWebResponse) e.Response).StatusCode, error.error,
                                                 error.reason);
                    }
                }
                else
                {
                    throw;   
                }
            }
        }
    }

    public class CouchException : Exception
    {
        public int Status { get; private set; }
        public string Error { get; private set; }

        public CouchException(int status, string error, string reason)
            : base(reason)
        {
            Status = status;
            Error = error;
        }
    }

    public class Connection
    {
        public Connection(Uri location)
        {
            this.Location = location;
        }

        public Uri Location { get; private set; }
        public string Version { get; set; }

        private Dictionary<string, List<Session>> _sessions = new Dictionary<string, List<Session>>();

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
                return (string[])serializer.Deserialize(reader, typeof(string[]));
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
            // give a very short pause here, as there are some file locking issues
            // in windows where the delete goes through before a previous file lock
            // is released.
            // http://issues.apache.org/jira/browse/COUCHDB-326
            if (Environment.OSVersion.Platform == PlatformID.Win32NT ||
                Environment.OSVersion.Platform == PlatformID.Win32Windows)
            {
                System.Threading.Thread.Sleep(100);
            }

            var request = (HttpWebRequest)WebRequest.Create(GetDatabaseLocation(database));
            request.Method = "DELETE";
            var response = request.GetCouchResponse();
        }

        public Session CreateSession(string database)
        {
            lock (_sessions)
            {
                if (_sessions.ContainsKey(database) &&
                    _sessions[database].Count > 0)
                {
                    var s = _sessions[database][0];
                    _sessions[database].RemoveAt(0);
                    return s;
                }
            }
            return new Session(this, database);
        }

        public void ReturnSession(Session sx)
        {
            if (sx.Connection != this)
            {
                throw new ArgumentException("The Session being returned was not created by this connection. Please using the session Lock() api to manage Session lifecycle.");
            }
            lock (_sessions)
            {
                if (!_sessions.ContainsKey(sx.Database))
                {
                    _sessions[sx.Database] = new List<Session>();
                }
                if (_sessions[sx.Database].Contains(sx))
                {
                    throw new InvalidOperationException("The Session has already been returned the Session Pool. This is an indication that your Lock() usage is incorrect.");
                }
                _sessions[sx.Database].Add(sx);
                sx.Reset();
            }
        }
    }
}
