using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Relax.Test
{
    [TestFixture]
    public class RepositoryTests
    {
        public class Widget
        {
            public string Name { get; set; }
            public string Manufacturer { get; set; }
            public decimal Cost { get; set; }
        }

        private Connection _cx;
        private Session _sx;
        private Session _sx2;

        [TestFixtureSetUp]
        public void __setup()
        {
            _cx = ConnectionTests.CreateConnection();
            if (_cx.ListDatabases().Contains("relax-repository-tests"))
            {
                _cx.DeleteDatabase("relax-repository-tests");
            }
            _cx.CreateDatabase("relax-repository-tests");
            _sx = _cx.CreateSession("relax-repository-tests");
            _sx2 = _cx.CreateSession("relax-repository-tests");
        }

        [Test]
        public void Can_create_repository()
        {
            var r = new Repository<Widget>(_sx);
            Assert.AreSame(_sx, r.Session);
        }

        [Test]
        public void Repository_can_get_entity()
        {
            var w = new Widget {Name = "gear"};
            var doc = _sx.Save(w);
            var r = new Repository<Widget>(_sx2);
            var w2 = r.Get(doc.Id);

            Assert.AreEqual(w.Name, w2.Name);
        }

        [Test]
        public void Repository_can_delete_entity()
        {
            var w = new Widget { Name = "sprocket" };
            var doc = _sx.Save(w);
            var r = new Repository<Widget>(_sx);
            r.Delete(w);

            Assert.IsFalse(_sx.List().Any(x => x.Id == doc.Id));
        }

        [Test]
        public void Repository_can_save_entity()
        {
            var w = new Widget { Name = "doodad" };
            var r = new Repository<Widget>(_sx);
            var doc = r.Save(w);

            Assert.IsTrue(_sx.List().Any(x => x.Id == doc.Id));
        }
    }
}
