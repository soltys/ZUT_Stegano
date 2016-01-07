using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageHelperLibrary
{
    public static class BitArrayExtensions
    {
        public static byte ConvertColorBitsToByte(this BitArray bits)
        {
            if (bits.Count != 8)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            return bytes[0];
        }

        public static int  ConvertBitsToWord(this BitArray bits)
        { 
            if (bits.Count < 32 && bits.Count > 48)
            {
                throw new ArgumentException("bits");
            }
            byte[] bytes = new byte[6];
            bits.CopyTo(bytes, 0);
            return BitConverter.ToInt32(bytes, 0);
        }

    }
}
