﻿
using ProcessWrapper;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;

static class Program
{
    public static readonly HttpClient Client = new HttpClient();

    public static ConsoleWrapper ConsoleWrapper;

    static int Main(string[] args)
    {
        var arguments = string.Empty;
        var fileName = string.Empty;

        if (!ValidateArgs(args))
        {
            return -1;
        }
        if (args.Length == 0)
        {
            fileName = "java";
            arguments = "-jar server.jar nogui";
        }
        else if (args.Length == 1)
        {
            fileName = args[0];
        }
        else
        {
            fileName = args[0];
            arguments = args[1];
        }

        new MinecraftWrapper(fileName, arguments);

        return 0;
    }

    private static bool ValidateArgs(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Insufficient minimum arguments supplied");
            return false;
        }
        else if (args.Length > 2)
        {
            Console.WriteLine("Too many arguments");
            return false;
        }

        return true;
    }
}