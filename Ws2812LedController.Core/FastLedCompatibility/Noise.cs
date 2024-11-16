namespace Ws2812LedController.Core.FastLedCompatibility;

public static class Noise
{
    private static readonly byte[] P = {151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180, 151};

    private static sbyte inoise8_raw(ushort x, ushort y, ushort z)
    {
        // Find the unit cube containing the point
        byte X = (byte)(x >> 8);
        byte Y = (byte)(y >> 8);
        byte Z = (byte)(z >> 8);

        // Hash cube corner coordinates
        byte A = (byte)(P[X] + Y);
        byte AA = (byte)(P[A] + Z);
        byte AB = (byte)(P[A + 1] + Z);
        byte B = (byte)(P[X + 1] + Y);
        byte BA = (byte)(P[B] + Z);
        byte BB = (byte)(P[B + 1] + Z);

        // Get the relative position of the point in the cube
        byte u = (byte)x;
        byte v = (byte)y;
        byte w = (byte)z;

        // Get a signed version of the above for the grad function
        sbyte xx = (sbyte)(((byte)(x) >> 1) & 0x7F);
        sbyte yy = (sbyte)(((byte)(y) >> 1) & 0x7F);
        sbyte zz = (sbyte)(((byte)(z) >> 1) & 0x7F);
        byte N = 0x80;

        u = ease8InOutQuad(u);
        v = ease8InOutQuad(v);
        w = ease8InOutQuad(w);

        sbyte X1 = lerp7by8(grad8(P[AA], xx, yy, zz), grad8(P[BA], (sbyte)(xx - N), yy, zz), u);
        sbyte X2 = lerp7by8(grad8(P[AB], xx, (sbyte)(yy - N), zz), grad8(P[BB], (sbyte)(xx - N), (sbyte)(yy - N), zz), u);
        sbyte X3 = lerp7by8(grad8(P[AA + 1], xx, yy, (sbyte)(zz - N)), grad8(P[BA + 1], (sbyte)(xx - N), yy, (sbyte)(zz - N)), u);
        sbyte X4 = lerp7by8(grad8(P[AB + 1], xx, (sbyte)(yy - N), (sbyte)(zz - N)), grad8(P[BB + 1], (sbyte)(xx - N), (sbyte)(yy - N), (sbyte)(zz - N)), u);

        sbyte Y1 = lerp7by8(X1,X2,v);
        sbyte Y2 = lerp7by8(X3,X4,v);

        sbyte ans = lerp7by8(Y1,Y2,w);

        return ans;
    }
    
    public static sbyte lerp7by8(sbyte a, sbyte b, byte frac)
    {
        // int8_t delta = b - a;
        // int16_t prod = (uint16_t)delta * (uint16_t)frac;
        // int8_t scaled = prod >> 8;
        // int8_t result = a + scaled;
        // return result;
        sbyte result;
        if (b > a)
        {
            byte delta = (byte)(b - a);
            byte scaled = Scale.scale8(delta, frac);
            result = (sbyte)(a + scaled);
        }
        else
        {
            byte delta = (byte)(a - b);
            byte scaled = Scale.scale8(delta, frac);
            result = (sbyte)(a - scaled);
        }
        return result;
    }

    public static short inoise16_raw(uint x, uint y)
    {
        // Find the unit cube containing the point
        byte X = (byte)(x >> 16);
        byte Y = (byte)(y >> 16);

        // Hash cube corner coordinates
        byte A = (byte)(P[X] + Y);
        byte AA = P[A];
        byte AB = P[A + 1];
        byte B = (byte)(P[X + 1] + Y);
        byte BA = P[B];
        byte BB = P[B + 1];

        // Get the relative position of the point in the cube
        ushort u = (ushort)(x & 0xFFFF);
        ushort v = (ushort)(y & 0xFFFF);

        // Get a signed version of the above for the grad function
        short xx = (short)((u >> 1) & 0x7FFF);
        short yy = (short)((v >> 1) & 0x7FFF);
        ushort N = 0x8000;

        u = ease16InOutQuad(u);
        v = ease16InOutQuad(v);

        short X1 = lerp15by16(grad16(P[AA], xx, yy), grad16(P[BA], (short)(xx - N), yy), u);
        short X2 = lerp15by16(grad16(P[AB], xx, (short)(yy - N)), grad16(P[BB], (short)(xx - N), (short)(yy - N)), u);

        short ans = lerp15by16(X1,X2,v);

        return ans;
    }

    public static ushort inoise16(uint x, uint y)
    {
        int ans = inoise16_raw(x, y);
        ans += 17308;
        uint pan = (uint)ans;
        // pan = (ans * 242L) >> 7.  That's the same as:
        // pan = (ans * 484L) >> 8.  And this way avoids a 7X four-byte shift-loop on AVR.
        // Identical math, except for the highest bit, which we don't care about anyway,
        // since we're returning the 'middle' 16 out of a 32-bit value anyway.
        pan *= 484;
        return (ushort)(pan >> 8);

        // return (uint32_t)(((int32_t)inoise16_raw(x,y)+(uint32_t)17308)*242)>>7;
        // return scale16by8(inoise16_raw(x,y)+17308,242)<<1;
    }
    
    public static ushort ease16InOutQuad(ushort i)
    {
        ushort j = i;
        if ((j & 0x8000) != 0)
        {
            j = (ushort)(65535 - j);
        }
        ushort jj = Scale.scale16(j, j);
        ushort jj2 = (ushort)(jj << 1);
        if ((i & 0x8000) != 0)
        {
            jj2 = (ushort)(65535 - jj2);
        }
        return jj2;
    }

    
    public static short lerp15by16(short a, short b, ushort frac)
    {
        short result;
        if (b > a)
        {
            ushort delta = (ushort)(b - a);
            ushort scaled = Scale.scale16(delta, frac);
            result = (short)(a + scaled);
        }
        else
        {
            ushort delta = (ushort)(a - b);
            ushort scaled = Scale.scale16(delta, frac);
            result = (short)(a - scaled);
        }
        return result;
    }


    public static short grad16(byte hash, short x, short y)
    {
        hash = (byte)(hash & 7);
        short u;
        short v;
        if (hash < 4)
        {
            u = x;
            v = y;
        }
        else
        {
            u = y;
            v = x;
        }
        if ((hash & 1) != 0)
        {
            u = (short)(-u);
        }
        if ((hash & 2) != 0)
        {
            v = (short)(-v);
        }

        return (short)((u+v) >> 1);
    }

    
    
    public static byte ease8InOutQuad(byte i)
    {
        byte j = i;
        if ((j & 0x80) != 0)
        {
            j = (byte)(255 - j);
        }
        byte jj = Scale.scale8(j, j);
        byte jj2 = (byte)(jj << 1);
        if ((i & 0x80) != 0)
        {
            jj2 = (byte)(255 - jj2);
        }
        return jj2;
    }

    public static byte inoise8(ushort x, ushort y, ushort z)
    {
        //return scale8(76+(inoise8_raw(x,y,z)),215)<<1;
        sbyte n = inoise8_raw(x, y, z); // -64..+64
        n += 64; //   0..128
        byte ans = Math8.qadd8(n, n); //   0..255
        return ans;
    }
    
    private static sbyte grad8(byte hash, sbyte x, sbyte y, sbyte z)
    {

        hash &= 0xF;

        sbyte u;
        sbyte v;
        //u = (hash&8)?y:x;
        u = ((hash & (1<<3)) > 0 /* TODO */) ? y : x;
        
        v = hash < 4?y:hash == 12 || hash == 14?x:z;

        if ((hash & 1) != 0)
        {
            u = (sbyte)-u;
        }
        if ((hash & 2) != 0)
        {
            v = (sbyte)-v;
        }

        return Math8.avg7(u,v);
    }

    private static sbyte inoise8_raw(ushort x, ushort y)
    {
        // Find the unit cube containing the point
        byte X = (byte)(x >> 8);
        byte Y = (byte)(y >> 8);

        // Hash cube corner coordinates
        byte A = (byte)(P[X] + Y);
        byte AA = P[A];
        byte AB = P[A + 1];
        byte B = (byte)(P[X + 1] + Y);
        byte BA = P[B];
        byte BB = P[B + 1];

        // Get the relative position of the point in the cube
        byte u = (byte)x;
        byte v = (byte)y;

        // Get a signed version of the above for the grad function
        sbyte xx = (sbyte)(((byte)(x) >> 1) & 0x7F);
        sbyte yy = (sbyte)(((byte)(y) >> 1) & 0x7F);
        byte N = 0x80;

        u = ease8InOutQuad(u);
        v = ease8InOutQuad(v);

        sbyte X1 = lerp7by8(grad8(P[AA], xx, yy), grad8(P[BA], (sbyte)(xx - N), yy), u);
        sbyte X2 = lerp7by8(grad8(P[AB], xx, (sbyte)(yy - N)), grad8(P[BB], (sbyte)(xx - N), (sbyte)(yy - N)), u);

        sbyte ans = lerp7by8(X1,X2,v);

        return ans;
        // return scale8((70+(ans)),234)<<1;
    }
    
    public static byte inoise8(ushort x, ushort y)
    {
        //return scale8(69+inoise8_raw(x,y),237)<<1;
        sbyte n = inoise8_raw(x, y); // -64..+64
        n += 64; //   0..128
        byte ans = Math8.qadd8(n, n); //   0..255
        return ans;
    }

    public static sbyte grad8(byte hash, sbyte x, sbyte y)
    {
        // since the tests below can be done bit-wise on the bottom
        // three bits, there's no need to mask off the higher bits
        //  hash = hash & 7;

        sbyte u;
        sbyte v;
        if ((hash & 4) != 0)
        {
            u = y;
            v = x;
        }
        else
        {
            u = x;
            v = y;
        }

        if ((hash & 1) != 0)
        {
            u = (sbyte)-u;
        }
        if ((hash & 2) != 0)
        {
            v = (sbyte)-v;
        }

        return Math8.avg7(u,v);
    }

    
}