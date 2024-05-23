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

public class BKOCRManga : BKOCRBase
{
    private static readonly Dictionary<BKTransMap.LangType, string> LangMap =
        new() { { BKTransMap.LangType.ja, "ja" }, { BKTransMap.LangType.zh_cn, "zh" }, };

    public class SettingMangaOCR : BKOCRSetting
    {
        public SettingMangaOCR()
        {
            name = "manga";
            language = BKTransMap.LangType.ja;
        }
    }

    internal class MangaOCR
    {
        dynamic _manga_ocr { get; set; }
        dynamic _mocr { get; set; }
        nint _threadState { get; set; }

        public MangaOCR()
        {
            BKPythonEngine.Initialize();
            using (Py.GIL())
            {
                _manga_ocr = Py.Import("manga_ocr");
                _mocr = _manga_ocr.MangaOcr();
            }
        }

        public string OCR(Bitmap image)
        {
            string result = "";
            image.Save($"manga_ocr_temp.jpg", ImageFormat.Jpeg);
            using (Py.GIL())
            {
                result = _mocr("manga_ocr_temp.jpg");
            }
            return result;
        }
    }

    private static Lazy<MangaOCR> _mangeOCR = new();

    public BKOCRManga()
    {
        // https://github.com/kha-white/manga-ocr
    }

    public override List<BKTransMap.LangType> GetLangType()
    {
        return LangMap.Keys.ToList();
    }

    public override bool OCR(BKBaseSetting setting_, Bitmap image, out string result)
    {
        bool success = true;
        result = _mangeOCR.Value.OCR(image);

        return success;
    }
}
