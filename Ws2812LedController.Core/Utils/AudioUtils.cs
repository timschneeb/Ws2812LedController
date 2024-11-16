namespace Ws2812LedController.Core.Utils;

public static class AudioUtils
{
    public static float[][] DeinterleaveFast(float[] inout)
    { 
        unsafe
        {
            float[][] tempbuf = new float[2][];
            int length = inout.Length / 2;

            fixed (float* buffer = inout)
            {
                float* pbuffer = buffer;

                tempbuf[0] = new float[length];
                tempbuf[1] = new float[length];

                fixed (float* buffer0 = tempbuf[0])
                fixed (float* buffer1 = tempbuf[1])
                {
                    float* pbuffer0 = buffer0;
                    float* pbuffer1 = buffer1;

                    for (int i = 0; i < length; i++)
                    {
                        *pbuffer0++ = *pbuffer++;
                        *pbuffer1++ = *pbuffer++;
                    }
                }
            }

            return tempbuf;
        }
    }
}