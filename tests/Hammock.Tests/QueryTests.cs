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
using Xunit;
using Hammock.Design;
namespace Hammock.Tests
{
    
    public class QueryTests : DatabaseTestFixture
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

        public QueryTests()
        {
      
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

        [Fact]
        public void Can_execute_query()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");
            var r = q.All().Execute();

            Assert.Equal(r.Total, 3);
        }

        [Fact]
        public void Can_execute_query_with_keys_and_values()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-manufacturers", true);
            var r = q.All().Execute();

            Assert.Equal(2, r.Total);
            Assert.NotNull(r.Rows[0].Key);
            Assert.NotNull(r.Rows[1].Value);
        }

        [Fact]
        public void Can_execute_query_with_result_limit()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");
            var r = q.Limit(2).Execute();

            Assert.Equal(r.Total, 3);
            Assert.Equal(r.Rows.Length, 2);
        } 

        [Fact]
        public void Can_page_through_results()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");
            
            var r = q.Limit(2).Execute();
            Assert.Equal(3, r.Total);
            Assert.Equal(2, r.Rows.Length);

            r = r.Next();
            Assert.Equal(3, r.Total);
            Assert.Equal(1, r.Rows.Length);

            r = r.Next();
            Assert.Null(r);
        }

        [Fact]
        public void Can_load_through_id()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");
            var r = q.Limit(1).Execute();
            var o = r.Rows.First().Entity;

            Assert.NotNull(o);
        }

        [Fact]
        public void Can_prefetch_documents_and_enroll_doc()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");

            var r0 = q.Limit(1).WithDocuments().Execute();
            var e0 = r0.Rows.First().Entity;

            var e1 = _sx.Load<Widget>(r0.Rows.First().Id);

            Assert.NotNull(e0);
            Assert.Same(e0, e1);
        }

        [Fact]
        public void Can_prefetch_documents_where_doc_is_already_enrolled()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-widgets");

            var r0 = q.Limit(1).Execute();
            var e0 = _sx.Load<Widget>(r0.Rows.First().Id);
            
            var r1 = q.Limit(1).WithDocuments().Execute();
            var e1 = r1.Rows.First().Entity;

            Assert.NotNull(e1);
            Assert.Same(e0, e1);
        }

        [Fact]
        public void Can_prefetch_document_and_fill_ihasdocument()
        {
            var s2 = _cx.CreateSession(_sx.Database);
            var q = new Query<WidgetWithDocument>(s2, "widgets", "all-widgets");

            var r0 = q.Limit(1).WithDocuments().Execute();
            var e0 = r0.Rows.First().Entity;

            Assert.NotNull(e0.Document);
        }

    }
}
