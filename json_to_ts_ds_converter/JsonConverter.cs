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
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace CsClassToTsConverter;

public class JsonConverter
{
    private string? FileName { get; set; }
    private string Path { get; set; }
    private bool? FileExists { get; set; }
    private bool? IsJson { get; set; }
    private Dictionary<string, string>? FileCache { get; set; } = new Dictionary<string, string>();
    private string Content { get; set; }
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
        AccessCacheValue(path);
        CurrentObject = new TsClass("root", TsType.Object, Content![1..^1], null);
    }


    // separates FileName from path.
    private string SeparateFileName(string path)
    {
        int closing = path.LastIndexOf('/');
        if (closing == -1) closing = path.LastIndexOf('\\');
        return path.Substring(closing + 1, path.Length - closing - 1);
    }


    // populates Content with cache value or reads file and saves string to cache then returns string
    // populates Content with cache value or reads file and saves string to cache then returns string
    private bool AccessCacheValue(string path)
    {
        bool truth = FileCache!.ContainsKey(key: Path);
        if (truth) Content = FileCache[key: Path];
        else if (!truth)
        {
            string content = File.ReadAllText(path, Encoding.UTF8) ?? throw new NullReferenceException("File is empty, or it coudln't been read. Content is null.");
            content = content.Replace(" ", "").Replace("\n", "").Replace("\r", "").Replace("\t", "").Replace("\\", "");
            FileCache.Add(key: Path, value: content);
            Content = content;
        }
        return true;
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
    public bool TraverseObjects()
    {
        int objectsCount = CountObjects();
        for (int i = TreeIndex; i < objectsCount; i++)
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
        int trimEnd = closing - opening;
        string fieldName = Content.Substring(opening, trimEnd);
        trimEnd = Content.Length - closing - 1;
        string content = Content.Substring(closing + 1, trimEnd);
        Content = content;

        // 2
        opening = Content.IndexOf("\"");
        closing = Content.IndexOf(",");
        string? value;

        if (opening > closing) value = Content[..closing];
        else value = Content[..opening];
        

        string dataType = TsType.Undefined;
        int i;
        // 3
        opening = Content.IndexOf(":");
        for (i = 0; i < value.Length && dataType == TsType.Undefined; i++)
        {
            
            char chara = value[i];
            switch (chara)
            {
                case '[':
                    dataType = TsType.Array;
                    closing = Content.IndexOf("}],");
                    break;

                case '{':
                    dataType = TsType.Object;
                    closing = Content.IndexOf("}},");
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

        if (closing == -1) throw new Exception("Closing has not been found");
        value = Content.Substring(i + 1, closing);
        return  new(fieldName.Replace("\"", ""), dataType, value, null);
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