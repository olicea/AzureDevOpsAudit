using System;

namespace auditlog
{
    class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine( Resources.Program_usage);
                return;
            }

            using (CommandLineApplication app = new CommandLineApplication(args))
            {
                app.RunAsync().Wait();
            }
        }
    }
}
