//
//  StringExtensionTests.cs
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
using NUnit.Framework;

namespace Hammock.Test
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
