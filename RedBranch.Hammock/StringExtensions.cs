using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RedBranch.Hammock
{
    public static class StringExtensions
    {
        public static string ToSlug(this string str)
        {
            str = str.Substring(0, str.Length <= 45 ? str.Length : 45); // cut and trim it   
            str = str.ToLowerInvariant();
            str = Regex.Replace(str, @"[^a-z0-9]+", "-"); // invalid chars           
            
            // remove leading & trailing dashes
            str = Regex.Replace(str, @"^[-]", "");
            str = Regex.Replace(str, @"[-]$", "");

            return str;
        }
    }
}
