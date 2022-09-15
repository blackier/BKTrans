﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace BKAssembly
{
    public class BKOCRMicrosoft : BKOCRBase
    {
        public class SettingMiscrosoftOCR : BKSetting
        {
            public string language_tag { get; set; }

            public SettingMiscrosoftOCR()
            {
                language_tag = "";
            }
        }

        public BKOCRMicrosoft()
        {

        }
        public override bool OCR(BKSetting setting_, Bitmap image, out string result)
        {
            bool success = false;
            result = "";
            string lanTag = (setting_ as SettingMiscrosoftOCR).language_tag;
            do
            {
                Language language = new Language(lanTag);

                if (!OcrEngine.IsLanguageSupported(language))
                {
                    string supportlan = "";
                    foreach (var lan in OcrEngine.AvailableRecognizerLanguages)
                        supportlan += $" {lan.LanguageTag}({lan.DisplayName})";

                    result = $"{lanTag} language is not available in this system for OCR, support language: {supportlan}";

                    break;
                }

                OcrEngine engine = OcrEngine.TryCreateFromLanguage(language);

                using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
                {
                    image.Save(stream.AsStream(), ImageFormat.Bmp);
                    OcrResult ocrResult = Task.Run(async () =>
                    {
                        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                        return await engine.RecognizeAsync(softwareBitmap);
                    }).Result;

                    string separator = " ";

                    if (language.LanguageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase) || // Chinese
                        language.LanguageTag.StartsWith("ja", StringComparison.OrdinalIgnoreCase) || // Japanese
                        language.LanguageTag.StartsWith("ko", StringComparison.OrdinalIgnoreCase)) // Korean
                    {
                        // If CJK language then remove spaces between words.
                        result = string.Join(separator, ocrResult.Lines.Select(line => string.Concat(line.Words.Select(word => word.Text))));
                    }
                    else if (language.LayoutDirection == LanguageLayoutDirection.Rtl)
                    {
                        // If RTL language then reverse order of words.
                        result = string.Join(separator, ocrResult.Lines.Select(line => string.Join(" ", line.Words.Reverse().Select(word => word.Text))));
                    }
                    else
                    {
                        result = string.Join(separator, ocrResult.Lines.Select(line => line.Text));
                    }
                }
                success = true;
            } while (false);
            return success;
        }
    }
}
