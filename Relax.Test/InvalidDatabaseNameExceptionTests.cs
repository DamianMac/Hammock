using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace Relax.Test
{
    [TestFixture]
    public class InvalidDatabaseNameExceptionTests
    {
        [Test]
        [ExpectedException(ExpectedException = typeof(InvalidDatabaseNameException))]
        public void Name_must_not_contain_uppercase()
        {
            InvalidDatabaseNameException.Validate("asdfASDF");    
        }       
        
        [Test]
        [ExpectedException(ExpectedException = typeof(ArgumentNullException))]
        public void Name_must_not_be_null()
        {
            InvalidDatabaseNameException.Validate(null);    
        }
       
        [Test]
        [ExpectedException(ExpectedException = typeof(InvalidDatabaseNameException))]
        public void Name_must_not_be_empty()
        {
            InvalidDatabaseNameException.Validate(string.Empty);    
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(InvalidDatabaseNameException))]
        public void Name_must_not_contain_invalid_symbols()
        {
            InvalidDatabaseNameException.Validate("asdf!@#$%^&*()");    
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(InvalidDatabaseNameException))]
        public void Name_must_start_with_alphas()
        {
            InvalidDatabaseNameException.Validate("1234asdf");
        }

        [Test]
        public void Name_can_contain_valid_symbols()
        {
            InvalidDatabaseNameException.Validate("asdf_$()+-/");
        }
    }
}
