namespace Dvr.Commands.AdHoc;

public class DvrCommandIdAttribute : Attribute
{
    public static ushort? GetCommandId<T>() =>
        typeof(T)
            .GetCustomAttributes(typeof(DvrCommandIdAttribute), false)
            .OfType<DvrCommandIdAttribute>()
            .SingleOrDefault()?
            .CommandId;

    public DvrCommandIdAttribute(ushort commandId)
    {
        CommandId = commandId;
    }

    public ushort CommandId { get; set; }
}