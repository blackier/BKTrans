using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.Core;

public abstract class BKTransBase
{
    public abstract List<BKTransMap.LangType> GetLangType();
    public abstract string Trans(BKBaseSetting setting, string srcText);
}
