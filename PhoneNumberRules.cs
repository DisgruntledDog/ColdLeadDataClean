using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColdLeadDataClean
{
    public class PhoneNumberRules
    {
        public string CountryId { get; set; }
        public string CountryName { get; set; }
        public int MinDigits { get; set; }
        public int MaxDigits { get; set; }
        public string Prefix { get; set; }

    }
}
