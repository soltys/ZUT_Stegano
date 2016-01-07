using System;
using System.Drawing;

namespace ImageHelperLibrary
{
    public class Pixel
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Color Color { get; set; }
        public byte[] ColorBytes
        {
            get { return BitConverter.GetBytes(Color.ToArgb()); }
        }

        public byte Red
        {
            get { return ColorBytes[0]; }
        }

        public byte Green
        {
            get { return ColorBytes[1]; }
        }

        public byte Blue
        {
            get { return ColorBytes[2]; }
        }


    }
}