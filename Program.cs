using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColdLeadDataClean
{
    class Program
    {
        static void Main(string[] args)
        {
            CleanContacts cc = new CleanContacts();
            cc.Clean(args[0]);

            CleanInteractions ci = new CleanInteractions();
            ci.Clean(args[1]);

        }
    }
}
