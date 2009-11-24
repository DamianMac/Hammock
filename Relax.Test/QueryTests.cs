using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Relax.Design;

namespace Relax.Test
{
    [TestFixture]
    public class QueryTests : CouchTest
    {
        public class Widget
        {
            public string Name { get; set; }
            public string Manufacturer { get; set; }
            public decimal Cost { get; set; }
        }

        private Connection _cx;
        private Session _sx;

        
        public override void __setup()
        {
            base.__setup();
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
    }
}
