using ShareX.HelpersLib;
using ShareX.ScreenCaptureLib;
using System.Drawing;

namespace BKAssembly
{
    public class BKScreenCapture
    {

        public struct DataStruct
        {
            public Bitmap captureBmp;
            public Rectangle captureRect;
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
                Rectangle regionArea = Rectangle.Round(RegionCaptureForm.LastRegionFillPath.GetBounds());
                Rectangle screenRectangle = CaptureHelpers.GetScreenBounds();
                result.captureRect = Rectangle.Intersect(regionArea, new Rectangle(0, 0, screenRectangle.Width, screenRectangle.Height));
            }

            return result;
        }

        public DataStruct CaptureLastRegion()
        {
            DataStruct result = new DataStruct();

            if (RegionCaptureForm.LastRegionFillPath != null)
            {
                Screenshot screenshot = new Screenshot();
                result.captureBmp = RegionCaptureTasks.ApplyRegionPathToImage(screenshot.CaptureFullscreen(), RegionCaptureForm.LastRegionFillPath, out result.captureRect);
            }
            else
            {
                result = CaptureRegion();
            }

            return result;
        }

    }
}
