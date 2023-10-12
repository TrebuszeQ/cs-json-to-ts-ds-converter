// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Diagnostics.Tracing;
using CsClassToTsConverter;
using Microsoft.VisualBasic;

Console.WriteLine("JSON to Ts data structure converter");
InputLoop();


void InputLoop()
{
    JsonConverter? lol;
    do
    {
        Console.Clear();
        Console.WriteLine("Provide file path to read JSON from.");
        // string? input = Console.ReadLine();
        // test only
        string input = "/home/trebuszeq/Net/jsonconverter/json_to_ts_ds_converter/example.json";
        if (input == null) Console.WriteLine("Wrong input.");
        else
        {
            input = input.Trim();
            lol = new(input);
            break;
        }
    } while (true);
    Console.WriteLine("Success");
    lol.TraverseObjects();
    Console.WriteLine(lol.GetObject());
}
