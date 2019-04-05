// 
//  QueryTests.cs
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
using NUnit.Framework;
using Hammock.Design;

namespace Hammock.Test
{
    [TestFixture]
    public class QueryTests
    {
        public class Widget
        {
            public string Name { get; set; }
            public string Manufacturer { get; set; }
            public decimal Cost { get; set; }
        }

        public class WidgetWithDocument : Widget, IHasDocument
        {
            public Document Document { get; set; }
        }

        private Connection _cx;
        private Session _sx;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _cx = ConnectionTests.CreateConnection();
            if (_cx.ListDatabases().Contains("relax-query-tests"))
            {
                _cx.DeleteDatabase("relax-query-tests");
            }
            _cx.CreateDatabase("relax-query-tests");
            _sx = _cx.CreateSession("relax-query-tests");
        
            // populate a few widgets & a simple design doc
            _sx.Save(new Widget { Name = "widget", Manufacturer = "acme" });
            _sx.Save(new Widget { Name = "sprocket", Manufacturer = "acme" });
            _sx.Save(new Widget { Name = "doodad", Manufacturer = "widgetco" });

            _sx.Save(
                new DesignDocument {
                         Language = "javascript",
                         Views = new Dictionary<string, View>
                         {
                            { "all-widgets", new View {
                                Map = @"function(doc) { emit(null, null); }"
                            }},
                            { "all-manufacturers", new View() {
                                Map = @"function(doc) { emit(doc.Manufacturer, 1); }",
                                Reduce = @"function(keys, values, rereduce) { return sum(values); }"
                            }}
                         }
                 },
                 "_design/widgets"
            );      
        }

        [Test]
        public void Can_execute_query()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");
            var r = q.All().Execute();

            Assert.AreEqual(r.Total, 3);
        }

        [Test]
        public void Can_execute_query_with_keys_and_values()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-manufacturers", true);
            var r = q.All().Execute();

            Assert.AreEqual(2, r.Total);
            Assert.IsNotNull(r.Rows[0].Key);
            Assert.IsNotNull(r.Rows[1].Value);
        }

        [Test]
        public void Can_execute_query_with_result_limit()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");
            var r = q.Limit(2).Execute();

            Assert.AreEqual(r.Total, 3);
            Assert.AreEqual(r.Rows.Length, 2);
        } 

        [Test]
        public void Can_page_through_results()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");
            
            var r = q.Limit(2).Execute();
            Assert.AreEqual(3, r.Total);
            Assert.AreEqual(2, r.Rows.Length);

            r = r.Next();
            Assert.AreEqual(3, r.Total);
            Assert.AreEqual(1, r.Rows.Length);

            r = r.Next();
            Assert.IsNull(r);
        }

        [Test]
        public void Can_load_through_id()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");
            var r = q.Limit(1).Execute();
            var o = r.Rows.First().Entity;

            Assert.IsNotNull(o);
        }

        [Test]
        public void Can_prefetch_documents_and_enroll_doc()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");

            var r0 = q.Limit(1).WithDocuments().Execute();
            var e0 = r0.Rows.First().Entity;

            var e1 = _sx.Load<Widget>(r0.Rows.First().Id);

            Assert.IsNotNull(e0);
            Assert.AreSame(e0, e1);
        }

        [Test]
        public void Can_prefetch_documents_where_doc_is_already_enrolled()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");

            var r0 = q.Limit(1).Execute();
            var e0 = _sx.Load<Widget>(r0.Rows.First().Id);
            
            var r1 = q.Limit(1).WithDocuments().Execute();
            var e1 = r1.Rows.First().Entity;

            Assert.IsNotNull(e1);
            Assert.AreSame(e0, e1);
        }

        [Test]
        public void Can_prefetch_document_and_fill_ihasdocument()
        {
            var s2 = _cx.CreateSession("relax-query-tests");
            var q = new Query<WidgetWithDocument>(s2, "widgets", "all-widgets");

            var r0 = q.Limit(1).WithDocuments().Execute();
            var e0 = r0.Rows.First().Entity;

            Assert.That(e0.Document, Is.Not.Null);
        }

    }
}
