using System;
using System.Reflection;

class Test {
    static void Main() {
        var asm = Assembly.LoadFile(System.IO.Path.GetFullPath("bin/Debug/Clash SL Server.exe"));
        foreach (var r in asm.GetManifestResourceNames()) {
            Console.WriteLine("RESOURCE: " + r);
        }
    }
}
