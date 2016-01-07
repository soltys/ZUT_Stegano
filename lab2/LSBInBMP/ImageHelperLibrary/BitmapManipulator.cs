using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageHelperLibrary
{
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

        const int MessageLength = 4;
        const int MessageLengthPadding = 2;

        const int BitsPerByte = 8;
        const int BitsPerPixel = 6;

        const int BitsWithMessageLengthAndPadding = (MessageLength + MessageLengthPadding) * BitsPerByte;
        const int PixelsWithMessageLengthAndPadding = BitsWithMessageLengthAndPadding / BitsPerPixel;

        public void InsertMessage(string message)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var dataToEncode = Encoding.ASCII.GetBytes(message);

            byte[] dataWithMeta = new byte[dataToEncode.Length + MessageLength + MessageLengthPadding];


            //four bytes for message length; 2 bytes for padding
            var messageLengthInfo = BitConverter.GetBytes(message.Length);
            Array.Copy(messageLengthInfo, dataWithMeta, MessageLength);
            Array.Copy(dataToEncode, 0, dataWithMeta, MessageLength + MessageLengthPadding, dataToEncode.Length);

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <param name="oldBlue"></param>
        /// <param name="newBlue"></param>
        /// <returns>Returns true if enumerator has ended</returns>
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

            newBlue = blueBits.ConvertColorBitsToByte();
            return false;
        }

      

        /// <summary>
        /// Expected 471 length
        /// </summary>
        /// <returns></returns>
        public string ReadMessage()
        {
            var bmpPixelEnumerator = new BitmapPixelEnumerator(_img);
            var encodedLength = bmpPixelEnumerator.Take(PixelsWithMessageLengthAndPadding).ToArray();

            int indexLengthBit = 0;
            var ba = new BitArray(BitsWithMessageLengthAndPadding);
            foreach (var pixel in encodedLength)
            {
                indexLengthBit = ReadTwoBits(pixel.Blue, ba, indexLengthBit);
                indexLengthBit = ReadTwoBits(pixel.Green, ba, indexLengthBit);
                indexLengthBit = ReadTwoBits(pixel.Red, ba, indexLengthBit);
            }

            var messageLength = ba.ConvertBitsToWord();

            int indexMessageBit = 0;
            var messageBitArray = new BitArray(messageLength * PixelsWithMessageLengthAndPadding);
            bmpPixelEnumerator.Reset();
            var encodedPixels = bmpPixelEnumerator.Skip(PixelsWithMessageLengthAndPadding).Take((int)Math.Ceiling(messageLength * (double)PixelsWithMessageLengthAndPadding / (double)BitsPerPixel));
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
            messageBitArray.CopyTo(messageBytes, 0);
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
