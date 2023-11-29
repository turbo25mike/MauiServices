using System.IO.Hashing;
using System.Text;

namespace Turbo.Maui.Services.Utilities;

public static class SecurityUtil
{
    private const string _Lookup = "ACEFHLPSU";

    // Accepts 32 bit challenge from Rangefinder and a stored 16 byte GUID.   Combines the two with a hash and returns a 32 bit unsigned int
    public static uint Encrypt(uint challenge, Guid key)
    {
        // Generate a CRC32 that hashes 8 different combinations of the key (starting at different offsets)
        var guidBytes = key.ToByteArray();
        var bytes = new byte[guidBytes.Length *
                             8]; // should be 128 bytes since guid is 16 bytes and we have 8 versions of it

        uint pos = 0; // next byte position in the output array bytes[]
        for (var i = 0; i < sizeof(uint) * 2; i++) // move to each nibble in the challenge
        {
            var offset = challenge & 0xF; // start offset into the guid bytes
            for (var j = 0;
                 j < guidBytes.Length;
                 j++) // add in the guid starting at the offset determined by the challenge
                bytes[pos++] = guidBytes[(offset + j) % 16]; // copy the guid byte (with wraparound)
            challenge >>= 4; // move to the next nibble in the challenge
        }

        // Compute the response....simply a CRC32 over 128 bytes
        return Convert(Crc32.Hash(bytes));
    }

    // Combines the code seen in the display with random data "salt" sent by the Rangefinder
    // "password" is 3 digit code (100-999)
    // "salt" is value read from rangefinder
    public static uint Encrypt(uint password, uint salt)
    {
        var digits = Encoding.ASCII.GetBytes(password.ToString());
        var b = BitConverter.GetBytes(salt);
        var c = new Crc32();
        // 10 iterations of 7 bytes each = 70 bytes
        for (var i = 0; i < 10; i++)
        {
            c.Append(digits);
            c.Append(b);
        }

        // Compute the response....simply a CRC32 over 70 bytes
        return Convert(c.GetCurrentHash());
    }

    public static string GetAlphaCode(uint code)
    {
        var letter1 = _Lookup[((int)code >> 4) & 0xF]; // get first character
        var letter2 = _Lookup[(int)code & 0xF]; // get second character
        return $"{letter1}{letter2}";
    }

    public static uint Convert(byte[] c)
    {
        return BitConverter.ToUInt32(c, 0);
    }

    public static Guid Decrypt(Guid encodedGuid, uint C3)
    {
        var guidBytes = encodedGuid.ToByteArray();
        for (var i = 0; i < guidBytes.Length; i++)
            guidBytes[i] ^=
                (byte)((C3 * (i + 1)) &
                       0xFF); // xor each byte of the guid with a multiplied version of the 3 digit code
        return new Guid(guidBytes);
    }
}