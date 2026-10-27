using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Wallhaven.Desktop;

internal sealed class ApiKeyVault(string root)
{
    private string FilePath => Path.Combine(root, "api-key.dpapi");
    public string Load() => File.Exists(FilePath) ? Encoding.UTF8.GetString(Transform(File.ReadAllBytes(FilePath), false)) : "";
    public void Save(string key)
    {
        var data = Transform(Encoding.UTF8.GetBytes(key), true);
        Directory.CreateDirectory(root);
        string temp = FilePath + ".tmp";
        File.WriteAllBytes(temp, data);
        File.Move(temp, FilePath, true);
    }
    public void Clear() { if (File.Exists(FilePath)) File.Delete(FilePath); }
    [StructLayout(LayoutKind.Sequential)] private struct Blob { public int Length; public IntPtr Pointer; }
    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(ref Blob input, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out Blob output);
    [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, uint flags, out Blob output);
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr pointer);
    private static byte[] Transform(byte[] data, bool encrypt)
    {
        var input = new Blob { Length = data.Length, Pointer = Marshal.AllocHGlobal(data.Length) };
        var output = new Blob();
        try
        {
            Marshal.Copy(data, 0, input.Pointer, data.Length);
            bool ok = encrypt ? CryptProtectData(ref input, "Wallhaven API Key", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
            if (!ok) throw new Win32Exception(Marshal.GetLastWin32Error(), "无法访问本机加密的 API Key。");
            var result = new byte[output.Length]; Marshal.Copy(output.Pointer, result, 0, result.Length);
            return result;
        }
        finally
        {
            for (int i = 0; i < input.Length; i++) Marshal.WriteByte(input.Pointer, i, 0);
            Marshal.FreeHGlobal(input.Pointer);
            if (output.Pointer != IntPtr.Zero) LocalFree(output.Pointer);
            Array.Clear(data);
        }
    }
}
