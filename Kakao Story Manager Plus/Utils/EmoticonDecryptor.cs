// Reference: https://github.com/blluv/KakaoTalkEmoticonDownloader

using ImageMagick;
using System.Collections.Generic;
using System.Text;

namespace KSMP.Utils;

public static class EmoticonDecryptor
{
    public static void ConvertWebPToGif(byte[] data, string fileName)
    {
        using var animatedWebP = new MagickImageCollection(data);
        animatedWebP.Write(fileName, MagickFormat.Gif);
    }

    public static byte[] DecodeImage(byte[] buffer)
    {
        List<uint> sequence = GenerateLFsr("a271730728cbe141e47fd9d677e9006da271730728cbe141e47fd9d677e9006d");
        for (int i = 0; i < 128; i++) buffer[i] = (byte)ApplyByteXor(buffer[i], sequence);
        return buffer;
    }

    public static List<uint> GenerateLFsr(string key)
    {
        List<uint> sequence = new();

        byte[] keys = Encoding.UTF8.GetBytes(key);

        sequence.Add(0x12000032);
        sequence.Add(0x2527ac91);
        sequence.Add(0x888c1214);

        for (int i = 0; i < 4; ++i)
        {
            sequence[0] = keys[i] | (sequence[0] << 8);
            sequence[1] = keys[4 + i] | (sequence[1] << 8);
            sequence[2] = keys[8 + i] | (sequence[2] << 8);
        }

        return sequence;
    }

    private static int ApplyByteXor(uint b, List<uint> sequence)
    {
        char flag1 = (char)1;
        char flag2 = (char)0;

        int result = 0;

        for (int i = 0; i < 8; i++)
        {
            int v10 = (int)(sequence[0] >> 1);
            if ((sequence[0] << 31) != 0)
            {
                sequence[0] = (uint)(v10 ^ 0xC0000031);
                uint v12 = sequence[1] >> 1;
                if (sequence[1] << 31 != 0)
                {
                    sequence[1] = (v12 | 0xC0000000) ^ 0x20000010;
                    flag1 = (char)1;
                }
                else
                {
                    sequence[1] = v12 & 0x3FFFFFFF;
                    flag1 = (char)0;
                }
            }
            else
            {
                sequence[0] = (uint)v10;
                int v11 = (int)(sequence[2] >> 1);
                if (sequence[2] << 31 != 0)
                {
                    sequence[2] = (uint)((v11 | 0xF0000000) ^ 0x8000001);
                    flag2 = (char)1;
                }
                else
                {
                    sequence[2] = (uint)(v11 & 0xFFFFFFF);
                    flag2 = (char)0;
                }
            }
            result = flag1 ^ flag2 | 2 * result;
        }
        result = (int)(result ^ b);
        return result;
    }
}
