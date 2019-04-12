// 
//  ConnectionTests.cs
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
using Xunit;

namespace Hammock.Tests
{
    
    public class ConnectionTests : IDisposable
    {
        public static Connection CreateConnection()
        {
            const string UriString = "http://couchdb_test:5984";
            return new Connection(new Uri(UriString));
        }

        
        public ConnectionTests()
        {
            var c = CreateConnection();
            c.ListDatabases().Where(x => x.StartsWith("relax-can-"))
                             .Each(x => c.DeleteDatabase(x));
            c.CreateDatabase("relax-can-delete-database");
        }

        public void Dispose()
        {
            var c = CreateConnection();
            c.ListDatabases().Where(x => x.StartsWith("relax-can-"))
                             .Each(x => c.DeleteDatabase(x));
        }

        [Fact]
        public void Slashes_in_database_names_must_be_escaped()
        {
            Assert.Equal(
                "http://localhost:5984/forward%2Fslash/",
                CreateConnection().GetDatabaseLocation("forward/slash")
            );
        }

        [Fact]
        public void Connection_can_list_databases()
        {
            Assert.NotEmpty(CreateConnection().ListDatabases());
        }

        [Fact]
        public void Connection_can_create_database()
        {
            var c = CreateConnection();
            c.CreateDatabase("relax-can-create-database");
            Assert.True(c.ListDatabases().Contains("relax-can-create-database"));
        }

        [Fact]
        public void Connection_can_delete_database()
        {
            var c = CreateConnection();
            c.DeleteDatabase("relax-can-delete-database");
            Assert.False(c.ListDatabases().Contains("relax-can-delete-database"));
        }

        [Fact]
        public void Connection_can_create_Session()
        {
            var c = CreateConnection();
            var s = c.CreateSession("relax-can-create-session");
            Assert.NotNull(s);
            Assert.Same(c, s.Connection);
            Assert.Equal("relax-can-create-session", s.Database);
        }

        [Fact]
        public void Conection_can_reuse_session()
        {
            var c = CreateConnection();
            var s = c.CreateSession("relax-can-create-session");
            c.ReturnSession(s);
            var s2 = c.CreateSession("relax-can-create-session");
            
            Assert.Same(s2, s);
        }
    }
}
