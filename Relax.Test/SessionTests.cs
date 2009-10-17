using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Relax.Test
{
    [TestFixture]
    public class SessionTests
    {
        private Connection _cx;
        private Session _sx;
        private Document _doc;

        public class Widget
        {
            public string Name { get; set; }
            public string[] Tags { get; set; }
        }

        public class Doodad
        {
            public string Name { get; set; }
        }

        [TestFixtureSetUp]
        public void __setup()
        {
            _cx = ConnectionTests.CreateConnection();
            if (_cx.ListDatabases().Contains("relax-session-tests"))
            {
                _cx.DeleteDatabase("relax-session-tests");
            }
            _cx.CreateDatabase("relax-session-tests");
            _sx = _cx.CreateSession("relax-session-tests");

            // create an initial document on a seperate session
            var x = _cx.CreateSession(_sx.Database);
            var w = new Widget {Name = "gizmo", Tags = new[] {"whizbang", "geegollie"}};
            _doc = x.Save(w);
        }

        [TestFixtureTearDown]
        public void __teardown()
        {
            _cx.DeleteDatabase("relax-session-tests");
        }

        [Test]
        public void Can_list_documents()
        {
            var ids = _sx.List();
            Assert.IsNotNull(ids);
        }

        [Test]
        public void Can_create_document()
        {
            var w = new Widget {Name = "sproket", Tags = new[] {"big", "small"}};
            var doc = _sx.Save(w);
            Assert.IsTrue(_sx.List().Any(x => x.Id == doc.Id && x.Revision == doc.Revision));
        }

        [Test]
        public void Can_delete_document()
        {
            var w = new Widget { Name = "sproket", Tags = new[] { "big", "small" } };
            var doc = _sx.Save(w);
            _sx.Delete(w);
            Assert.IsFalse(_sx.List().Any(x => x.Id == doc.Id && x.Revision == doc.Revision));
        }

        [Test]
        public void Can_update_document_after_creating_it()
        {
            var w = new Widget { Name = "sproket", Tags = new[] { "big", "small" } };
            var doc = _sx.Save(w);
            var doc2 = _sx.Save(w);
            Assert.AreEqual(doc.Id, doc2.Id);
            Assert.AreNotEqual(doc.Revision, doc2.Revision);
        }
        
        [Test]
        public void Can_load_document()
        {
            var w = _sx.Load<Widget>(_doc.Id);
            Assert.AreEqual("gizmo", w.Name);
        }

        [Test]
        public void Can_update_loaded_document()
        {
            var x = _sx.List();
            var w = _sx.Load<Widget>(_doc.Id);
            w.Name = new string(w.Name.Reverse().ToArray());
            _sx.Save(w);

            var y = _sx.List();
            Assert.AreEqual(x.Count, y.Count);
            Assert.AreNotEqual(
                x.First(z => z.Id == _doc.Id).Revision,
                y.First(z => z.Id == _doc.Id).Revision
            );
        }

        [Test]
        [ExpectedException(typeof(InvalidCastException))]
        public void Cannot_load_document_with_wrong_generic_argument()
        {
            var w = _sx.Load<Widget>(_doc.Id);
            var whoops = _sx.Load<Doodad>(_doc.Id);
        }
    }
}
