// 
//  RepositoryTests.cs
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Hammock.Design;

namespace Hammock.Test
{
    [TestFixture]
    public class RepositoryTests
    {
        public class Widget
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("mfg")] public string Manufacturer { get; set; }
            public decimal Cost { get; set; }
        }

        public class Gizmo
        {
            public string Name { get; set; }
            public string Manufacturer { get; set; }
            public decimal Cost { get; set; }
        }

        public class Wingding
        {
            
        }

        private Connection _cx;
        private Session _sx;
        private Session _sx2;

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _cx = ConnectionTests.CreateConnection();
            if (_cx.ListDatabases().Contains("relax-repository-tests"))
            {
                _cx.DeleteDatabase("relax-repository-tests");
            }
            _cx.CreateDatabase("relax-repository-tests");
            _sx = _cx.CreateSession("relax-repository-tests");
            _sx2 = _cx.CreateSession("relax-repository-tests");

            _sx.Save(new Gizmo { Name = "Widget", Cost = 30, Manufacturer = "ACME" });
            _sx.Save(new Gizmo { Name = "Gadget", Cost = 30, Manufacturer = "ACME" });
            _sx.Save(new Gizmo { Name = "Foo",    Cost = 35, Manufacturer = "ACME" });
            _sx.Save(new Gizmo { Name = "Bar",    Cost = 35, Manufacturer = "Widgetco" });
            _sx.Save(new Gizmo { Name = "Biz",    Cost = 45, Manufacturer = "Widgetco" });
            _sx.Save(new Gizmo { Name = "Bang",   Cost = 55, Manufacturer = "Widgetco" });

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

            Assert.IsFalse(_sx.ListDocuments().Any(x => x.Id == doc.Id));
        }

        [Test]
        public void Repository_can_save_entity()
        {
            var w = new Widget { Name = "doodad" };
            var r = new Repository<Widget>(_sx);
            var doc = r.Save(w);

            Assert.IsTrue(_sx.ListDocuments().Any(x => x.Id == doc.Id));
        }

        [Test]
        public void Repository_can_create_queries_from_design()
        {
            var design = new DesignDocument
            {
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
            };
            var r = new Repository<Widget>(_sx, design);

            Assert.IsNotNull(r.Queries["all-widgets"]);
            Assert.IsNotNull(r.Queries["all-manufacturers"]);
            Assert.AreEqual("widget", r.Queries["all-manufacturers"].Design);
            Assert.IsTrue(r.Queries["all-manufacturers"].Group);
        }

        [Test]
        public void Repository_loads_design_document_from_session()
        {
            DesignDocument design = null;
            if (_sx.IsEnrolled("_design/widget"))
            {
                design = _sx.Load<DesignDocument>("_design/widget");
            }
            else
            {
                design = new DesignDocument
                 {
                     Language = "javascript", 
                 };
            }
            design.Views = new Dictionary<string, View>
             {
                { "all-widgets", new View {
                    Map = @"function(doc) { emit(null, null); }"
                }},
                { "all-manufacturers", new View() {
                    Map = @"function(doc) { emit(doc.Manufacturer, 1); }",
                    Reduce = @"function(keys, values, rereduce) { return sum(values); }"
                }}
             };
            _sx.Save(design, "_design/widget");

            var r = new Repository<Widget>(_sx);
            Assert.AreEqual(2, r.Queries.Count);
        }

        [Test]
        public void Repository_creates_design_document_if_not_found()
        {
            new Repository<Wingding>(_sx);
            Assert.That(_sx.IsEnrolled("_design/wingding"));
        }

        [Test]
        public void Repository_can_generate_views()
        {
            var r = new Repository<Widget>(_sx);
            r.Where(x => x.Name).Eq("gadget")
               .And(x => x.Cost).Bw(10,20)
               .List();
            Assert.That(
                _sx.Load<DesignDocument>("_design/widget").Views.ContainsKey(
                "by-name-cost"
            ));
        }

        [Test]
        public void Fields_must_appear_only_once_in_a_generated_view()
        {
            var r = new Repository<Widget>(_sx);
            Assert.Throws<ArgumentException>(() =>
                r.Where(x => x.Name).Eq("gadget")
                   .And(x => x.Cost).Ge(10)
                   .And(x => x.Cost).Le(20));
        }

        [Test]
        public void Repository_can_query_by_equality()
        {
            var r = new Repository<Gizmo>(_sx);
            var z = r.Where(x => x.Manufacturer).Eq("Widgetco").List();
            Assert.That(z.Rows.Length, Is.EqualTo(3));
        }

        [Test]
        public void Repository_can_query_by_greater_than_or_equal_to()
        {
            var r = new Repository<Gizmo>(_sx);
            var z = r.Where(x => x.Cost).Ge(35).List();
            Assert.That(z.Rows.Length, Is.EqualTo(4));
        }

        [Test]
        public void Repository_can_query_by_less_than_or_equal_to()
        {
            var r = new Repository<Gizmo>(_sx);
            var z = r.Where(x => x.Cost).Le(35).List();
            Assert.That(z.Rows.Length, Is.EqualTo(4));
        }

        [Test]
        public void Repository_can_query_by_between()
        {
            var r = new Repository<Gizmo>(_sx);
            var z = r.Where(x => x.Cost).Bw(35, 45).List();
            Assert.That(z.Rows.Length, Is.EqualTo(3));
        }

        [Test]
        public void Repository_can_with_and()
        {
            var r = new Repository<Gizmo>(_sx);
            var z = r.Where(x => x.Manufacturer).Eq("ACME")
                     .And(x => x.Cost).Bw(35, 45)
                     .List();
            Assert.That(z.Rows.Length, Is.EqualTo(1));
        }

        [Test]
        public void Repository_can_list_all_entities()
        {
            var r = new Repository<Gizmo>(_sx);
            var all = r.All();
            Assert.That(all.Count(), Is.EqualTo(6));
        }

        [Test]
        public void Repository_can_query_by_like()
        {
            var r = new Repository<Gizmo>(_sx);
            var z = r.Where(x => x.Name).Like("dget").List();
            Assert.That(z.Rows.Length, Is.EqualTo(2));
        }

        [Test]
        public void Repository_query_like_must_appear_only_once()
        {
            var r = new Repository<Gizmo>(_sx);
            Assert.Throws<InvalidOperationException>(() => {
                var z = r.Where(x => x.Name).Like("dget").And(x => x.Manufacturer).Like("foo").List();
            });
        }

        [Test]
        public void Repository_query_can_return_values()
        {
            var r = new Repository<Gizmo>(_sx);
            var z = r.Where(x => x.Cost).Le(35).Returns(x => x.Name).List();
            Assert.That(z.Rows.All(x => x.Value != null), Is.True);
        }

        [Test]
        public void Repository_query_can_return_multiple_values()
        {
            var r = new Repository<Gizmo>(_sx);
            var z = r
                .Where(x => x.Cost).Le(35)
                .Returns(x => x.Name)
                .Returns(x => x.Cost)
                .List();
            Assert.That(z.Rows.All(x => (x.Value as JArray) != null), Is.True);
        }

        public class GizmoRepository : Repository<Gizmo>
        {
            public GizmoRepository(Session sx) : base(sx) {}

            public Query<Gizmo> CustomView(string name)
            {
                return WithView(
                    name,
                    View.BasicMap<Gizmo>("emit(null, null);")
                );
            }

            public Query<Gizmo> CustomView2(string name)
            {
                return WithView(
                    name,
                    View.BasicMap<Gizmo>("emit(doc._id, doc._id);")
                );
            }
        }

        [Test]
        public void Repository_can_create_custom_view()
        {
            var r = new GizmoRepository(_sx);
            r.CustomView("CustomView0");

            Assert.That(r.Design.Views.ContainsKey("CustomView0"));
        }

        [Test]
        public void Repository_can_update_existing_custom_view()
        {
            var r = new GizmoRepository(_sx);
            r.CustomView("CustomView0");
            r.CustomView2("CustomView0");

            Assert.That(r.Design.Views["CustomView0"].Map.Contains("emit(doc._id, doc._id);"));
        }
    }
}
