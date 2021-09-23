/*  MapleLib - A general-purpose MapleStory library
 * Copyright (C) 2009, 2010 Snow and haha01haha01
   
 * This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.*/

using System;
using System.Security.Cryptography;

namespace MapleLib.MapleCryptoLib
{
	/// <summary>
	/// Class to manage Encryption and IV generation
	/// </summary>
	public class MapleCrypto
	{
		#region Properties
		/// <summary>
		/// (private) IV used in the packet encryption
		/// </summary>
		private volatile byte[] _IV;

		/// <summary>
		/// Version of MapleStory used in encryption
		/// </summary>
		private short _mapleVersion;

		/// <summary>
		/// (public) IV used in the packet encryption
		/// </summary>
		public byte[] IV
		{
			get { return _IV; }
			set { _IV = value; }
		}

        private RijndaelManaged mAES = new RijndaelManaged();
        private ICryptoTransform mTransformer = null;

		#endregion

		#region Methods
		/// <summary>
		/// Creates a new MapleCrypto class
		/// </summary>
		/// <param name="IV">Intializing Vector</param>
		/// <param name="mapleVersion">Version of MapleStory</param>
		public MapleCrypto(byte[] IV, short mapleVersion)
		{
			this._IV = IV;
			this._mapleVersion = mapleVersion;

            mAES.Key = CryptoConstants.UserKey;
            mAES.Mode = CipherMode.ECB;
            mAES.Padding = PaddingMode.PKCS7;
            mTransformer = mAES.CreateEncryptor();
		}

        public void Transform(byte[] pBuffer) //=crypt
        {
            int remaining = pBuffer.Length;
            int length = 0x5B0;
            int start = 0;
            byte[] real_IV = new byte[_IV.Length * 4];
            while (remaining > 0)
            {
                for (int index = 0; index < real_IV.Length; ++index) real_IV[index] = _IV[index % 4];

                if (remaining < length) length = remaining;
                for (int index = start; index < (start + length); ++index)
                {
                    if (((index - start) % real_IV.Length) == 0)
                    {
                        byte[] temp_IV = new byte[real_IV.Length];
                        mTransformer.TransformBlock(real_IV, 0, real_IV.Length, temp_IV, 0);
                        Buffer.BlockCopy(temp_IV, 0, real_IV, 0, real_IV.Length);
                        //real_IV = mTransformer.TransformFinalBlock(real_IV, 0, real_IV.Length);
                    }
                    pBuffer[index] ^= real_IV[(index - start) % real_IV.Length];
                }
                start += length;
                remaining -= length;
                length = 0x5B4;
            }
            ShiftIV();
        }

        public bool checkPacket(byte[] packet)
        {
            return ((((packet[0] ^ _IV[2]) & 0xFF) == ((_mapleVersion >> 8) & 0xFF)) && (((packet[1] ^ _IV[3]) & 0xFF) == (_mapleVersion & 0xFF)));
        }

        public void Decrypt(byte[] pBuffer)
        {
            Transform(pBuffer); //AES + IV shift
            for (int index1 = 1; index1 <= 6; ++index1)
            {
                byte firstFeedback = 0;
                byte secondFeedback = 0;
                byte length = (byte)(pBuffer.Length & 0xFF);
                if ((index1 % 2) == 0)
                {
                    for (int index2 = 0; index2 < pBuffer.Length; ++index2)
                    {
                        byte temp = pBuffer[index2];
                        temp -= 0x48;
                        temp = (byte)(~temp);
                        temp = temp.RollLeft(length & 0xFF);
                        secondFeedback = temp;
                        temp ^= firstFeedback;
                        firstFeedback = secondFeedback;
                        temp -= length;
                        temp = temp.RollRight(3);
                        pBuffer[index2] = temp;
                        --length;
                    }
                }
                else
                {
                    for (int index2 = pBuffer.Length - 1; index2 >= 0; --index2)
                    {
                        byte temp = pBuffer[index2];
                        temp = temp.RollLeft(3);
                        temp ^= 0x13;
                        secondFeedback = temp;
                        temp ^= firstFeedback;
                        firstFeedback = secondFeedback;
                        temp -= length;
                        temp = temp.RollRight(4);
                        pBuffer[index2] = temp;
                        --length;
                    }
                }
            }
        }

        public void Encrypt(byte[] data)
        {
            int size = data.Length;
            int j;
            byte a, c;
            for (int i = 0; i < 3; ++i)
            {
                a = 0;
                for (j = size; j > 0; --j)
                {
                    c = data[size - j];
                    c = c.RollLeft(3);
                    c = (byte)(c + j);
                    c ^= a;
                    a = c;
                    c = a.RollRight(j);
                    c ^= 0xFF;
                    c += 0x48;
                    data[size - j] = c;
                }
                a = 0;
                for (j = data.Length; j > 0; --j)
                {
                    c = data[j - 1];
                    c = c.RollLeft(4);
                    c = (byte)(c + j);
                    c ^= a;
                    a = c;
                    c ^= 0x13;
                    c = c.RollRight(3);
                    data[j - 1] = c;
                }
            }
            Transform(data); //crypt
        }

        private void ShiftIV()
        {
            byte[] newIV = new byte[] { 0xF2, 0x53, 0x50, 0xC6 };
            for (int index = 0; index < _IV.Length; ++index)
            {
                byte temp1 = newIV[1];
                byte temp2 = CryptoConstants.bShuffle[temp1];
                byte temp3 = _IV[index];
                temp2 -= temp3;
                newIV[0] += temp2;
                temp2 = newIV[2];
                temp2 ^= CryptoConstants.bShuffle[temp3];
                temp1 -= temp2;
                newIV[1] = temp1;
                temp1 = newIV[3];
                temp2 = temp1;
                temp1 -= newIV[0];
                temp2 = CryptoConstants.bShuffle[temp2];
                temp2 += temp3;
                temp2 ^= newIV[2];
                newIV[2] = temp2;
                temp1 += CryptoConstants.bShuffle[temp3];
                newIV[3] = temp1;
                uint result1 = (uint)newIV[0] | ((uint)newIV[1] << 8) | ((uint)newIV[2] << 16) | ((uint)newIV[3] << 24);
                uint result2 = result1 >> 0x1D;
                result1 <<= 3;
                result2 |= result1;
                newIV[0] = (byte)(result2 & 0xFF);
                newIV[1] = (byte)((result2 >> 8) & 0xFF);
                newIV[2] = (byte)((result2 >> 16) & 0xFF);
                newIV[3] = (byte)((result2 >> 24) & 0xFF);
            }
            Buffer.BlockCopy(newIV, 0, _IV, 0, _IV.Length);
        }

		/// <summary>
		/// Get a packet header for a packet being sent to the server
		/// </summary>
		/// <param name="size">Size of the packet</param>
		/// <returns>The packet header</returns>
		public byte[] getHeaderToClient(int size)
		{
			byte[] header = new byte[4];
			int a = _IV[3] * 0x100 + _IV[2];
			a ^= -(_mapleVersion + 1);
			int b = a ^ size;
			header[0] = (byte)(a % 0x100);
			header[1] = (byte)((a - header[0]) / 0x100);
			header[2] = (byte)(b ^ 0x100);
			header[3] = (byte)((b - header[2]) / 0x100);
			return header;
		}

		/// <summary>
		/// Get a packet header for a packet being sent to the client
		/// </summary>
		/// <param name="size">Size of the packet</param>
		/// <returns>The packet header</returns>
		public byte[] getHeaderToServer(int size)
		{
			byte[] header = new byte[4];
			int a = IV[3] * 0x100 + IV[2];
			a = a ^ (_mapleVersion);
			int b = a ^ size;
			header[0] = Convert.ToByte(a % 0x100);
			header[1] = Convert.ToByte(a / 0x100);
			header[2] = Convert.ToByte(b % 0x100);
			header[3] = Convert.ToByte(b / 0x100);
			return header;
		}

		/// <summary>
		/// Gets the length of a packet from the header
		/// </summary>
		/// <param name="packetHeader">Header of the packet</param>
		/// <returns>The length of the packet</returns>
		public static ushort getPacketLength(byte[] pBuffer, int pStart)
		{
            int length = (int)pBuffer[pStart] |
                        (int)(pBuffer[pStart + 1] << 8) |
                        (int)(pBuffer[pStart + 2] << 16) |
                        (int)(pBuffer[pStart + 3] << 24);
            length = (length >> 16) ^ (length & 0xFFFF);
            return (ushort)length;
		}

		/// <summary>
		/// Checks to make sure the packet is a valid MapleStory packet
		/// </summary>
		/// <param name="packetHeader">The header of the packet received</param>
		/// <returns>The packet is valid</returns>
		public bool checkPacketToServer(byte[] packet, int offset)
		{
			int a = packet[offset] ^ _IV[2];
			int b = _mapleVersion;
			int c = packet[offset + 1] ^ _IV[3];
			int d = _mapleVersion >> 8;
			return (a == b && c == d);
		}

		/// <summary>
		/// Multiplies bytes
		/// </summary>
		/// <param name="input">Bytes to multiply</param>
		/// <param name="count">Amount of bytes to repeat</param>
		/// <param name="mult">Times to repeat the packet</param>
		/// <returns>The multiplied bytes</returns>
		public static byte[] multiplyBytes(byte[] input, int count, int mult)
		{
			byte[] ret = new byte[count * mult];
			for (int x = 0; x < ret.Length; x++)
			{
				ret[x] = input[x % count];
			}
			return ret;
		}
		#endregion

	}

    public static class Extensions
    {
        public static byte RollLeft(this byte pThis, int pCount)
        {
            uint overflow = ((uint)pThis) << (pCount % 8);
            return (byte)((overflow & 0xFF) | (overflow >> 8));
        }

        public static byte RollRight(this byte pThis, int pCount)
        {
            uint overflow = (((uint)pThis) << 8) >> (pCount % 8);
            return (byte)((overflow & 0xFF) | (overflow >> 8));
        }
    }
}