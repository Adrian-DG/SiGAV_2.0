using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Common
{
    public static class DomainRegexPattern
    {
        private const string PlacaVehiculo = @"^[A-Z]\d{5,6}$";
        public static readonly Regex PlacaRegex = new(PlacaVehiculo, RegexOptions.Compiled);
    }
}
