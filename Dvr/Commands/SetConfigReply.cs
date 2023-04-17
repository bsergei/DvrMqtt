using Dvr.Commands.AdHoc;

namespace Dvr.Commands;

[DvrCommandId(1041)]
public class SetConfigReply : IDvrReply
{
    public string? Name { get; set; }

    public int Ret { get; set; }

    public string? SessionID { get; set; }
}