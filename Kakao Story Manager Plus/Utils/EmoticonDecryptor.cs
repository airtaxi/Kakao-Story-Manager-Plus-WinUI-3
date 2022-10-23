// Reference: https://github.com/blluv/KakaoTalkEmoticonDownloader

using ImageMagick;
using System.Collections.Generic;
using System.Text;

namespace KSMP.Utils
{
    class EmoticonDecryptor
    {
        public static void ConvertWebPToGIF(byte[] data, string fileName)
        {
            using (var animatedWebP = new MagickImageCollection(data))
            {
                animatedWebP.Write(fileName, MagickFormat.Gif);
            }
        }
        public static byte[] decodeImage(byte[] buf)
        {
            List<uint> seq = new List<uint>();
            seq = generateLFSR("a271730728cbe141e47fd9d677e9006da271730728cbe141e47fd9d677e9006d");
            for (int i = 0; i < 128; i++)
            {
                buf[i] = (byte)xorByte(buf[i], seq);
            }

            return buf;
        }
        public static List<uint> generateLFSR(string key)
        {
            List<uint> seq = new List<uint>();

            byte[] keySet = Encoding.UTF8.GetBytes(key);

            seq.Add(0x12000032);
            seq.Add(0x2527ac91);
            seq.Add(0x888c1214);


            for (int i = 0; i < 4; ++i)
            {
                seq[0] = keySet[i] | (seq[0] << 8);
                seq[1] = keySet[4 + i] | (seq[1] << 8);
                seq[2] = keySet[8 + i] | (seq[2] << 8);
            }

            return seq;
        }
        private static int xorByte(uint b, List<uint> seq)
        {
            char flag1 = (char)1;
            char flag2 = (char)0;

            int result = 0;

            for (int i = 0; i < 8; i++)
            {
                int v10 = (int)(seq[0] >> 1);
                if ((seq[0] << 31) != 0)
                {
                    seq[0] = (uint)(v10 ^ 0xC0000031);
                    uint v12 = seq[1] >> 1;
                    if (seq[1] << 31 != 0)
                    {
                        seq[1] = (uint)((v12 | 0xC0000000) ^ 0x20000010);
                        flag1 = (char)1;
                    }
                    else
                    {
                        seq[1] = (uint)(v12 & 0x3FFFFFFF);
                        flag1 = (char)0;
                    }
                }
                else
                {
                    seq[0] = (uint)v10;
                    int v11 = (int)(seq[2] >> 1);
                    if (seq[2] << 31 != 0)
                    {
                        seq[2] = (uint)((v11 | 0xF0000000) ^ 0x8000001);
                        flag2 = (char)1;
                    }
                    else
                    {
                        seq[2] = (uint)(v11 & 0xFFFFFFF);
                        flag2 = (char)0;
                    }
                }
                result = (flag1 ^ flag2 | 2 * result);
            }
            result = (int)(result ^ b);
            return result;
        }
    }
}
