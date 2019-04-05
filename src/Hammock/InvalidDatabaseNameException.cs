// 
//  InvalidDatabaseNameException.cs
//  
//  Author:
//       Nick Nystrom <nnystrom@gmail.com>
//       Eddie Dillon <eddie.d.2000@gmail.com>
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

namespace RedBranch.Hammock
{
    public class InvalidDatabaseNameException : Exception
    {
        private static char[] ValidSymbols = new[] {'_','$','(',')','+','-','/'};

        public string Database { get; private set; }

        private InvalidDatabaseNameException(string database, string message)
            : base(message)
        {
            Database = database;
        }

        public static void Validate(string database)
        {
            if ("_replicator" == database)
                return;

            if (null == database)
            {
                throw new ArgumentNullException("database", "Database name argument must not be null.");
            }
            if (string.Empty == database)
            {
                throw new InvalidDatabaseNameException(database, "Database name must not be empty.");
            }
            if (database.Any(x => char.IsUpper(x)))
            {
                throw new InvalidDatabaseNameException(database, "Database name must not contain uppercase letters (http://wiki.apache.org/couchdb/HTTP_database_API).");
            }
            if (!char.IsLetter(database[0]))
            {
                throw new InvalidDatabaseNameException(database, "Database name must begin with a lowercase letter (http://wiki.apache.org/couchdb/HTTP_database_API).");
            }
            if (database.Any(x => !char.IsLetterOrDigit(x) && !ValidSymbols.Contains(x)))
            {
                throw new InvalidDatabaseNameException(database, "Database name contains invalid symbols (http://wiki.apache.org/couchdb/HTTP_database_API).");
            }
        }
    }
}
