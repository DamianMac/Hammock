using System;
using System.Linq;

namespace Hammock.Tests
{
    public abstract class DatabaseTestFixture : IDisposable
    {

        internal Connection _cx { get; set; }
        internal Session _sx { get; set; }
        internal Document _doc { get; set; }

        private string databaseName;

        public DatabaseTestFixture()
        {
            databaseName = "db_" + Guid.NewGuid().ToString();

            _cx = ConnectionTests.CreateConnection();
            _cx.CreateDatabase(databaseName);
            _sx = _cx.CreateSession(databaseName);

        }

        public void Dispose()
        {
            if (_cx.ListDatabases().Contains(databaseName))
            {
                _cx.DeleteDatabase(databaseName);
            }
        }
    }
}