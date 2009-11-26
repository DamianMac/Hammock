using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using RedBranch.Hammock.Design;

namespace RedBranch.Hammock.Test
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
        public void FixtureSetup()
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

        [Test]
        public void Can_list_documents()
        {
            var ids = _sx.List();
            Assert.IsNotNull(ids);
        }

        [Test]
        public void Can_create_entity()
        {
            var w = new Widget {Name = "sproket", Tags = new[] {"big", "small"}};
            var doc = _sx.Save(w);
            Assert.IsTrue(_sx.List().Any(x => x.Id == doc.Id && x.Revision == doc.Revision));
        }

        [Test]
        public void Can_delete_entity()
        {
            var w = new Widget { Name = "sproket", Tags = new[] { "big", "small" } };
            var doc = _sx.Save(w);
            _sx.Delete(w);
            Assert.IsFalse(_sx.List().Any(x => x.Id == doc.Id && x.Revision == doc.Revision));
        }

        [Test]
        public void Can_update_entity_after_creating_it()
        {
            var w = new Widget { Name = "sproket", Tags = new[] { "big", "small" } };
            var doc = _sx.Save(w);
            var doc2 = _sx.Save(w);
            Assert.AreEqual(doc.Id, doc2.Id);
            Assert.AreNotEqual(doc.Revision, doc2.Revision);
        }
        
        [Test]
        public void Can_load_entity()
        {
            var w = _sx.Load<Widget>(_doc.Id);
            Assert.AreEqual("gizmo", w.Name);
        }

        [Test]
        public void Can_update_loaded_entity()
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
        public void Cannot_load_entity_with_wrong_generic_argument()
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

        [Test]
        public void Session_can_be_reset()
        {
            var s = _cx.CreateSession(_sx.Database);
            var w = new Widget {Name = "wingnut"};
            s.Save(w);
            s.Reset();
            Assert.That(() => s.Delete(w), Throws.InstanceOf<Exception>());
        }

        [Test]
        public void Session_preserves_design_document_when_reset()
        {
            var s = _cx.CreateSession(_sx.Database);
            var d = new DesignDocument {Language = "javascript"};
            s.Save(d, "_design/bang");
            s.Reset();
            var e = s.Load<DesignDocument>("_design/bang");
            Assert.That(e, Is.SameAs(d));
        }

        [Test]
        public void Session_returns_itself_to_connection_when_all_locks_disposed()
        {
            var s = _cx.CreateSession(_sx.Database);
            using (s.Lock())
            {
                using (s.Lock())
                {
                }
                var t = _cx.CreateSession(_sx.Database);
                Assert.That(t, Is.Not.SameAs(s));
            }
            var u = _cx.CreateSession(_sx.Database);
            Assert.That(u, Is.SameAs(s));
        }

    }
}
