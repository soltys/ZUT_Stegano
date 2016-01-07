using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace ImageHelperLibrary
{
    public class BitmapPixelEnumerator : IEnumerator<Pixel>, IEnumerable<Pixel>
    {
        private Bitmap _bitmap;
        private int _y;
        private int _x;

        public BitmapPixelEnumerator(Bitmap bitmap)
        {
            _bitmap = bitmap;
            Reset();
        }
        public void Dispose()
        {

        }

        public bool MoveNext()
        {
            var isDone = _x == _bitmap.Width && _y == _bitmap.Height;

            _x++;
            if (_bitmap.Width == _x)
            {
                _x = 0;
                _y++;
            }

            return !isDone;
        }

        public void Reset()
        {
            _x = 0;
            _y = 0;
        }

        public Pixel Current
        {
            get
            {
                var p = new Pixel();
                p.X = _x;
                p.Y = _y;
                p.Color = _bitmap.GetPixel(_x, _y);
                return p;
            }

        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public IEnumerator<Pixel> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}