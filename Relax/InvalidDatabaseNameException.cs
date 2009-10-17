using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Relax
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
