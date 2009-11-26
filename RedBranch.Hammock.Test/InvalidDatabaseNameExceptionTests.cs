using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace RedBranch.Hammock.Test
{
    [TestFixture]
    public class InvalidDatabaseNameExceptionTests 
    {
        [Test]
        public void Name_must_not_contain_uppercase()
        {
            Assert.Throws<InvalidDatabaseNameException>(() => 
                InvalidDatabaseNameException.Validate("asdfASDF"));    
        }       
        
        [Test]
        public void Name_must_not_be_null()
        {
            Assert.Throws<ArgumentNullException>(() => 
                InvalidDatabaseNameException.Validate(null));    
        }
       
        [Test]
        public void Name_must_not_be_empty()
        {
            Assert.Throws<InvalidDatabaseNameException>(() =>
                InvalidDatabaseNameException.Validate(string.Empty));    
        }

        [Test]
        public void Name_must_not_contain_invalid_symbols()
        {
            Assert.Throws<InvalidDatabaseNameException>(() =>
                InvalidDatabaseNameException.Validate("asdf!@#$%^&*()"));    
        }

        [Test]
        public void Name_must_start_with_alphas()
        {
            Assert.Throws<InvalidDatabaseNameException>(() =>
                InvalidDatabaseNameException.Validate("1234asdf"));
        }

        [Test]
        public void Name_can_contain_valid_symbols()
        {
            InvalidDatabaseNameException.Validate("asdf_$()+-/");
        }
    }
}
