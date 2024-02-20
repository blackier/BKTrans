using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BKTrans.Core;

public class BKTransMap
{
    [JsonConverter(typeof(BKJsonStringEnumConverter<LangType>))]
    public enum LangType
    {
        zh_cn = 0,
        ja,
        en_us,
        ko,
        fr,
        de,
        ru,
        es,
        pt,
        it,
    }
    public readonly static List<LangType> LangTypeList = EnumExtensions.GetTypeList<LangType>().ToList();

    [JsonConverter(typeof(BKJsonStringEnumConverter<LangType>))]
    public enum OCRType
    {
        baidu = 0,
        microsoft,
        manga,
        easy
    }
    public readonly static List<OCRType> OCRTypeList = EnumExtensions.GetTypeList<OCRType>().ToList();

    [JsonConverter(typeof(BKJsonStringEnumConverter<LangType>))]
    public enum TransType
    {
        baidu = 0,
        caiyun,
        google
    }
    public readonly static List<TransType> TransTypeList = EnumExtensions.GetTypeList<TransType>().ToList();


    public static BKOCRBase CreateBKOCRClient(OCRType ocrType)
    {
        switch (ocrType)
        {
            case OCRType.baidu:
                return new BKOCRBaidu();
            case OCRType.microsoft:
                return new BKOCRMicrosoft();
            case OCRType.manga:
                return new BKOCRManga();
            case OCRType.easy:
                return new BKOCREasy();
            default:
                return new BKOCRBaidu();
        }
    }

    public static BKTransBase CreateBKTransClient(TransType transType)
    {
        switch (transType)
        {
            case TransType.baidu:
                return new BKTransBaidu();
            case TransType.caiyun:
                return new BKTransCaiyun();
            case TransType.google:
                return new BKTransGoogle();
            default:
                return new BKTransBaidu();
        }
    }
}
