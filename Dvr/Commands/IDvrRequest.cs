namespace Dvr.Commands;

public interface IDvrRequest
{
    string? SessionID { get; set; }
}

public interface IDvrRequest<out T> : IDvrRequest where T : IDvrReply
{
}