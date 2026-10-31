using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace MindAttic.Components.Wpf.Controls;

/// <summary>Where <see cref="MaSecretBox"/> copies a secret. <see cref="MaSecretClipboard.System"/> is the real one;
/// tests pass a fake so they never touch the user's clipboard.</summary>
public interface IMaSecretClipboard
{
    /// <summary>Puts <paramref name="secret"/> on the clipboard and returns the clipboard's <see cref="Stamp"/> after it.</summary>
    long Put(string secret);

    /// <summary>A number that changes whenever anything changes the clipboard.</summary>
    long Stamp { get; }

    /// <summary>Empties the clipboard.</summary>
    void Clear();
}

/// <summary>The Windows clipboard for secrets (M.COMP-T155): every copy carries the formats that keep it out of
/// Windows clipboard history and cloud clipboard sync, and clipboard monitors are asked to skip it. Whether the
/// clipboard still holds a copy is told by the clipboard sequence number, so the secret itself is never kept to compare.</summary>
public sealed class MaSecretClipboard : IMaSecretClipboard
{
    /// <summary>Presence asks clipboard monitors (history, sync, managers) to ignore the content.</summary>
    public const string ExcludeFromMonitorProcessingFormat = "ExcludeClipboardContentFromMonitorProcessing";

    /// <summary>A DWORD 0 keeps the content out of Windows clipboard history (Win+V).</summary>
    public const string CanIncludeInClipboardHistoryFormat = "CanIncludeInClipboardHistory";

    /// <summary>A DWORD 0 keeps the content out of cloud clipboard sync across devices.</summary>
    public const string CanUploadToCloudClipboardFormat = "CanUploadToCloudClipboard";

    /// <summary>The Windows clipboard.</summary>
    public static MaSecretClipboard System { get; } = new();

    private MaSecretClipboard() { }

    /// <summary>The data object a copy puts on the clipboard: the text plus the three exclusion formats.</summary>
    public static DataObject CreateDataObject(string secret)
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, secret, false);
        data.SetData(ExcludeFromMonitorProcessingFormat, Dword(0), false);
        data.SetData(CanIncludeInClipboardHistoryFormat, Dword(0), false);
        data.SetData(CanUploadToCloudClipboardFormat, Dword(0), false);
        return data;
    }

    private static MemoryStream Dword(int value) => new(BitConverter.GetBytes(value));

    /// <inheritdoc />
    public long Put(string secret)
    {
        Clipboard.SetDataObject(CreateDataObject(secret), true);
        return Stamp;
    }

    /// <inheritdoc />
    public long Stamp => GetClipboardSequenceNumber();

    /// <inheritdoc />
    public void Clear() => Clipboard.Clear();

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();
}
