using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace RedBranch.Hammock.Test
{
    [TestFixture]
    public class AttachmentTests
    {
        private Connection _cx;
        private Session _sx;

        public class Widget : IHasAttachments
        {
            public string Name { get; set; }
            [JsonIgnore] public Attachments Attachments { get; set; }
        }

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            _cx = ConnectionTests.CreateConnection();
            if (_cx.ListDatabases().Contains("relax-session-tests"))
            {
                _cx.DeleteDatabase("relax-session-tests");
            }
            _cx.CreateDatabase("relax-session-tests");
            _sx = _cx.CreateSession("relax-session-tests");
        }

        [Test]
        public void Session_can_only_save_attachments_to_enrolled_entities()
        {
            var a = "Sessions can save attachments!";
            var w = new Widget() {Name = "foo"};
            Assert.Throws<Exception>(
                () => _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)))
            );
        }

        [Test]
        public void Session_can_save_attachments()
        {
            var a = "Sessions can save attachments!";
            var w = new Widget() {Name = "foo"};
            var d = _sx.Save(w);
            var e = _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)));

            Assert.That(d.Revision, Is.Not.EqualTo(e.Revision));

        }

        [Test]
        public void Session_can_list_attachments()
        {
            var a = "Sessions can list attachments!";
            var w = new Widget() {Name = "foo"};
            var d = _sx.Save(w);
            var e = _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)));

            var x = _cx.CreateSession(_sx.Database);
            var y = x.Load<Widget>(e.Id);

            Assert.That(y.Attachments["test.txt"].ContentType, Is.EqualTo("text/plain"));
        }

        [Test]
        public void Session_can_delete_attachments()
        {
            var a = "Sessions can list attachments!";
            var w = new Widget() {Name = "foo"};
            var d = _sx.Save(w);
            var e = _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)));
            w.Attachments.Clear();
            _sx.Save(w);

            var x = _cx.CreateSession(_sx.Database);
            var y = x.Load<Widget>(e.Id);

            Assert.That(y.Attachments, Is.Null);
        }

        [Test]
        public void Session_can_load_attachments()
        {
            var a = "Sessions can load attachments!";
            var w = new Widget() {Name = "foo"};
            var d = _sx.Save(w);
            var e = _sx.AttachFile(w, "test.txt", "text/plain", new MemoryStream(Encoding.ASCII.GetBytes(a)));

            var x = _cx.CreateSession(_sx.Database);
            var y = x.Load<Widget>(e.Id);

            var z = y.Attachments["test.txt"].Load();
            var b = new StreamReader(z.GetResponseStream()).ReadToEnd();

            Assert.That(b, Is.EqualTo(a));
        }
    }
}
