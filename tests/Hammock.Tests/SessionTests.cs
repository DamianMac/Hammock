// 
//  SessionTests.cs
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
using Hammock.Design;

namespace Hammock.Tests
{
    
    public class SessionTests : DatabaseTestFixture
    {
        
        public class Widget
        {
            public string Name { get; set; }
            public string[] Tags { get; set; }
        }

        public class Doodad
        {
            public string Name { get; set; }
        }

        
        public SessionTests()
        {

            // create an initial document on a seperate session
            var x = _cx.CreateSession(_sx.Database);
            var w = new Widget {Name = "gizmo", Tags = new[] {"whizbang", "geegollie"}};
            _doc = x.Save(w);
        }


        [Fact]
        public void Can_list_documents()
        {
            var ids = _sx.ListDocuments();
            Assert.NotNull(ids);
        }

        [Fact]
        public void Can_create_entity()
        {
            var w = new Widget {Name = "sproket", Tags = new[] {"big", "small"}};
            var doc = _sx.Save(w);
            Assert.True(_sx.ListDocuments().Any(x => x.Id == doc.Id && x.Revision == doc.Revision));
        }

        [Fact]
        public void Can_delete_entity()
        {
            var w = new Widget { Name = "sproket", Tags = new[] { "big", "small" } };
            var doc = _sx.Save(w);
            _sx.Delete(w);
            Assert.False(_sx.ListDocuments().Any(x => x.Id == doc.Id && x.Revision == doc.Revision));
        }

        [Fact]
        public void Can_update_entity_after_creating_it()
        {
            var w = new Widget { Name = "sproket", Tags = new[] { "big", "small" } };
            var doc = _sx.Save(w);
            var doc2 = _sx.Save(w);
            Assert.Equal(doc.Id, doc2.Id);
            Assert.NotEqual(doc.Revision, doc2.Revision);
        }
        
        [Fact]
        public void Can_load_entity()
        {
            var w = _sx.Load<Widget>(_doc.Id);
            Assert.Equal("gizmo", w.Name);
        }

        [Fact]
        public void Can_update_loaded_entity()
        {
            var x = _sx.ListDocuments();
            var w = _sx.Load<Widget>(_doc.Id);
            w.Name = new string(w.Name.Reverse().ToArray());
            _sx.Save(w);

            var y = _sx.ListDocuments();
            Assert.Equal(x.Count, y.Count);
            Assert.NotEqual(
                x.First(z => z.Id == _doc.Id).Revision,
                y.First(z => z.Id == _doc.Id).Revision
            );
        }

        [Fact]
        public void Cannot_load_entity_with_wrong_generic_argument()
        {
            _sx.Load<Widget>(_doc.Id);
            Assert.Throws<InvalidCastException>(() => _sx.Load<Doodad>(_doc.Id));
        }

        [Fact]
        public void Session_can_save_design_document()
        {
            var d = new DesignDocument { Language = "javascript" };
            _sx.Save(d, "_design/foo");
            Assert.True(_sx.ListDocuments().Any(x => x.Id == "_design/foo"));
        }

        [Fact]
        public void Session_can_load_design_document()
        {
            _cx.CreateSession(_sx.Database).Save(
                new DesignDocument { Language = "javascript", },
                "_design/bar"
            );

            var d = _sx.Load<DesignDocument>("_design/bar");

            Assert.NotNull(d);
        }

        [Fact]
        public void Session_can_delete_design_document()
        {
            _cx.CreateSession(_sx.Database).Save(
                new DesignDocument { Language = "javascript", },
                "_design/baz"
            );

            var d = _sx.Load<DesignDocument>("_design/baz");
            _sx.Delete(d);

            Assert.False(_sx.ListDocuments().Any(x => x.Id == "_design/baz"));
        }

        [Fact]
        public void Session_can_be_reset()
        {
            var s = _cx.CreateSession(_sx.Database);
            var w = new Widget {Name = "wingnut"};
            s.Save(w);
            s.Reset();
            Assert.Throws<IndexOutOfRangeException>(() => s.Delete(w));
        }

        [Fact]
        public void Session_preserves_design_document_when_reset()
        {
            var s = _cx.CreateSession(_sx.Database);
            var d = new DesignDocument {Language = "javascript"};
            s.Save(d, "_design/bang");
            s.Reset();
            var e = s.Load<DesignDocument>("_design/bang");
            Assert.Same(d, e);
        }

        [Fact]
        public void Session_returns_itself_to_connection_when_all_locks_disposed()
        {
            var s = _cx.CreateSession(_sx.Database);
            using (s.Lock())
            {
                using (s.Lock())
                {
                }
                var t = _cx.CreateSession(_sx.Database);
                Assert.NotSame(s, t);
            }
            var u = _cx.CreateSession(_sx.Database);
            Assert.Same(s, u);
        }

        public class DocumentSubclass : Document
        {
            public string Name { get; set; }
        }

        public class IHasDocumentImplementation : IHasDocument
        {
            public Document Document { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public void Session_uses_id_when_saving_document_subclassed_entities()
        {
            // http://code.google.com/p/relax-net/issues/detail?id=7
            var s = _cx.CreateSession(_sx.Database);
            var x = new DocumentSubclass() {Name = "foo", Id = "foo-document-subclass"};
            s.Save(x);
            var y = s.Load<DocumentSubclass>("foo-document-subclass");
            Assert.Same(x, y);
        }

        [Fact]
        public void Session_fills_id_and_revision_when_saving_document_subclassed_entities()
        {
            // http://code.google.com/p/relax-net/issues/detail?id=7
            var s = _cx.CreateSession(_sx.Database);
            var x = new DocumentSubclass();
            s.Save(x);
            Assert.NotEmpty(x.Id);
            Assert.NotEmpty(x.Revision);
        }

        [Fact]
        public void Session_fills_id_and_revision_when_loading_document_subclassed_entities()
        {
            // http://code.google.com/p/relax-net/issues/detail?id=7
            var s = _cx.CreateSession(_sx.Database);
            var x = new DocumentSubclass();
            s.Save(x);

            var t = _cx.CreateSession(_sx.Database);
            var y = t.Load<DocumentSubclass>(x.Id);

            Assert.Equal(x.Id, y.Id);
            Assert.Equal(x.Revision, y.Revision);
        }

        [Fact]
        public void Session_uses_id_when_saving_ihasdocument_implentations()
        {
            // http://code.google.com/p/relax-net/issues/detail?id=7
            var s = _cx.CreateSession(_sx.Database);
            var x = new IHasDocumentImplementation()
            {
                Name = "bar",
                Document = new Document { Id = "bar-document-subclass" }
            };
            s.Save(x);
            var y = s.Load<IHasDocumentImplementation>("bar-document-subclass");
            Assert.Same(x, y);
        }

        [Fact]
        public void Session_fills_document_property_when_saving_entities_that_implement_ihasdocument()
        {
            // http://code.google.com/p/relax-net/issues/detail?id=7
            var s = _cx.CreateSession(_sx.Database);
            var x = new IHasDocumentImplementation();
            s.Save(x);
            Assert.NotEmpty(x.Document.Id);
            Assert.NotEmpty(x.Document.Revision);
        }

        [Fact]
        public void Session_fills_document_property_when_loading_entities_that_implement_ihasdocument()
        {
            // http://code.google.com/p/relax-net/issues/detail?id=7
            var s = _cx.CreateSession(_sx.Database);
            var x = new IHasDocumentImplementation();
            s.Save(x);

            var t = _cx.CreateSession(_sx.Database);
            var y = t.Load<IHasDocumentImplementation>(x.Document.Id);

            Assert.Equal(x.Document.Id, y.Document.Id);
            Assert.Equal(x.Document.Revision, y.Document.Revision);
        }
    }
}
