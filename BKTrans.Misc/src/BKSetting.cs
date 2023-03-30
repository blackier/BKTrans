using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.Misc
{
    public class BKSetting
    {
        public string name { get; set; }
    }

    public class BKTransSetting : BKSetting
    {
        public string from { get; set; }
        public string to { get; set; }
    }

    public class BKOCRSetting : BKSetting
    {
        public string language { get; set; }
    }
}
