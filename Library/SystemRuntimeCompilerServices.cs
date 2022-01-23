using System.ComponentModel;

/// <summary>
/// Workaround for compiler issues in Visual Studio
/// See: https://stackoverflow.com/a/62656145
/// </summary>

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public record IsExternalInit;
}
