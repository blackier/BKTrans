using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.Misc
{
    public abstract class BKTransBase
    {
        public abstract List<string> GetLangType();
        public abstract string Trans(BKSetting setting, string srcText);
    }
}
