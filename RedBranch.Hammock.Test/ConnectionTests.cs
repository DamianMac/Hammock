using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace RedBranch.Hammock.Test
{
    [TestFixture]
    public class ConnectionTests
    {
        public static Connection CreateConnection()
        {
            return new CouchProcess(new Uri("http://localhost:5984")).Connect();    
        }

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            var c = CreateConnection();
            c.ListDatabases().Where(x => x.StartsWith("relax-can-"))
                             .Each(x => c.DeleteDatabase(x));
            c.CreateDatabase("relax-can-delete-database");
        }

        [TestFixtureTearDown]
        public void FixtureTeardown()
        {
            var c = CreateConnection();
            c.ListDatabases().Where(x => x.StartsWith("relax-can-"))
                             .Each(x => c.DeleteDatabase(x));
        }

        [Test]
        public void Slashes_in_database_names_must_be_escaped()
        {
            Assert.AreEqual(
                "http://localhost:5984/forward%2Fslash/",
                CreateConnection().GetDatabaseLocation("forward/slash")
            );
        }

        [Test]
        public void Connection_can_list_databases()
        {
            Assert.IsNotEmpty(CreateConnection().ListDatabases());
        }

        [Test]
        public void Connection_can_create_database()
        {
            var c = CreateConnection();
            c.CreateDatabase("relax-can-create-database");
            Assert.IsTrue(c.ListDatabases().Contains("relax-can-create-database"));
        }

        [Test]
        public void Connection_can_delete_database()
        {
            var c = CreateConnection();
            c.DeleteDatabase("relax-can-delete-database");
            Assert.IsFalse(c.ListDatabases().Contains("relax-can-delete-database"));
        }

        [Test]
        public void Connection_can_create_Session()
        {
            var c = CreateConnection();
            var s = c.CreateSession("relax-can-create-session");
            Assert.IsNotNull(s);
            Assert.AreSame(c, s.Connection);
            Assert.AreEqual("relax-can-create-session", s.Database);
        }

        [Test]
        public void Conection_can_reuse_session()
        {
            var c = CreateConnection();
            var s = c.CreateSession("relax-can-create-session");
            c.ReturnSession(s);
            var s2 = c.CreateSession("relax-can-create-session");
            
            Assert.That(s2, Is.SameAs(s));
        }
    }
}
