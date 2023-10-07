// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Diagnostics.Tracing;
using CsClassToTsConverter;
using Microsoft.VisualBasic;

Console.WriteLine("JSON to Ts data structure converter");
InputLoop();


void InputLoop()
{
    do
    {
        Console.Clear();
        Console.WriteLine("Provide file path to read JSON from.");
        string? input = Console.ReadLine();
        if (input == null) Console.WriteLine("Wrong input.");
        else
        {
            JsonConverter lol = new(input.Trim());
        }
    } while (true);
}
