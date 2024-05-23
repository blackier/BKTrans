using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;

namespace BKTrans.Core;

public class BKOCREasy : BKOCRBase
{
    private static readonly Dictionary<BKTransMap.LangType, string[]> LangMap =
        new()
        {
            { BKTransMap.LangType.ja, new string[] { "ja", "en" } },
            { BKTransMap.LangType.zh_cn, new string[] { "ch_sim", "en" } }
        };

    public class SettingEasyOCR : BKOCRSetting
    {
        public SettingEasyOCR()
        {
            name = "easy";
            language = BKTransMap.LangType.ja;
        }
    }

    internal class EasyOCR
    {
        dynamic _easy_ocr { get; set; }
        dynamic _mocr { get; set; }
        nint _threadState { get; set; }

        BKTransMap.LangType? _langType = null;

        public EasyOCR()
        {
            BKPythonEngine.Initialize();
            using (Py.GIL())
            {
                _easy_ocr = Py.Import("easyocr");
            }
        }

        public string OCR(Bitmap image, BKTransMap.LangType langType)
        {
            string result = "";
            image.Save("easy_ocr_temp.jpg", ImageFormat.Jpeg);
            using (Py.GIL())
            {
                if (_langType != langType)
                {
                    _mocr = _easy_ocr.Reader(LangMap[langType]);
                    _langType = langType;
                }
                PyList text = _mocr.readtext("easy_ocr_temp.jpg", detail: 0);
                result = string.Join("", text);
            }
            return result;
        }
    }

    private static Lazy<EasyOCR> _easyOCR = new();

    public BKOCREasy()
    {
        // https://github.com/JaidedAI/EasyOCR
    }

    public override List<BKTransMap.LangType> GetLangType()
    {
        return LangMap.Keys.ToList();
    }

    public override bool OCR(BKBaseSetting setting_, Bitmap image, out string result)
    {
        bool success = true;

        result = _easyOCR.Value.OCR(image, (setting_ as SettingEasyOCR).language);

        return success;
    }
}
