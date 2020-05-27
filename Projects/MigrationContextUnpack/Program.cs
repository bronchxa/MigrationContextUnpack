using MigrationContextUnpack.Sources;
using System;

namespace MigrationContextUnpack
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.Exit(UnpackHandler.Unpack(args[0], args[1]));
        }
    }
}
