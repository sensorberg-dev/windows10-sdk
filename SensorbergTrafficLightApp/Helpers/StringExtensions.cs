using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorbergTrafficLightApp.Helpers
{
    public static class StringExtensions
    {
        public static string ToFirstLetterUpper(this string input)
        {
            if (input.Length < 2)
            {
                return input;
            }

            return input[0].ToString().ToUpper() + input.Substring(1, input.Length - 1).ToLower();
        }
    }
}
