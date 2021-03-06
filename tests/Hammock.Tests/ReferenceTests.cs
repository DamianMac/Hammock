// 
//  ReferenceTests.cs
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
using Xunit;
using Hammock.Design;

namespace Hammock.Tests
{
    
    public class ReferenceTests : DatabaseTestFixture
    {
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

        public ReferenceTests()
        {
      
            _sx2 = _cx.CreateSession(_sx.Database);

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

        [Fact]
        public void Lazy_reference_can_be_resolved()
        {
            var r = new Repository<Gizmo>(_sx);
            var g1 = r.Get("g1");
            Assert.Equal("w1", g1.Primary.Id);
            Assert.Equal(g1.Primary.Id, g1.Primary.Value.Id);
        }
        
        [Fact]
        public void Lazy_reference_array_can_be_resolved()
        {
            var r = new Repository<Gizmo>(_sx);
            var g2 = r.Get("g2");
            
            Assert.Equal(95, g2.Secondary.Sum(x => x.Value.Cost));
        }
        
        [Fact]
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
                r.Save(g3);
                
            }
            {
                var r = new Repository<Gizmo>(_sx2);
                var g3 = r.Get("g3");
                Assert.Equal(55, g3.Primary.Value.Cost);
            }
        }
        
        [Fact]
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
        
        [Fact]
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
                r.Save(g4);
            }
            {
                var r = new Repository<Gizmo>(_sx2);
                var g4 = r.Get("g4");
                Assert.Equal(45, g4.Primary.Value.Cost);
            }
        }
        
        [Fact]
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
        
        [Fact]
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
                Assert.Same(c1, c1a);
                Assert.Same(c2, c2a);
            }
        }
		
		[Fact]
		public void Poco_with_lazy1_can_be_written()
		{
			var a = new Lazypoco { Name = "foo" }; 
			var Da = _sx.Save(a);
			
			var b = new Lazypoco { Name = "bar", Parent = new Lazy<Lazypoco>(() => a) };
			var Db = _sx.Save(b);
			
			var c = _sx2.Load<Lazypoco>(Db.Id);
			Assert.Equal(c.Parent.Value.Name, a.Name);
		}
    }
}

