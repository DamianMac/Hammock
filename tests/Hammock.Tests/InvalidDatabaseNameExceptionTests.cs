﻿// 
//  InvalidDatabaseNameExceptionTests.cs
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
    
    public class InvalidDatabaseNameExceptionTests 
    {
        [Fact]
        public void Name_must_not_contain_uppercase()
        {
            Assert.Throws<InvalidDatabaseNameException>(() => 
                InvalidDatabaseNameException.Validate("asdfASDF"));    
        }       
        
        [Fact]
        public void Name_must_not_be_null()
        {
            Assert.Throws<ArgumentNullException>(() => 
                InvalidDatabaseNameException.Validate(null));    
        }
       
        [Fact]
        public void Name_must_not_be_empty()
        {
            Assert.Throws<InvalidDatabaseNameException>(() =>
                InvalidDatabaseNameException.Validate(string.Empty));    
        }

        [Fact]
        public void Name_must_not_contain_invalid_symbols()
        {
            Assert.Throws<InvalidDatabaseNameException>(() =>
                InvalidDatabaseNameException.Validate("asdf!@#$%^&*()"));    
        }

        [Fact]
        public void Name_must_start_with_alphas()
        {
            Assert.Throws<InvalidDatabaseNameException>(() =>
                InvalidDatabaseNameException.Validate("1234asdf"));
        }

        [Fact]
        public void Name_can_contain_valid_symbols()
        {
            InvalidDatabaseNameException.Validate("asdf_$()+-/");
        }
    }
}
