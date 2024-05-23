using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.Core;

public class BKBaseSetting
{
    public string name { get; set; }
}

public class BKTransSetting : BKBaseSetting
{
    public BKTransMap.LangType from { get; set; }
    public BKTransMap.LangType to { get; set; }
}

public class BKOCRSetting : BKBaseSetting
{
    public BKTransMap.LangType language { get; set; }
}
