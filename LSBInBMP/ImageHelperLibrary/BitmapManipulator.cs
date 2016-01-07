using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class BitmapManipulator
    {
        private Bitmap _img;
        private Bitmap _copy;


        public BitmapManipulator(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                _img = new Bitmap(ms);
                _copy = new Bitmap(ms);
            }

        }

        public void Strike()
        {
            for (int i = 0; i < _img.Width; i++)
            {
                _img.SetPixel(i, i, Color.Black);
            }

        }

        public byte[] Bytes
        {
            get
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    _copy.Save(ms, ImageFormat.Bmp);
                    data = ms.ToArray();

                }

                return data;

            }
        }

        public void InsertMessage(string message)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var dataToEncode = Encoding.ASCII.GetBytes(message);
            byte[] dataWithMeta = new byte[dataToEncode.Length + 4 + 2];
            //four bytes for message length; 2 bytes for padding
            var messageLengthInfo = BitConverter.GetBytes(message.Length);
            Array.Copy(messageLengthInfo, dataWithMeta, 4);
            Array.Copy(dataToEncode, 0, dataWithMeta, 6, dataToEncode.Length);

            BitArray bits = new BitArray(dataWithMeta);
            var e = bits.GetEnumerator();
            var bmpPixelEnumerator = new BitmapPixelEnumerator(_img);
            foreach (Pixel pixel in bmpPixelEnumerator)
            {

                byte oldBlue = pixel.Blue;
                byte oldGreen = pixel.Green;
                byte oldRed = pixel.Red;

                byte newBlue;
                if (InsertData(e, oldBlue, out newBlue)) return;
                _copy.SetPixel(pixel.X, pixel.Y, Color.FromArgb(newBlue, oldGreen, oldRed));


                byte newGreen;
                if (InsertData(e, oldGreen, out newGreen)) return;
                _copy.SetPixel(pixel.X, pixel.Y, Color.FromArgb(newBlue, newGreen, oldRed));


                byte newRed;
                if (InsertData(e, oldRed, out newRed)) return;
                _copy.SetPixel(pixel.X, pixel.Y, Color.FromArgb(newBlue, newGreen, newRed));
            }
            watch.Stop();
            Debug.WriteLine(watch.Elapsed);
        }



        private bool InsertData(IEnumerator e, byte oldBlue,
            out byte newBlue)
        {
            bool end = e.MoveNext();
            if (!end)
            {
                newBlue = oldBlue;
                return true;
            }
            var x1 = (bool)e.Current;
            e.MoveNext();
            var x2 = (bool)e.Current;

            var blueBits = new BitArray(new[] { oldBlue });

            var a1 = blueBits[0];
            var a2 = blueBits[1];
            var a3 = blueBits[2];
            var t1 = (x1 == a1 ^ a3);
            var t2 = (x2 == a2 ^ a3);
            if (t1 && t2)
            {
            }
            if (!t1 && t2)
            {
                blueBits[0] = !blueBits[0];
            }
            if (t1 && !t2)
            {
                blueBits[1] = !blueBits[1];
            }
            if (!t1 && !t2)
            {
                blueBits[2] = !blueBits[2];
            }

            newBlue = ConvertColorBitsToByte(blueBits);
            return false;
        }

        byte ConvertColorBitsToByte(BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }

        int ConvertBitsToWord(BitArray bits)
        {
            if (bits.Count < 32 && bits.Count > 48)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[6];
            bits.CopyTo(bytes, 0);
            return BitConverter.ToInt32(bytes, 0);
        }


        /// <summary>
        /// Expected 471 length
        /// </summary>
        /// <returns></returns>
        public string ReadMessage()
        {
            var bmpPixelEnumerator = new BitmapPixelEnumerator(_img);
            var encodedLength = bmpPixelEnumerator.Take(8).ToArray();

            int indexLengthBit = 0;
            var ba = new BitArray(48);
            foreach (var pixel in encodedLength)
            {
                indexLengthBit = ReadTwoBits(pixel.Blue, ba, indexLengthBit);
                indexLengthBit = ReadTwoBits(pixel.Green, ba, indexLengthBit);
                indexLengthBit = ReadTwoBits(pixel.Red, ba, indexLengthBit);
            }

            var messageLength = ConvertBitsToWord(ba);

            int indexMessageBit = 0;
            var messageBitArray = new BitArray(messageLength * 8);
            bmpPixelEnumerator.Reset();
            var encodedPixels = bmpPixelEnumerator.Skip(8).Take((int)Math.Ceiling(messageLength * 8d / 6d));
            foreach (var pixel in encodedPixels)
            {
                indexMessageBit = ReadTwoBits(pixel.Blue, messageBitArray, indexMessageBit);
                if (indexMessageBit >= messageBitArray.Count)
                {
                    break;
                }
                indexMessageBit = ReadTwoBits(pixel.Green, messageBitArray, indexMessageBit);
                if (indexMessageBit >= messageBitArray.Count)
                {
                    break;
                }
                indexMessageBit = ReadTwoBits(pixel.Red, messageBitArray, indexMessageBit);
                if (indexMessageBit >= messageBitArray.Count)
                {
                    break;
                }
            }
            
            byte[] messageBytes = new byte[messageLength];
            messageBitArray.CopyTo(messageBytes,0);
            var message = Encoding.ASCII.GetString(messageBytes);

            return message;
        }

        private static int ReadTwoBits(byte redByte, BitArray ba, int indexMessageBit)
        {
            var redBits = new BitArray(new[] { redByte });

            var a1 = redBits[0];
            var a2 = redBits[1];
            var a3 = redBits[2];

            var x1 = a1 ^ a3;
            ba.Set(indexMessageBit++, x1);
            var x2 = a2 ^ a3;
            ba.Set(indexMessageBit++, x2);
            return indexMessageBit;
        }
    }
}
