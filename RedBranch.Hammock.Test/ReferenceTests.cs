using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using RedBranch.Hammock.Design;

namespace RedBranch.Hammock.Test
{
    [TestFixture]
    public class ReferenceTests
    {
        private Connection _cx;
        private Session _sx;
        private Session _sx2;
        
        public class Widget : Document
        {
            public string Name { get; set; }
            public decimal Cost { get; set; }
        }
        
        public class Gizmo : Document
        {
            public string Name { get; set; }
            public Reference<Widget> Primary { get; set; }
            public IList<Reference<Widget>> Secondary { get; set; }
        }
        
        public class Cyclocycle : Document
        {
            public string Name { get; set; }
            public Reference<Cyclocycle> Whoah { get; set; }
        }
		
		public class Lazypoco
		{
			public string Name { get; set; }
			public Lazy<Lazypoco> Parent { get; set; }
			public IList<Lazy<Lazypoco>> Children { get; set; }
		}

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _cx = ConnectionTests.CreateConnection();
            if (_cx.ListDatabases().Contains("relax-reference-tests"))
            {
                _cx.DeleteDatabase("relax-reference-tests");
            }
            _cx.CreateDatabase("relax-reference-tests");
            _sx = _cx.CreateSession("relax-reference-tests");
            _sx2 = _cx.CreateSession("relax-reference-tests");

            _sx.Save(new Widget { Id = "w1", Name = "Widget", Cost = 30 });
            _sx.Save(new Widget { Id = "w2", Name = "Gadget", Cost = 30 });
            _sx.Save(new Widget { Id = "w3", Name = "Foo",    Cost = 35 });
            _sx.Save(new Widget { Id = "w4", Name = "Bar",    Cost = 35 });
            _sx.Save(new Widget { Id = "w5", Name = "Biz",    Cost = 45 });
            _sx.Save(new Widget { Id = "w6", Name = "Bang",   Cost = 55 });
            
            _sx.SaveRaw(JObject.Parse(
            @"{
                _id: 'g1',
                Name: 'Gadget #1',
                Primary: 'w1'
            }"));
            
            _sx.SaveRaw(JObject.Parse(
            @"{
                _id: 'g2',
                Name: 'Gadget #1',
                Secondary: ['w1', 'w2', 'w3']
            }"));

        }

        [Test]
        public void Lazy_reference_can_be_resolved()
        {
            var r = new Repository<Gizmo>(_sx);
            var g1 = r.Get("g1");
            Assert.That(g1.Primary.Id, Is.EqualTo("w1"));
            Assert.That(g1.Primary.Value.Id, Is.EqualTo(g1.Primary.Id));
        }
        
        [Test]
        public void Lazy_reference_array_can_be_resolved()
        {
            var r = new Repository<Gizmo>(_sx);
            var g2 = r.Get("g2");
            
            Assert.That(g2.Secondary.Sum(x => x.Value.Cost), Is.EqualTo(95));
        }
        
        [Test]
        public void Weak_reference_can_be_written()
        {
            {
                var r = new Repository<Gizmo>(_sx);
                var g3 = new Gizmo
                {
                    Id = "g3",
                    Name = "weeeeaaaak",
                    Primary = Reference.To<Widget>("w6"),
                };
                Assert.DoesNotThrow(() => r.Save(g3));
            }
            {
                var r = new Repository<Gizmo>(_sx2);
                var g3 = r.Get("g3");
                Assert.That(g3.Primary.Value.Cost, Is.EqualTo(55));
            }
        }
        
        [Test]
        public void Weak_reference_cannot_be_resolved()
        {
            var g = new Gizmo
            {
                Id = "gx",
                Name = "weeeeaaaak",
                Primary = Reference.To<Widget>("w6"),
            };
            Assert.Throws<InvalidOperationException>(() => { var x = g.Primary.Value; });
        }
        
        [Test]
        public void Strong_reference_can_be_written()
        {
            {
                var r = new Repository<Gizmo>(_sx);
                var rw = new Repository<Widget>(_sx);
                var w5 = rw.Get("w5");
                var g4 = new Gizmo
                {
                    Id = "g4",
                    Name = "stronglikeukraine",
                    Primary = Reference.To(w5),
                };
                Assert.DoesNotThrow(() => r.Save(g4));
            }
            {
                var r = new Repository<Gizmo>(_sx2);
                var g4 = r.Get("g4");
                Assert.That(g4.Primary.Value.Cost, Is.EqualTo(45));
            }
        }
        
        [Test]
        public void Strong_reference_value_must_have_id_before_save()
        {
            var r = new Repository<Gizmo>(_sx);
            var g5 = new Gizmo
            {
                Id = "g5",
                Name = "stronglikewho?",
                Primary = Reference.To(new Widget { Name = "name, but no id" }),
            };
            Assert.Throws<InvalidOperationException>(() => r.Save(g5));
        }
        
        [Test]
        public void Cyclical_references_are_supported()
        {
            {
                var r = new Repository<Cyclocycle>(_sx);
                var c1 = new Cyclocycle { Id = "c1", Name = "Cycle1" };
                var c2 = new Cyclocycle { Id = "c2", Name = "Cycle2", Whoah = Reference.To(c1) };
                r.Save(c1);
                r.Save(c2);
                c1.Whoah = Reference.To(c2);
                r.Save(c1);
            }
            {
                var r = new Repository<Cyclocycle>(_sx);
                var c1 = r.Get("c1");
                var c2 = c1.Whoah.Value;
                var c1a = c2.Whoah.Value;   
                var c2a = c1a.Whoah.Value;
                Assert.That(c1a, Is.SameAs(c1));
                Assert.That(c2a, Is.SameAs(c2));
            }
        }
		
		[Test]
		public void Poco_with_lazy1_can_be_written()
		{
			var a = new Lazypoco { Name = "foo" }; 
			var Da = _sx.Save(a);
			
			var b = new Lazypoco { Name = "bar", Parent = new Lazy<Lazypoco>(() => a) };
			var Db = _sx.Save(b);
			
			var c = _sx2.Load<Lazypoco>(Db.Id);
			Assert.That(a.Name, Is.EqualTo(c.Parent.Value.Name));
		}
    }
}

