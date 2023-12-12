using System.Text;

namespace BluetoothService.Example.Utilities;

public static class Radix64Util
{
    #region Radix64 Encode/Decode Methods

    // Takes a unsigned int of various sizes, which is expressed in bits.  Valid sizes are 8,12,16 and 32 bits
    // A string is created and returned with the Radix64 value, with MSW first.
    public static string Radix64EncodeValue(int bits, uint x)
    {
        byte b0, b1, b2, b3;
        var sb = new StringBuilder();
        switch (bits)
        {
            case 8:
                b0 = (byte)(x & 0xFF);
                sb.Append(ToRadix64(b0 >> 6));
                sb.Append(ToRadix64(b0 & 0x3F));
                break;
            case 12:
                b0 = (byte)(x & 0xFF);
                b1 = (byte)((x >> 8) & 0xFF);
                sb.Append(ToRadix64(((b1 & 0xF) << 2) | (b0 >> 6)));
                sb.Append(ToRadix64(b0 & 0x3F));
                break;
            case 16:
                b0 = (byte)(x & 0xFF);
                b1 = (byte)((x >> 8) & 0xFF);

                sb.Append(ToRadix64(b1 >> 4));
                sb.Append(ToRadix64(((b1 & 0xF) << 2) | (b0 >> 6)));
                sb.Append(ToRadix64(b0 & 0x3F));
                break;
            case 32:
                b0 = (byte)(x & 0xFF);
                b1 = (byte)((x >> 8) & 0xFF);
                b2 = (byte)((x >> 16) & 0xFF);
                b3 = (byte)((x >> 24) & 0xFF);

                sb.Append(ToRadix64(b3 >> 6));
                sb.Append(ToRadix64(b3 & 0x3f));
                sb.Append(ToRadix64(b2 >> 2));
                sb.Append(ToRadix64((b1 >> 4) | ((b2 & 0x3) << 4)));
                sb.Append(ToRadix64(((b1 & 0xF) << 2) | (b0 >> 6)));
                sb.Append(ToRadix64(b0 & 0x3F));
                break;
        }

        return sb.ToString();
    }


    // Takes a unsigned int of various sizes, which is expressed in bits.  Valid sizes are 8,12,16 and 32 bits
    // A string is created and returned with the Radix64 value, with MSW first.
    // retCode (if space available) is encoded in the first character
    public static string Radix64EncodeValue_WithReturnCode(int bits, uint x, byte retCode)
    {
        byte b0, b1, b2, b3;
        var sb = new StringBuilder();
        switch (bits)
        {
            case 8:
                b0 = (byte)(x & 0xFF);
                sb.Append(ToRadix64(
                    (b0 >> 6) | ((retCode & 0xF) << 2)
                ));
                sb.Append(ToRadix64(b0 & 0x3F));
                break;
            case 12:
                b0 = (byte)(x & 0xFF);
                b1 = (byte)((x >> 8) & 0xFF);
                sb.Append(ToRadix64(((b1 & 0xF) << 2) | (b0 >> 6)));
                sb.Append(ToRadix64(b0 & 0x3F));
                break;
            case 16:
                b0 = (byte)(x & 0xFF);
                b1 = (byte)((x >> 8) & 0xFF);

                sb.Append(ToRadix64(
                    (b1 >> 4) | ((retCode & 0x3) << 4)
                ));
                sb.Append(ToRadix64(((b1 & 0xF) << 2) | (b0 >> 6)));
                sb.Append(ToRadix64(b0 & 0x3F));
                break;
            case 32:
                b0 = (byte)(x & 0xFF);
                b1 = (byte)((x >> 8) & 0xFF);
                b2 = (byte)((x >> 16) & 0xFF);
                b3 = (byte)((x >> 24) & 0xFF);

                sb.Append(ToRadix64(
                    (b3 >> 6) | ((retCode & 0xF) << 2)
                ));
                sb.Append(ToRadix64(b3 & 0x3f));
                sb.Append(ToRadix64(b2 >> 2));
                sb.Append(ToRadix64((b1 >> 4) | ((b2 & 0x3) << 4)));
                sb.Append(ToRadix64(((b1 & 0xF) << 2) | (b0 >> 6)));
                sb.Append(ToRadix64(b0 & 0x3F));
                break;
        }

        return sb.ToString();
    }


    // Decode a Radix 64 value that is one of the following sizes:
    // 8 bits (2 bytes)
    // 12 bits (2 bytes)
    // 16 bits (3 bytes)
    // 32 bits (6 bytes)
    // The bytes are already have been translated to values 0-63, and now they are assembled here using bitwise operations.
    // An Radix64 array is passed in and the 'pos' index into the array points to the MSW of the incoming Radix 64 word.
    // Updates the position in the array at the end of the function to point to the next value (pos)
    // Any extra bits are returned that preceed the MSW -- This is where the return code is stored for reads/writes
    public static uint Radix64DecodeValue(int bits, ref byte[] a, ref int pos, out uint extraBits)
    {
        uint value = 0;
        switch (bits)
        {
            case 8:
                value |= (uint)(a[pos + 1] | ((a[pos] << 6) & 0xff));
                extraBits = (uint)(a[pos] >> 2); // 4 extra bits
                pos += 2;
                break;
            case 12:
                value |= (uint)((a[pos + 1] | ((a[pos] << 6) & 0xff)) << 0);
                value |= (uint)((a[pos] >> 2) << 8);
                extraBits = 0; // no extra bits
                pos += 2;
                break;
            case 16:
                value |= (uint)((a[pos + 2] | ((a[pos + 1] << 6) & 0xff)) << 0);
                value |= (uint)(((a[pos + 1] >> 2) | ((a[pos] << 4) & 0xff)) << 8);
                extraBits = (uint)(a[pos] >> 4); // 2 extra bits
                pos += 3;
                break;
            case 32:
                value |= (uint)((a[pos + 5] | ((a[pos + 4] << 6) & 0xff)) << 0);
                value |= (uint)(((a[pos + 4] >> 2) | ((a[pos + 3] << 4) & 0xff)) << 8);
                value |= (uint)(((a[pos + 3] >> 4) | ((a[pos + 2] << 2) & 0xff)) << 16);
                value |= (uint)((a[pos + 1] | ((a[pos] << 6) & 0xff)) << 24);
                extraBits = (uint)(a[pos] >> 2); // 4 extra bits
                pos += 6;
                break;
            default:
                // default case is an error, TODO
                extraBits = 0;
                break;
        }

        return value;
    }

    public static byte[] ToByteArray(this string radixEncodedValue) => Encoding.ASCII.GetBytes(radixEncodedValue);

    // Translates a value from 0-63 into a character string using a the mapping array.
    public static string ToRadix64(int value) => ((char)Radix64Character[value]).ToString(); // 0 becomes '0', etc


    // Translates back a Radix64 byte back into an integer between 0 and 63.
    public static byte FromRadix64(byte value) => Radix64Value[value];

    // The 64 symbols used to map 6-bit numbers from 0 to 63 to an alphanumeric character
    private static readonly byte[] Radix64Character = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+-"u8.ToArray();
    private static readonly byte[] Radix64Value = new byte[256] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 62, 0, 63, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 0, 0, 0, 0, 0, 0, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

    #endregion

    #region Message formats

    // Computes the checksum for the string 's'
    private static string ComputeChecksum(string s)
    {
        // Generate checksum
        // Checksum is sum of all characters in the string not including the 4 character command or the checksum (least significant 6 bits)
        var bytes = Encoding.ASCII.GetBytes(s);
        var sum = 0;
        for (var i = 3; i < bytes.Length; i++)
            sum += bytes[i];
        return ToRadix64(sum & 0x3F);
    }

    public static bool VerifyChecksum(string s)
    {
        var len = s.Length;

        // Generate checksum, check that it matches the checksum on the last byte of the string
        // Checksum is sum of all characters in the string except the last byte
        var bytes = Encoding.ASCII.GetBytes(s);
        var sum = 0;
        for (var i = 0; i < len - 1; i++) // don't include the last byte (the checksum)
            sum += bytes[i];
        return FromRadix64(bytes[len - 1]) == (byte)(sum & 0x3F);
    }

    public static string Generate(uint[] registers, int sequenceNumber, uint[]? data = null) => data == null ? AssembleReadRequest("$R4", (byte)sequenceNumber, registers) : AssembleWriteRequest("$W4", (byte)sequenceNumber, registers, data, 32);

    public static string Generate(uint register, int sequenceNumber, uint? data) => data == null ? AssembleReadRequest(register, (byte)sequenceNumber) : AssembleWriteRequest(register, (uint)data, (byte)sequenceNumber);

    /*
     * Read up to 64 registers from the rangefinder.
     * 
     * Format: $RsQnxxyyzzR?
     * Where s is 1,2,4 (bytes)
     * Q is 6 bit client assigned value (for example, incrementing message counter) that will be returned in the output...not modified by rangefinder
     * n is 6 bit count of registers to read in this request
     * xx 12 bit register numbers
     * yy 12 bit register numbers
     * zz 12 bit register numbers
     * R is sum of everything between the "s" and the "R" (bottom 6 bits)
     * The length of this request is 4 + 2 + n*2 + 1 
     * 
     * Return from rangefinder format: $Rs=QnAAAAAABBBBBBCCCCCCDDDDDDR!
     * Where s is 1,2,4
     * Q is 6 bit client assigned msg counter that will be returned in the output
     * n is 6 bit count of registers to read in this request
     * length of return:
     * s=4: 4+2+n*6+1  
     * s=2: 4+2+n*3+1
     * s=1: 4+2+n*2+1
     */
    public static string AssembleReadRequest(string req, byte sequenceNumber, uint[] regs)
    {
        var sb = new StringBuilder();
        sb.Append(req);
        sb.Append(ToRadix64(sequenceNumber));
        sb.Append(ToRadix64(regs.Length));
        for (var i = 0; i < regs.Length; i++)
            sb.Append(Radix64EncodeValue(12, regs[i]));

        // adds a 6 bit checksum onto the end of the request
        var s = sb.ToString();
        return s + ComputeChecksum(s) + "?";
    }

    public static string AssembleReadRequest(uint register, byte sequenceNumber) => AssembleReadRequest("$R4", sequenceNumber, new[] { register });

    /*
     * Write up to 64 registers from the rangefinder.
     * 
     * request $WsQnxxXXXXXXyyYYYYYYzzZZZZZZR? 
     * Writes n registers where s = 1, 2, or 4 (bytes)
     * Q is 6 bit code sent by client and not modified by rangefinder
     * n is 6 bit number of registers to write
     * xx is 1st register number to write
     * XXXXXX is value of 1st register to write
     * yy is 2nd register number to write
     * YYYYYY is value of 2nd register to write
     * zz is 3rd register number to write
     * ZZZZZZ is value of 3rd register to write
     * R is checksum (sum of 
     * length of write request:
     * s=4: 4+2+n*(2+6)+1
     * s=2: 4+2+n*(2+3)+1
     * s=1: 4+2+n*(2+2)+1
     * 
     * Return format: $Ws=QnrrrR!
     * where s = 1, 2, or 4
     * Q is echo of sequence code the client sent
     * n is number of registers written
     * r is return code from each register write
     * R is sum of everything between the "=" and the "R" (bottom 6 bits)
     */
    public static string AssembleWriteRequest(string req, byte sequenceNumber, uint[] regs, uint[] data, int bits)
    {
        var sb = new StringBuilder();
        sb.Append(req);
        sb.Append(ToRadix64(sequenceNumber));
        sb.Append(ToRadix64(regs.Length));
        for (var i = 0; i < regs.Length; i++)
        {
            sb.Append(Radix64EncodeValue(12, regs[i]));
            sb.Append(Radix64EncodeValue(bits, data[i]));
        }

        // adds a 6 bit checksum onto the end of the request
        var s = sb.ToString();
        return s + ComputeChecksum(s) + "?";
    }

    public static string AssembleWriteRequest(uint register, uint data, byte sequenceNumber) => AssembleWriteRequest("$W4", sequenceNumber, new[] { register }, new[] { data }, 32);

    // This is example of the embedded processor processing for WRITES
    private static string AssembleWriteResponse(string req, byte sequenceNumber, byte[] retCode)
    {
        var sb = new StringBuilder();
        sb.Append(req);
        sb.Append(ToRadix64(sequenceNumber));
        sb.Append(ToRadix64(retCode.Length));
        for (var i = 0; i < retCode.Length; i++)
            sb.Append(ToRadix64(retCode[i]));


        // adds a 6 bit checksum onto the end of the request
        var s = sb.ToString();
        return s + ComputeChecksum(s);
    }

    // This is example of the embedded processor processing for READS
    private static string AssembleReadResponse(string req, byte sequenceNumber, uint[] data, byte[] retCode, int bits)
    {
        var sb = new StringBuilder();
        sb.Append(req);
        sb.Append(ToRadix64(sequenceNumber));
        sb.Append(ToRadix64(retCode.Length));
        for (var i = 0; i < retCode.Length; i++)
            sb.Append(Radix64EncodeValue_WithReturnCode(bits, data[i], retCode[i]));

        // adds a 6 bit checksum onto the end of the request
        var s = sb.ToString();
        return s + ComputeChecksum(s);
    }

    public static bool ParseReadResponse(string response, out byte sequenceNumber, out List<uint> data,
        out List<uint> returnCode, out int bits)
    {
        // assign output values
        sequenceNumber = 0;
        data = new List<uint>();
        returnCode = new List<uint>();
        bits = 0;

        var len = response.Length;

        if (len < 7)
            return false;

        // Compute checksum and translate the radix64 character set
        var bytes = Encoding.ASCII.GetBytes(response);
        var sum = 0;
        for (var i = 4; i < len - 1; i++)
        {
            sum += bytes[i];
            bytes[i] = Radix64Value[bytes[i]];
        }

        if ((sum & 0x3F) != Radix64Value[bytes[len - 1]])
            return false;

        // get number of bytes per register
        switch (response[2])
        {
            case '1':
                bits = 8;
                break;
            case '2':
                bits = 16;
                break;
            case '4':
                bits = 32;
                break;
            default:
                return false;
        }

        // start processing the array at the 4th character
        var pos = 4;
        sequenceNumber = bytes[pos++];

        // Get number of elements
        int n = bytes[pos++];

        for (var i = 0; i < n; i++)
        {
            data.Add(Radix64DecodeValue(bits, ref bytes, ref pos, out uint retCode));
            returnCode.Add(retCode);
        }

        return true;
    }

    public static bool ParseWriteResponse(string response, out byte sequenceNumber, out List<uint> returnCode)
    {
        // assign output values
        sequenceNumber = 0;
        returnCode = new List<uint>();

        var len = response.Length;

        if (len < 7)
            return false;

        // Compute checksum and translate the radix64 character set
        var bytes = Encoding.ASCII.GetBytes(response);
        var sum = 0;
        for (var i = 4; i < len - 1; i++)
        {
            sum += bytes[i];
            bytes[i] = Radix64Value[bytes[i]];
        }

        if ((sum & 0x3F) != Radix64Value[bytes[len - 1]])
            return false;

        // start processing the array at the 4th character
        var pos = 4;
        sequenceNumber = bytes[pos++];

        // Get number of elements
        int n = bytes[pos++];

        for (var i = 0; i < n; i++)
            returnCode.Add(bytes[pos++]);

        return true;
    }

    #endregion

    #region Run tests

    // Encoding and decode Radix64 1 byte payload
    public static bool test8()
    {
        const int testCount = 10000;
        var r = new Random(0);

        for (var i = 0; i < testCount; i++)
        {
            var n = (uint)r.Next(0, 255);

            // encode
            var s = Radix64EncodeValue(8, n);

            // decode
            var bytes = Encoding.ASCII.GetBytes(s);
            for (var k = 0; k < bytes.Length; k++)
                bytes[k] = FromRadix64(bytes[k]);
            var pos = 0;
            uint extraBits;
            var n2 = Radix64DecodeValue(8, ref bytes, ref pos, out extraBits);

            if (n != n2)
                return false;
        }

        return true;
    }

    // Encoding and decode Radix64 register size (12 bit) payload
    public static bool test12()
    {
        const int testCount = 10000;
        var r = new Random(0);

        for (var i = 0; i < testCount; i++)
        {
            var n = (uint)r.Next(0, 4096);

            // encode
            var s = Radix64EncodeValue(12, n);

            // decode
            var bytes = Encoding.ASCII.GetBytes(s);
            for (var k = 0; k < bytes.Length; k++)
                bytes[k] = FromRadix64(bytes[k]);
            var pos = 0;
            uint extraBits;
            var n2 = Radix64DecodeValue(12, ref bytes, ref pos, out extraBits);

            if (n != n2)
                return false;
        }

        return true;
    }

    // Encoding and decode Radix64 2 byte payload
    public static bool test16()
    {
        const int testCount = 10000;
        var r = new Random(0);

        for (var i = 0; i < testCount; i++)
        {
            var n = (uint)r.Next(0, 65536);

            // encode
            var s = Radix64EncodeValue(16, n);

            // decode
            var bytes = Encoding.ASCII.GetBytes(s);
            for (var k = 0; k < bytes.Length; k++)
                bytes[k] = FromRadix64(bytes[k]);
            var pos = 0;
            uint extraBits;
            var n2 = Radix64DecodeValue(16, ref bytes, ref pos, out extraBits);

            if (n != n2)
                return false;
        }

        return true;
    }

    // Encoding and decode Radix64 4 byte payload
    public static bool test32()
    {
        const int testCount = 10000;
        var r = new Random(0);

        for (var i = 0; i < testCount; i++)
        {
            var lsw = (uint)r.Next(0, 65536);
            var msw = (uint)r.Next(0, 65536);
            var n = (msw << 16) | lsw;

            // encode
            var s = Radix64EncodeValue(32, n);

            // decode
            var bytes = Encoding.ASCII.GetBytes(s);
            for (var k = 0; k < bytes.Length; k++)
                bytes[k] = FromRadix64(bytes[k]);
            uint extraBits;
            var pos = 0;
            var n2 = Radix64DecodeValue(32, ref bytes, ref pos, out extraBits);

            if (n != n2)
                return false;
        }

        return true;
    }

    public static bool RunAllTests()
    {
        if (!test8()) return false;
        if (!test12()) return false;
        if (!test16()) return false;
        if (!test32()) return false;

        var readRegisterIDs = new uint[] { 1, 3, 5, 7, 9 };
        var readRegisterValues = new uint[] { 0, 100, 1000, 100000, 10000000, 1 };
        var readRegisterReturnCode = new byte[] { 0, 2, 0, 10, 0, 1 };

        var readReq4 = AssembleReadRequest("$R4?", 0, readRegisterIDs);
        Console.WriteLine(readReq4);

        // Create a pseudo response from the rangefinder and have it parsed on the mobile
        var readResponse = AssembleReadResponse("$R4=", 0, readRegisterValues, readRegisterReturnCode, 32);
        Console.WriteLine(readResponse);
        List<uint> data;
        int bits;
        byte seqNumber = 0;
        List<uint> retCode;
        if (!ParseReadResponse(readResponse, out seqNumber, out data, out retCode, out bits))
            return false;

        // check for proper parsing
        for (var i = 0; i < readRegisterIDs.Length; i++)
        {
            if (readRegisterValues[i] != data[i])
                return false;

            if (readRegisterReturnCode[i] != retCode[i])
                return false;
        }


        var writeReq1 =
            AssembleWriteRequest("$W1=", 1, new uint[] { 10, 11, 12, 13 }, new uint[] { 1, 2, 20, 200, 255 }, 8);
        Console.WriteLine(writeReq1);

        var writeReq2 = AssembleWriteRequest("$W2=", 2, new uint[] { 6, 7, 8, 9 },
            new uint[] { 0, 1000, 10000, 20000, 1 }, 16);
        Console.WriteLine(writeReq2);

        var writeReq4 = AssembleWriteRequest("$W4=", 3, new uint[] { 1, 2, 3, 4, 5 },
            new uint[] { 0, 1000, 100000, 10000000, 1 }, 32);
        Console.WriteLine(writeReq4);

        var writeRegisterReturnCode = new byte[] { 0, 0, 1, 0, 2 };
        // Use case...Create a pseudo response from the rangefinder and have it parsed on the mobile
        var writeResponse = AssembleWriteResponse("$W4=", 3, writeRegisterReturnCode);
        Console.WriteLine(writeResponse);
        if (!ParseWriteResponse(writeResponse, out seqNumber, out retCode))
            return false;

        // check for proper parsing
        for (var i = 0; i < writeRegisterReturnCode.Length; i++)
            if (writeRegisterReturnCode[i] != retCode[i])
                return false;

        // all passed
        return true;
    }

    #endregion
}