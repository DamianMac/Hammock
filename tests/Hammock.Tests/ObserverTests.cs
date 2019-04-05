// 
//  ObserverTests.cs
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
namespace Hammock.Tests
{
    
    public class ObserverTests : DatabaseTestFixture
    {

        public class Widget
        {
            public string Name { get; set; }
        }


        public class MockObserver : IObserver
        {
            public Disposition BeforeSaveDisposition = Disposition.Continue;
            public Disposition BeforeDeleteDisposition = Disposition.Continue; 

            public int TimesBeforeSaveCalled;
            public int TimesBeforeDeleteCalled;
            public int TimesAfterSaveCalled;
            public int TimesAfterDeleteCalled;
            public int TimesAfterLoadCalled;

            public object LastEntity;
            public Document LastDocument;

            public Disposition BeforeSave(object entity, Document document)
            {
                LastEntity = entity;
                LastDocument = document;
                TimesBeforeSaveCalled++;
                return BeforeSaveDisposition;
            }

            public Disposition BeforeDelete(object entity, Document document)
            {
                LastEntity = entity;
                LastDocument = document;
                TimesBeforeDeleteCalled++;
                return BeforeDeleteDisposition;
            }

            public void AfterSave(object entity, Document document)
            {
                LastEntity = entity;
                LastDocument = document;
                TimesAfterSaveCalled++;
            }

            public void AfterDelete(object entity, Document document)
            {
                LastEntity = entity;
                LastDocument = document;
                TimesAfterDeleteCalled++;
            }

            public void AfterLoad(object entity, Document document)
            {
                LastEntity = entity;
                LastDocument = document;
                TimesAfterLoadCalled++;
            }
        }

        [Fact]
        public void Session_invokes_observer_before_save()
        {
            var o = new MockObserver();
            var sx = _cx.CreateSession("relax-observer-tests");
            sx.Observers.Add(o);
            var w = new Widget();
            sx.Save(w);
            Assert.Equal(1, o.TimesBeforeSaveCalled);
            Assert.Same(w, o.LastEntity);
        }

        [Fact]
        public void Session_invokes_observer_after_save()
        {
            var o = new MockObserver();
            var sx = _cx.CreateSession("relax-observer-tests");
            sx.Observers.Add(o);
            var w = new Widget();
            sx.Save(w);
            Assert.Equal(1, o.TimesAfterSaveCalled);
        }

        [Fact]
        public void Session_invokes_observer_before_delete()
        {
            var o = new MockObserver();
            var sx = _cx.CreateSession("relax-observer-tests");
            sx.Observers.Add(o);
            var w = new Widget();
            sx.Save(w);
            sx.Delete(w);
            Assert.Equal(1, o.TimesBeforeDeleteCalled);
        }

        [Fact]
        public void Session_invokes_observer_after_delete()
        {
            var o = new MockObserver();
            var sx = _cx.CreateSession("relax-observer-tests");
            sx.Observers.Add(o);
            var w = new Widget();
            sx.Save(w);
            sx.Delete(w);
            Assert.Equal(1, o.TimesAfterDeleteCalled);
        }

        [Fact]
        public void Session_invokes_observer_after_load()
        {
            var o = new MockObserver();
            var sx1 = _cx.CreateSession("relax-observer-tests");
            var sx2 = _cx.CreateSession("relax-observer-tests");
            sx2.Observers.Add(o);
            var w = new Widget();
            var d = sx1.Save(w);
            var x = sx2.Load<Widget>(d.Id);
            Assert.Equal(1, o.TimesAfterLoadCalled);
        }

        [Fact]
        public void Session_respects_observer_before_save_disposition()
        {
            var o = new MockObserver();
            o.BeforeSaveDisposition = Disposition.Decline;
            var sx = _cx.CreateSession("relax-observer-tests");
            sx.Observers.Add(o);
            var w = new Widget();
            Assert.Throws<Exception>(() => sx.Save(w));
            Assert.Equal(0, o.TimesAfterSaveCalled);
        }

        [Fact]
        public void Session_respects_observer_before_delete_disposition()
        {
            var o = new MockObserver();
            o.BeforeDeleteDisposition = Disposition.Decline;
            var sx = _cx.CreateSession("relax-observer-tests");
            sx.Observers.Add(o);
            var w = new Widget();
            var d = sx.Save(w);
            Assert.Throws<Exception>(() => sx.Delete(w));
            Assert.Equal(0, o.TimesAfterDeleteCalled);
        }

        [Fact]
        public void Connection_fills_session_observers()
        {
            var cx = ConnectionTests.CreateConnection();
            cx.Observers.Add(x => new MockObserver());
            var sx = cx.CreateSession("relax-observer-tests");
            Assert.Equal(1, sx.Observers.Count);
        }
    }
}
