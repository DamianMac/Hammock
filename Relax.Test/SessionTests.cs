using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using Relax.Design;

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
            //_cx.DeleteDatabase("relax-session-tests");
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
        public void Cannot_load_document_with_wrong_generic_argument()
        {
            var w = _sx.Load<Widget>(_doc.Id);

            Assert.Throws<InvalidCastException>(() => _sx.Load<Doodad>(_doc.Id));
        }

        [Test]
        public void Session_can_save_design_document()
        {
            var d = new DesignDocument { Language = "javascript" };
            _sx.Save(d, "_design/foo");
            Assert.True(_sx.List().Any(x => x.Id == "_design/foo"));
        }

        [Test]
        public void Session_can_load_design_document()
        {
            _cx.CreateSession(_sx.Database).Save(
                new DesignDocument { Language = "javascript", },
                "_design/bar"
            );

            var d = _sx.Load<DesignDocument>("_design/bar");

            Assert.IsNotNull(d);
        }

        [Test]
        public void Session_can_delete_design_document()
        {
            _cx.CreateSession(_sx.Database).Save(
                new DesignDocument { Language = "javascript", },
                "_design/baz"
            );

            var d = _sx.Load<DesignDocument>("_design/baz");
            _sx.Delete(d);

            Assert.IsFalse(_sx.List().Any(x => x.Id == "_design/baz"));
        }
    }
}
