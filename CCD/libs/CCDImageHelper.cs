using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using OpenCvSharp;

namespace CCD.libs
{
    public class CCDImageHelper
    {
        public static void FromNativePointer(WriteableBitmap wbm, IntPtr pData, int ch)
        {
            if (wbm == null || wbm.IsFrozen)
            {
                return;
            }
            CopyMemory(wbm.BackBuffer, pData, (uint)(wbm.Width * wbm.Height * ch));
            wbm.Lock();
            wbm.AddDirtyRect(new Int32Rect(0, 0, wbm.PixelWidth, wbm.PixelHeight));
            wbm.Unlock();


        }

        public static Mat GetMatPointer(IntPtr pData, int ch, int Width, int Height)
        {
            return new Mat(Height, Width, MatType.CV_8UC3, pData);
        }

        public static BitmapSource FromArray(byte[] data, int w, int h, int ch)
        {
            PixelFormat format = PixelFormats.Default;

            if (ch == 1) format = PixelFormats.Gray8; //grey scale image 0-255
            if (ch == 3) format = PixelFormats.Bgr24; //RGB


            WriteableBitmap wbm = new WriteableBitmap(w, h, 96, 96, format, null);
            wbm.WritePixels(new Int32Rect(0, 0, w, h), data, ch * w, 0);
            wbm.Freeze();
            return wbm;
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", CharSet = CharSet.Ansi)]
        public extern static long CopyMemory(IntPtr dest, IntPtr source, uint size);
    }
}
