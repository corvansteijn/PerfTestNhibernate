using System;
using Xunit.Abstractions;

namespace PerfTestNhibernate
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var tests = new NHibernatePerfTests(XUnitConsole.Instance);
            tests.GetType().GetMethod(args[0]).Invoke(tests, null);
        }
    }

    public class XUnitConsole : ITestOutputHelper
    {
        public static readonly XUnitConsole Instance = new XUnitConsole();

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}