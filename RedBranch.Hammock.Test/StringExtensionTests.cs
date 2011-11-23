using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RedBranch.Hammock.Test
{
    [TestFixture]
    public class StringExtensionTests
    {
        [Test]
        public void ToSlug_lowers_case()
        {
            Assert.That("HOMEpage".ToSlug(), Is.EqualTo("homepage"));
        }        
        
        [Test]
        public void ToSlug_converts_nonalphanumeric_to_dashes()
        {
            Assert.That("1h.ome p/age".ToSlug(), Is.EqualTo("1h-ome-p-age"));
        }   
     
        [Test]
        public void ToSlug_condences_multiple_dashes()
        {
            Assert.That("home--page".ToSlug(), Is.EqualTo("home-page"));
        }   
  
        [Test]
        public void ToSlug_trims_leading_and_trailing_dashes()
        {
            Assert.That("(homepage)".ToSlug(), Is.EqualTo("homepage"));
        }
    }
}
