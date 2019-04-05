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
using Xunit;

namespace Hammock.Test
{
    
    public class StringExtensionTests
    {
        [Fact]
        public void ToSlug_lowers_case()
        {
            Assert.Equal("homepage", "HOMEpage".ToSlug());
        }        
        
        [Fact]
        public void ToSlug_converts_nonalphanumeric_to_dashes()
        {
            Assert.Equal("1h-ome-p-age", "1h.ome p/age".ToSlug());
        }   
     
        [Fact]
        public void ToSlug_condences_multiple_dashes()
        {
            Assert.Equal("home-page", "home--page".ToSlug());
        }   
  
        [Fact]
        public void ToSlug_trims_leading_and_trailing_dashes()
        {
            Assert.Equal("homepage", "(homepage)".ToSlug());
        }
    }
}
