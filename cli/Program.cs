using System;
using System.Threading.Tasks;
using wpfapp;

namespace cli
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            await WpfAppUtils.ShowDialog();

            Console.WriteLine("Goodbye World!");
        }
    }
}
