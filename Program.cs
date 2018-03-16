using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;

namespace CSharpToArduino
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = @"d:\data\electronics\Blink CSharp\Blink.cs";
            filename = @"d:\data\electronics\Blink CSharp\Fade.cs";
            filename = @"d:\data\electronics\Blink CSharp\DigitalReadSerial.cs";
            filename = @"d:\data\electronics\Blink CSharp\Debounce.cs";
            filename = @"d:\data\electronics\Blink CSharp\LoveOMeter.cs";

            Outputter output = new CodeConverter().Convert(filename);

            foreach (string line in output.Lines)
            {
                Console.WriteLine(line);
            }
        }
    }
}


