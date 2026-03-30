using System.Collections;

namespace VN.Core
{
    public interface IVNCommand
    {
        IEnumerator Execute(CommandContext context);
    }
}