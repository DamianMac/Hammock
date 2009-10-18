using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Relax.Design;

namespace Relax.Test
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

        private Connection _cx;
        private Session _sx;

        [TestFixtureSetUp]
        public void __setup()
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
            var r = q.Execute();

            Assert.AreEqual(r.Total, 3);
        }

        [Test]
        public void Can_execute_query_with_keys_and_values()
        {
            var q = new Query<Widget>(_sx, "widgets", "all-manufacturers");
            var r = q.Execute();

            Assert.AreEqual(r.Total, 3);
            Assert.IsNotNull(r.Rows[0].Key);
            Assert.IsNotNull(r.Rows[1].Value);
        }

        [Test]
        public void Can_execute_query_with_result_limit()
        {
            
        }

    }
}
