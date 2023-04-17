namespace Dvr;

public interface IDvrHttpWebService
{
    Task<byte[]> GetCameraSnapshot();
}