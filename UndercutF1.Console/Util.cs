using System.Text;

namespace UndercutF1.Console;

public static class Util
{
    public static string Sanitize(string str) => str.Replace("\u001B", "<ESC>").TrimEnd((char)0);

    public static string Sanitize(byte[] bytes) => Sanitize(Encoding.ASCII.GetString(bytes));
}
