using ShareX.HelpersLib;
using ShareX.ScreenCaptureLib;
using System.Drawing;

namespace BKTrans.Core;

public class BKScreenCapture
{

    public struct DataStruct
    {
        public Bitmap captureBmp;
        public RectangleF captureRect;
    }

    public BKScreenCapture()
    {

    }

    public DataStruct CaptureRegion()
    {
        DataStruct result = new DataStruct();

        Screenshot screenshot = new Screenshot();
        screenshot.CaptureCursor = false;

        Bitmap canvas = screenshot.CaptureFullscreen();
        using (RegionCaptureForm form = new RegionCaptureForm(RegionCaptureMode.Default, new RegionCaptureOptions(), canvas))
        {
            form.ShowDialog();

            result.captureBmp = form.GetResultImage();
        }

        if (RegionCaptureForm.LastRegionFillPath != null)
        {
            Rectangle screenRectangle = CaptureHelpers.GetScreenBounds();
            result.captureRect = RectangleF.Intersect(RegionCaptureForm.LastRegionFillPath.GetBounds(), new RectangleF(0, 0, screenRectangle.Width, screenRectangle.Height));
        }
        canvas?.Dispose();
        return result;
    }

    //public DataStruct CaptureLastRegion()
    //{
    //    DataStruct result = new DataStruct();

    //    if (RegionCaptureForm.LastRegionFillPath != null)
    //    {
    //        var screenshot = new Screenshot().CaptureFullscreen();
    //        result.captureBmp = RegionCaptureTasks.ApplyRegionPathToImage(screenshot, RegionCaptureForm.LastRegionFillPath, out Rectangle captureRect);
    //        result.captureRect = captureRect;
    //        screenshot?.Dispose();
    //    }
    //    else
    //    {
    //        result = CaptureRegion();
    //    }

    //    return result;
    //}

    public DataStruct CaptureCustomRegion(RectangleF captureRect)
    {
        DataStruct result = new DataStruct();
        result.captureRect = captureRect;
        if (captureRect.IsEmpty)
            result = CaptureRegion();
        else
            result.captureBmp = new Screenshot().CaptureRectangle(Rectangle.Round(captureRect));

        return result;
    }

}
