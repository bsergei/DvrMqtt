using System.Text.Json.Serialization;
using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1000)]
public class LoginRequest : IDvrRequest<LoginReply>
{
    public LoginRequest()
    {
        EncryptType = "MD5";
        LoginType = "DVRIP-Web";
    }

    [JsonIgnore]
    public string? SessionID { get; set; }

    [JsonPropertyName("EncryptType")]
    public string EncryptType { get; set; }

    [JsonPropertyName("LoginType")]
    public string LoginType { get; set; }

    [JsonPropertyName("PassWord")]
    public string? PassWord { get; set; }

    [JsonPropertyName("UserName")]
    public string? UserName { get; set; }
}