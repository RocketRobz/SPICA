using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPICA.Formats.Common.Compression
{
    public class LZ11
    {
        public const int SIGNATURE = 0x11;

        public static byte[] decompress(byte[] data)
        {
            Stream strm = new MemoryStream(data);
            byte[] dec = decompress(strm);
            strm.Close();
            return dec;
        }

        public static byte[] decompress(Stream instream)
        {
            //Algorithm based on DSDecmp enhanced for 2-3x speed increase through arraycopy
            BinaryReader reader = new BinaryReader(instream);

            byte type = (byte)reader.ReadByte();
            if (type != SIGNATURE)
            {
                throw new NotSupportedException("Not a LZ11 compressed stream.");
            }
            uint decompressedSize = reader.ReadUInt24();
            byte[] outData = new byte[decompressedSize];

            int currentOutSize = 0;
            int flags = 0, mask = 1;
            while (currentOutSize < decompressedSize)
            {
                if (mask == 1)
                {
                    flags = instream.ReadByte();
                    mask = 0x80;
                }
                else
                {
                    mask >>= 1;
                }
                if ((flags & mask) > 0)
                {
                    int byte1 = instream.ReadByte();
                    int length = byte1 >> 4;
                    int disp;

                    switch (length)
                    {
                        case 0:
                            {
                                int byte2 = instream.ReadByte();
                                int byte3 = instream.ReadByte();
                                length = (((byte1 & 0x0F) << 4) | (byte2 >> 4)) + 0x11;
                                disp = (((byte2 & 0x0F) << 8) | byte3) + 0x1;
                                break;
                            }
                        case 1:
                            {
                                int byte2 = instream.ReadByte();
                                int byte3 = instream.ReadByte();
                                int byte4 = instream.ReadByte();
                                length = (((byte1 & 0x0F) << 12) | (byte2 << 4) | (byte3 >> 4)) + 0x111;
                                disp = (((byte3 & 0x0F) << 8) | byte4) + 0x1;
                                break;
                            }
                        default:
                            {
                                int byte2 = instream.ReadByte();
                                length = ((byte1 & 0xF0) >> 4) + 0x1;
                                disp = (((byte1 & 0x0F) << 8) | byte2) + 0x1;
                                break;
                            }
                    }

                    int o = currentOutSize;
                    for (int i = 0; i <= length / disp; i++, o += disp)
                    {
                        Buffer.BlockCopy(outData, currentOutSize - disp, outData, o, Math.Min(disp, length - i * disp));
                    }

                    currentOutSize += length;
                }
                else
                {
                    outData[currentOutSize] = (byte)instream.ReadByte();
                    currentOutSize++;
                }
            }
            return outData;
        }
    }
}
