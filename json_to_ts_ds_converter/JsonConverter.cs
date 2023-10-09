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
using System.Security.AccessControl;

namespace CsClassToTsConverter;

public class JsonConverter
{
    private string? FileName;
    private string Path { get; set; }
    private bool? FileExists;
    private bool? IsJson;
    private Dictionary<string, string>? FileCache = new Dictionary<string, string>();
    private string Content;
    private int TreeIndex = 0;
    private TsClass? CurrentObject = null;
    private TsClass? Root = null;
    private TsClass? PreviousObject = null;

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
        Root = CurrentObject;
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
            if (chara == '{' || chara == '[') counter++;
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
            TraverseValue();
            TreeIndex = i;
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
                TsClass obj = SeparateObjectValues(value);
                if (CurrentObject.IsChildPresent(obj) == false)
                {
                    CurrentObject!.SetChild(obj);
                    if (obj.GetDType() == TsType.Object || obj.GetDType() == TsType.Array)
                    {
                        SwitchObjects(obj);
                        TraverseValue();
                    }
                }
            }
            return true;
        }
        else throw new NullReferenceException("Value of value variable is null.");
    }


    // separates objects from value of the current object 
    private TsClass SeparateObjectValues(string oldValue)
    {
        // 1
        int opening = oldValue.IndexOf("\"");
        int closing = oldValue.IndexOf(":");
        int trimEnd = closing - opening;
        string fieldName = oldValue.Substring(opening, trimEnd);
        trimEnd = oldValue.Length - closing - 1;
        string value = oldValue.Substring(closing + 1, trimEnd);

        // 2
        opening = value.IndexOf("\"");
        closing = value.IndexOf(",");
        string? newValue;
        int i = opening;
        if(opening == -1) 
        {
            newValue = oldValue;
            i = 0;
        }
        else if (opening > closing) 
        {
            closing += 1;
            newValue = value[..closing];
        }
        else newValue = value[..opening];
        

        string dataType = TsType.Undefined;
        

        for (i = 0; i < newValue.Length && dataType == TsType.Undefined; i++)
        {
            
            char chara = newValue[i];
            switch (chara)
            {
                case '[':
                    dataType = TsType.Array;
                    closing = value.IndexOf("}],");
                    if (closing == -1) closing = value.IndexOf("}]");
                    break;

                case '{':
                    dataType = TsType.Object;
                    closing = value.IndexOf("}},");
                    if (closing > newValue.Length) closing = Content.IndexOf("}");
                    break;

                case '"':
                    dataType = TsType.String;
                    closing = value.IndexOf(",");
                    break;

                case 'f':
                case 't':
                    dataType = TsType.Boolean;
                    closing = value.IndexOf(",");
                    break;

                default:
                    if (char.IsDigit(chara))
                    {
                        dataType = TsType.Number;
                        closing = value.IndexOf(",");
                    }
                    break;
            }
        }
        if (opening == -1 && closing == -1) return new(fieldName.Replace("\"", ""), dataType, newValue, null);
        else if (closing == -1) throw new Exception("Closing has not been found");
        newValue = value.Substring(opening, closing - opening);
        return  new(fieldName.Replace("\"", ""), dataType, newValue, null);
    }


    // sets CurrentObject and PreviousObject
    private bool SwitchObjects(TsClass obj)
    {
        if (CurrentObject != null)
        {
            PreviousObject = CurrentObject;
            CurrentObject = obj;
            CurrentObject!.SetParent(PreviousObject);
            return true;
        }
        else throw new NullReferenceException("CurrentObject is null."); 
    }

    public TsClass? GetObject()
    {
        return Root;
    }
}