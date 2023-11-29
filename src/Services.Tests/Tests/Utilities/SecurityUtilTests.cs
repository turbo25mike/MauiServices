namespace Turbo.Maui.Services.Tests;

public class SecurityUtilTests
{
    [Fact]
    public void E2E_UnitTest()
    {
        //**************** First section is what to do for initial access -- no secret guid

        var reg1000Resp = MockReadReg1000(); //expected response $R4=UINT32
        var C1_AlphaCode = uint.Parse(reg1000Resp);

        Console.WriteLine($"Present Rangefinder code to user: {SecurityUtil.GetAlphaCode(C1_AlphaCode)}");

        uint C3_UserInput = 123; // 3 digit display code   rangefinder has this, and user types it in
        var V1 = SecurityUtil.Encrypt(C3_UserInput, C1_AlphaCode);

        reg1000Resp = MockWriteReg1000(V1); // send V1 to Rangefinder and get back GUID
        var responseToken = new Guid(reg1000Resp); // Was notified with $K=e872f47a-e462-d05a-5ac4-42c832b43aa0

        var storedToken =
            SecurityUtil.Decrypt(responseToken,
                C3_UserInput); // Now decode and store this away...it is the real access guid

        //Store Guid with device details

        // *************** Now the portion if we are connecting with the code already

        var reg1003Resp = MockReadReg1003(); //read reg 1003 and that returns the challenge word
        var challenge = uint.Parse(reg1003Resp);
        var R1 = SecurityUtil.Encrypt(challenge, storedToken);

        MockWriteReg1003(R1);
    }

    private string MockReadReg1000()
    {
        byte
            alphaCode = (6 << 4) |
                        7; // high nibble is 6, refers to "P" above, and low nibble is 7, which refers to "S" above
        // top 24 bits are a random number and the lower 8 encode the 2 alpha characters
        var reg1000 = 0x11223300U | alphaCode;
        return reg1000.ToString();
    }

    private string MockWriteReg1000(uint v1)
    {
        // Rangefinder checks if v1 is the proper hash crc32 of the code and the 
        uint C3 = 123;
        var reg1000 = uint.Parse(MockReadReg1000());
        var ok = v1 == SecurityUtil.Encrypt(C3, reg1000);
        return ok ? "e872f47a-e462-d05a-5ac4-42c832b43aa0" : "";
    }

    private string MockReadReg1003()
    {
        return "12345678";
    }

    private string MockWriteReg1003(uint v1)
    {
        Console.WriteLine("Write 0x" + v1.ToString("X") + " to reg 1003 to get access");
        return "ok";
    }
}