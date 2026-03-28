// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Drawing.Imaging;

namespace RunCat365
{
    internal readonly ref struct BitmapLock
    {
        private readonly Bitmap _bitmap;
        internal BitmapData Data { get; }

        internal BitmapLock(Bitmap bitmap, ImageLockMode mode)
        {
            _bitmap = bitmap;
            Data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                mode,
                PixelFormat.Format32bppArgb
            );
        }

        internal void Dispose()
        {
            _bitmap.UnlockBits(Data);
        }
    }

    internal static class BitmapExtension
    {
        internal static Bitmap Recolor(this Bitmap bitmap, Color color)
        {
            var newBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

            using var srcLock = new BitmapLock(bitmap, ImageLockMode.ReadOnly);
            using var dstLock = new BitmapLock(newBitmap, ImageLockMode.WriteOnly);

            unsafe
            {
                byte* srcPtr = (byte*)srcLock.Data.Scan0;
                byte* dstPtr = (byte*)dstLock.Data.Scan0;

                for (int y = 0; y < bitmap.Height; y++)
                {
                    byte* srcRow = srcPtr + (y * srcLock.Data.Stride);
                    byte* dstRow = dstPtr + (y * dstLock.Data.Stride);

                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        byte* srcPixel = srcRow + (x * 4);
                        byte* dstPixel = dstRow + (x * 4);

                        dstPixel[0] = color.B;
                        dstPixel[1] = color.G;
                        dstPixel[2] = color.R;
                        dstPixel[3] = srcPixel[3];
                    }
                }
            }

            return newBitmap;
        }

        internal static Bitmap ScaleToFit(this Bitmap bitmap, int targetSize)
        {
            float scale = Math.Min(
                (float)targetSize / bitmap.Width,
                (float)targetSize / bitmap.Height
            );

            int newW = (int)(bitmap.Width * scale);
            int newH = (int)(bitmap.Height * scale);
            int offsetX = (targetSize - newW) / 2;
            int offsetY = (targetSize - newH) / 2;

            var canvas = new Bitmap(targetSize, targetSize, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(canvas);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(bitmap, offsetX, offsetY, newW, newH);
            return canvas;
        }

        internal static Icon ToIcon(this Bitmap bitmap)
        {
            using var pngStream = new MemoryStream();
            bitmap.Save(pngStream, ImageFormat.Png);
            var pngData = pngStream.ToArray();

            using var icoStream = new MemoryStream();
            using var bw = new BinaryWriter(icoStream);

            bw.Write((short)0);
            bw.Write((short)1);
            bw.Write((short)1);

            bw.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));
            bw.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((short)1);
            bw.Write((short)32);
            bw.Write(pngData.Length);
            bw.Write(22);

            bw.Write(pngData);

            icoStream.Position = 0;
            return new Icon(icoStream);
        }
    }
}
