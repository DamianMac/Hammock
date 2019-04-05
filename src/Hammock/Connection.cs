// 
//  Connection.cs
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
                        throw CouchException.CreateException(
                            request.RequestUri.ToString(),
                            (int) ((HttpWebResponse) e.Response).StatusCode,
                            error.error,
                            error.reason);
                    }
                }
                throw;
            }
        }
    }

    public class CouchException : Exception
    {
        public string Url { get; private set; }
        public int Status { get; private set; }
        public string Reason { get; private set; }

        public static CouchException CreateException(string url, int status, string error, string reason)
        {
            if (status == 404)
            {
                return new CouchException(url, status, "The resource could not be found (404).", reason);
            }
            return new CouchException(url, status, error, reason);
        }

        public CouchException(string url, int status, string error, string reason)
            : base(error)
        {
            Url = url;
            Status = status;
            Reason = reason;
        }
    }

    public class Connection
    {
        public Connection(Uri location)
        {
            Location = location;
        }

        public Uri Location { get; private set; }
        public string Version { get; set; }

        private Dictionary<string, List<Session>> _sessions = new Dictionary<string, List<Session>>();
        private List<Func<Connection, IObserver>> _observerFactories;
        private string[] _databases;

        public ICollection<Func<Connection, IObserver>> Observers
        {
            get { return _observerFactories ?? (_observerFactories = new List<Func<Connection, IObserver>>()); }
        }

        public string GetDatabaseLocation(string database)
        {
            InvalidDatabaseNameException.Validate(database);
            var uri = new Uri(Location, database);
            var location = uri.OriginalString;
            if (database.Contains("/") &&
                location.EndsWith(database))
            {
                var escaped = database.Replace("/", "%2F");
                location = location.Substring(0, location.Length - database.Length) + escaped; 
            }
            return location.EndsWith("/") ? location : location + "/";
        }

        public string[] ListDatabases()
        {
            return ListDatabases(false);
        }

        public string[] ListDatabases(bool cached)
        {
            if (cached && null != _databases)
            {
                return _databases;
            }

            var u = new Uri(Location, "_all_dbs");
            var request = (HttpWebRequest)WebRequest.Create(u);
            using (var reader = request.GetCouchResponse())
            {
                var serializer = new JsonSerializer();
                _databases = (string[])serializer.Deserialize(reader, typeof(string[]));
            }
            return _databases;
        }

        public void CreateDatabase(string database)
        {
            var request = (HttpWebRequest)WebRequest.Create(GetDatabaseLocation(database));
            request.Method = "PUT";
            request.GetCouchResponse().Close();
            _databases = null;
        }

        public void DeleteDatabase(string database)
        {
            var request = (HttpWebRequest)WebRequest.Create(GetDatabaseLocation(database));
            request.Method = "DELETE";
            request.GetCouchResponse().Close();
            _databases = null;
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
            var sx = new Session(this, database);
            if (null != _observerFactories && _observerFactories.Count > 0)
            {
                _observerFactories.Each(x => sx.Observers.Add(x(this)));
            }
            return sx;
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
