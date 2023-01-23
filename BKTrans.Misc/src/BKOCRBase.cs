using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.Misc
{
    public abstract class BKOCRBase
    {
        public abstract bool OCR(BKSetting setting, Bitmap image, out string result);
        public virtual string ParseResult(string result)
        {
            return result;
        }
    }
}
