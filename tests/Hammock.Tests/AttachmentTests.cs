﻿// 
//  AttachmentTests.cs
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
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace Hammock.Tests
{

    public class AttachmentTests : DatabaseTestFixture
    {
        

        public class Widget : IHasAttachments
        {
            public string Name { get; set; }
            [JsonIgnore] public Attachments Attachments { get; set; }
        }

       

        [Fact]
        public void Session_can_only_save_attachments_to_enrolled_entities()
        {
            var a = "Sessions can save attachments!";
            var w = new Widget() { Name = "foo" };
            Assert.Throws<Exception>(
                () => _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)))
            );
        }

        [Fact]
        public void Session_can_save_attachments()
        {
            var a = "Sessions can save attachments!";
            var w = new Widget() { Name = "foo" };
            var d = _sx.Save(w);
            var e = _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)));

            Assert.NotEqual(d.Revision, e.Revision);

        }

        [Fact]
        public void Session_can_list_attachments()
        {
            var a = "Sessions can list attachments!";
            var w = new Widget() { Name = "foo" };
            var d = _sx.Save(w);
            var e = _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)));

            var x = _cx.CreateSession(_sx.Database);
            var y = x.Load<Widget>(e.Id);

            Assert.Equal(y.Attachments["test.txt"].ContentType, "text/plain");
        }

        [Fact]
        public void Session_can_delete_attachments()
        {
            var a = "Sessions can list attachments!";
            var w = new Widget() { Name = "foo" };
            var d = _sx.Save(w);
            var e = _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)));
            w.Attachments.Clear();
            _sx.Save(w);

            var x = _cx.CreateSession(_sx.Database);
            var y = x.Load<Widget>(e.Id);

            Assert.Null(y.Attachments);
        }

        [Fact]
        public void Session_can_load_attachments()
        {
            var a = "Sessions can load attachments!";
            var w = new Widget() { Name = "foo" };
            var d = _sx.Save(w);
            var e = _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)));

            var x = _cx.CreateSession(_sx.Database);
            var y = x.Load<Widget>(e.Id);

            var z = y.Attachments["test.txt"].Load();
            var b = new StreamReader(z.GetResponseStream()).ReadToEnd();

            Assert.Equal(b, a);
        }


    }
}
