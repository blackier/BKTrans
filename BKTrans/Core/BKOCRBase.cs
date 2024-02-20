using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.Core;

public abstract class BKOCRBase
{

    public abstract List<BKTransMap.LangType> GetLangType();

    public abstract bool OCR(BKBaseSetting setting, Bitmap image, out string result);

    public virtual string ParseResult(string result)
    {
        return result;
    }
}
