namespace TcpMultiplexer.Server;

public static class CommandLineArgs
{
    public static int? GetIntArg(this string[] args, string name)
    {
        var ix = args.IndexOf(name);
        if (ix < 0 || ix + 1 >= args.Length) return null;
        if (int.TryParse(args[ix + 1], out var r))
            return r;
        return null;
    }

    public static bool ContainsAnyOfArgs(this string[] args, params string[] names)
    {

        foreach (var a in args)
        {
            foreach (var n in names)
            {
                if (n.Equals(a, StringComparison.CurrentCultureIgnoreCase))
                    return true;
            }
        }

        return false;
    }
    public static string? GetStringArg(this string[] args, string name)
    {
        var ix = args.IndexOf(name);
        if (ix < 0 || ix + 1 >= args.Length) return null;
        return args[ix+1];
    }

    private static int IndexOf(this string[] args, string search)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(search, StringComparison.CurrentCultureIgnoreCase))
                return i;
        }

        return -1;
    }
}