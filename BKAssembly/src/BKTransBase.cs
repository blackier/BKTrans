﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKAssembly
{
    public abstract class BKTransBase
    {
        public abstract string Trans(BKSetting setting, string srcText);
    }
}
