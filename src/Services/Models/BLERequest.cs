namespace Turbo.Maui.Services.Models;

public interface IBLERequest
{
    byte[] Data { get; }
    string ServiceID { get; }
    string CharacteristicID { get; }
    bool WithResponse { get; }
    byte[] Response { get; }
    bool SetResponse(byte[] responseData);
    void SetError(string error);
}

public class BLERequest : IBLERequest
{
    public BLERequest(string serviceID, string characteristicID, byte[] data, bool withResponse = false)
    {
        ServiceID = serviceID;
        CharacteristicID = characteristicID;
        Data = data;
        WithResponse = withResponse;
    }

    public bool SetResponse(byte[] responseData)
    {
        Response = responseData;
        return true;
    }

    public void SetError(string error)
    {
        WasSuccessful = false;
        Error = error;
    }

    public string ServiceID { get; private set; }
    public string CharacteristicID { get; private set; }
    public byte[] Data { get; private set; }
    public bool WithResponse { get; private set; }
    public byte[] Response { get; private set; }
    public bool WasSuccessful { get; private set; }
    public string Error { get; private set; }
}