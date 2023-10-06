// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Diagnostics.Tracing;
using CsClassToTsConverter;
using Microsoft.VisualBasic;

Console.WriteLine("JSON to Ts data structure converter");
InputLoop();


async void InputLoop()
{
    do
    {
        Console.Clear();
        Console.WriteLine("Provide file path to read JSON from.");
        string? input = Console.ReadLine();
        if (input == null) Console.WriteLine("Wrong input.");
        else
        {
            bool truth = CheckFileExist(input);
            if (truth)
            {
                truth = IsJson(input);
                if (truth)
                {
                    string content = await ReadFileToString(input);

                }
            }

        }
    } while (true);
}


bool CheckFileExist(string path)
{
    bool truth = File.Exists(path);
    if(truth) return true;
    Console.WriteLine("File doesn't exist");
    return false;
}

bool IsJson(string path)
{
    string extension = Path.GetExtension(path);
    if (extension == "json") return true;
    Console.WriteLine("File is not JSON file.");
    return false;
}

async Task<string> ReadFileToString(string path)
{
    return await File.ReadAllTextAsync(path);
}

string ValidateString(string content)
{
    int openingIndex = 0;
    int closingIndex = 0;
    int charIndex = 0;
    int[] indices;
    string keyword;
    string newString = content;
    char[] chars = { '{', '\"', '\"', ':', '[', ']', '}' };
    if (newString.Length < 10)
    {
        Console.WriteLine("File length is not sufficient.");
        return newString;
    }
    
    for (int i = 0; i < newString.Length; i++)
    {
        indices = FindIndexOfChar(content);
        if (!indices.Contains(-1))
        {
            openingIndex = indices[0];
            charIndex = chars[indices[1]];
            closingIndex = content.LastIndexOf(chars[indices[1]]);    
        }
        
    }
    return null;
}


int[] FindIndexOfChar(string content)
{
    int[] result = { -1, -1 };
    char[] chars = { '{', '\"', '\"', ':', '[', ']', '}' };
    for (int i = 0; i < content.Length; i++)
    {
        for  (int j = 0; j < chars.Length; j++)
        {
            if (content[i] == chars[j]) return new []{ i, j };
        }
    }
    
    return result;
}


// returns index of first occurrence of opening
int FindOpening(string content)
{
    return content.IndexOf("{", StringComparison.Ordinal);
}


// returns index of last occurence of closing
int FindClosing(string content)
{
    return content.LastIndexOf(("}"), StringComparison.Ordinal);
}


// 
string FindKeywords(string content)
{
    string? keyword;
    int opening = content.IndexOf("\"", StringComparison.Ordinal);
    string substring = content.Substring(opening + 1, content.Length);
    int closing = substring.IndexOf("\"", StringComparison.Ordinal);
    keyword = substring.Substring(opening, closing - opening);
    if (keyword.Length > 0) return keyword; 
    throw new Exception("Can't return keyword because it is missing.");
    
}
