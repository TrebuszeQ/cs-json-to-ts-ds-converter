using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Formats.Asn1;
using System.Net;
using System.Net.Mime;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Diagnostics;

namespace CsClassToTsConverter;

public class JsonConverter
{
    private string? FileName { get; set; }
    private string Path { get; set; }
    private bool? FileExists { get; set; }
    private bool? IsJson { get; set; }
    private Dictionary<string, string>? FileCache { get; set; }
    private string Content { get; set; }
    private int ObjectsCount { get; set; }
    private int TreeIndex { get; set; } = 0;
    private TsClass? CurrentObject { get; set; } = null;
    private TsClass? PreviousObject { get; set; } = null;

    public JsonConverter(string path)
    {
        Path = path;
        FileName = SeparateFileName(path);
        Console.WriteLine("Extension: " + System.IO.Path.GetExtension(FileName));
        Debug.WriteLine("Extension: " + System.IO.Path.GetExtension(FileName));
        try
        {
            FileExists = System.IO.Path.Exists(path) ? true : throw new ArgumentException("Path is not accessible or doesn't exist.");
            IsJson = System.IO.Path.GetExtension(FileName) == ".json" ? true : throw new ArgumentException("The file is not a valid JSON file");
        }
        catch (ArgumentException e)
        {
            throw e;
        }
        if (FileCache == null) FileCache = InitCache();
        AccessCacheValue();
        ObjectsCount = CountObjects();
        CurrentObject = new TsClass("root", TsType.Object, Content![1..^1], null);
    }


    // separates FileName from path.
    private string SeparateFileName(string path)
    {
        int closing = path.LastIndexOf('/');
        if (closing == -1) closing = path.LastIndexOf('\\');
        return path.Substring(closing + 1, path.Length - closing - 1);
    }


    // reads file to string asynchronously and returns it or throws exception
    private static async Task<string> TryReadFile(string path) => await File.ReadAllTextAsync(path) ?? throw new Exception("File could not been read.");


    // returns cache file or if it's different it reads file and saves string to cache. 
    private Dictionary<string, string> InitCache() => new Dictionary<string, string> { { Path!, Content } };


    // populates Content with cache value or reads file and saves string to cache then returns string
    private async void AccessCacheValue()
    {
        bool truth = FileCache!.ContainsKey(key: Path);
        if (truth) Content = FileCache[key: Path];
        else if(!truth)
        {
            string content = await TryReadFile(Path);
            Debug.WriteLine(content.Length);
            Console.WriteLine(content);
            content = content.Trim();
            FileCache.Add(key: Path, value: content);
            Content = content;
        }
    }


    private int CountObjects()
    {
        int counter = 0;
        foreach (char chara in Content)
        {
            if (chara == '{') counter++;
        }

        return counter;
    }


    // traverses through objects value, if successfull it returns true, if not it throws exception.
    // when it ReturnObject() returns Object or Array it calls to switch Objects and sets relation.
    private bool TraverseObjects()
    {
        for (int i = TreeIndex; i < ObjectsCount; i++)
        {
            string value = CurrentObject!.GetValue();
            Content = value;
            TraverseValue();
        }
        return true;
    }


    // traverses through value variable of an object. If array or obj is found it switches to this object.
    private bool TraverseValue()
    {
        if (CurrentObject != null)
        {
            string value = CurrentObject.GetValue();
            for (int c = 0; c < value.Length; c++)
            {
                TsClass obj = SeparateValueObjects();
                if (CurrentObject.IsChildPresent(obj) == false)
                {
                    if (obj.GetDType() == TsType.Object || obj.GetDType() == TsType.Array)
                    {
                        SwitchObjects(obj);
                        TraverseValue();
                    }
                    else CurrentObject!.SetChild(obj);
                }
            }
            return true;
        }
        else throw new NullReferenceException("Value of value variable is null.");
    }


    // separates objects from value of the current object 
    private TsClass SeparateValueObjects()
    {
        // 1
        int opening = Content.IndexOf("\"");
        int closing = Content.IndexOf(":");
        string fieldName = Content.Substring(opening, closing - opening);
        string content = Content.Substring(closing + 1, Content.Length - closing - 1);
        Content = content;

        // 2
        opening = Content.IndexOf("\"");
        closing = Content.IndexOf(",");
        string? value;

        if (opening > closing) value = Content[..closing];
        else value = Content[..opening];
        

        string? dataType = TsType.Undefined;
        int i;
        // 3
        opening = Content.IndexOf(":");
        for (i = 0; i < value.Length; i++)
        {
            char chara = value[i];
            switch (chara)
            {
                case '{':
                    dataType = TsType.Object;
                    closing = Content.IndexOf("},{");
                    break;
        
                case '[':
                    dataType = TsType.Array;
                    closing = Content.IndexOf("}]");
                    break;

                case '"':
                    dataType = TsType.String;
                    closing = Content.IndexOf(",");
                    break;

                case 'f':
                case 't':
                    dataType = TsType.Boolean;
                    closing = Content.IndexOf(",");
                    break;

                default:
                    if (char.IsDigit(chara))
                    {
                        dataType = TsType.Number;
                        closing = Content.IndexOf(",");
                    }
                    break;
            }
        }

        value = Content.Substring(i + 1, closing - i);
        return  new(fieldName.Trim('"'), dataType, value, null);
    }


    // sets CurrentObject and PreviousObject
    private bool SwitchObjects(TsClass obj)
    {
        if (CurrentObject != null)
        {
            PreviousObject = CurrentObject;
            PreviousObject!.SetChild(obj);
            CurrentObject = obj;
            CurrentObject!.SetParent(PreviousObject);
            return true;
        }
        else throw new NullReferenceException("CurrentObject is null."); 
    }
}