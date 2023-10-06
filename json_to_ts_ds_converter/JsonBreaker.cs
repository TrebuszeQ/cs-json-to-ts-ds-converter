using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Formats.Asn1;
using System.Net;
using System.Net.Mime;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

namespace CsClassToTsConverter;

public class JsonBreaker
{
    private string? FileName { get; set; }
    private string? Path { get; set; }
    private bool? FileExists { get; set; }
    private bool? IsJson { get; set; }
    private Dictionary<string, string>? FileCache { get; set; }
    private string Content { get; set; }
    private int ObjectsCount { get; set; }
    private TsClass[] JsonObjects { get; set; }
    private int TreeIndex { get; set; }
    private TsClass? CurrentObject { get; set; } = null;
    private TsClass? PreviousObject { get; set; } = null;
    private TsClass? ArrayObject { get; set; } = null;

    private JsonBreaker(string fileName, string path)
    {
        FileName = fileName;
        Path = ValidatePath(path);
        FileExists = DoFileExist(fileName, Path);
        IsJson = ValidateExtension(Path);
        InitCache(path, FileCache);
        AccessCacheValue(path, FileCache!);
        ObjectsCount = CountObjects();
        JsonObjects = new TsClass[ObjectsCount];
        TreeIndex = 0;
        CurrentObject = new TsClass("root", TsType.Object, Content.Substring(1, Content.Length - 2), null);
    }


    // checks if path exists and is accessible    
    private static string ValidatePath(string path)
    {
        if (path.Length > 10)
        {
            bool truth = System.IO.Path.Exists(path);
            if (truth) return path;
            throw new ArgumentException("Path doesn't exist or isn't accessible.");
        }

        throw new ArgumentException("Entry too short. Path doesn't exist.");
    }


    // check if file and full path exists and is accessible
    private bool DoFileExist(string filename, string path)
    {
        string joinedPath = System.IO.Path.Join(filename, path);
        bool pathB = System.IO.Path.Exists(joinedPath);
        if (pathB)
        {
            bool fileB = File.Exists(joinedPath);
            if (fileB)
            {
                Path = joinedPath;
                return true;
            }

            throw new ArgumentException("File doesn't exist or is inaccessible.");
        }

        throw new ArgumentException("Path to a file doesn't exist or is inaccessible.");
    }


    // validates if file is a json file
    private static bool ValidateExtension(string path)
    {
        string extension = System.IO.Path.GetExtension(path);
        return extension == "json" ? true : throw new ArgumentException("The file is not a valid JSON file");
    }


    // reads file to string asynchronously and returns it or throws exception
    private static async Task<string> TryReadFile(string path)
    {
        string? content = await File.ReadAllTextAsync(path);
        return content != null ? content.Trim(' ') : throw new Exception("File could not been read.");
    }


    // Changes Content value
    private void SetContent(string content) => Content = content;

    // returns cache file or if it's different it reads file and saves string to cache. 
    private void InitCache(string path, Dictionary<string, string>? cache)
    {
        if (cache == null)
        {
            string content = Content;
            cache = new Dictionary<string, string>
            {
                { path, content }
            };
        }

        FileCache = cache;
    }


    // populates Content with cache value or reads file and saves string to cache then returns string
    private async void AccessCacheValue(string path, Dictionary<string, string> cache)
    {
        bool truth = cache.ContainsKey(path);
        if (truth) Content = cache[path];
        else if(!truth)
        {
            string content = await TryReadFile(path);
            cache.Add(path, content);
            FileCache = cache;
            SetContent(content);
        }
    }


    private int CountObjects()
    {
        int counter = 0;
        string content = Content;
        foreach (char chara in content)
        {
            if (chara == '{') counter++;
        }

        return counter;
    }
    
    
    // trims json string by found structure's length, returns content and sets local Content
    private string TrimContent(int closing)
    {
        int length = Content.Length - closing;
        Content = Content.Substring(closing, length);
        Content = Content;
        return Content;
    }


    //private void TraverseContent()
    //{
    //    for (int i = 0; i < Content.Length; i++)
    //    {
    //        string? keyword;
    //        int opening = Content.IndexOf("\"");
    //        int closing = Content.IndexOf(":");
    //        keyword = ReturnKeyword2(opening, closing);
    //        TsClass tsClass;
    //        opening = closing;
    //        string value;
    //        int check = Content.IndexOf('"');
    //        closing = Content.IndexOf(',');


    //        if (check > closing)
    //        {
    //            value = Content[..closing].Trim(' ');
    //            TrimContent(closing);
    //        }
    //        else
    //        {
    //            value = Content[..check];
    //            TrimContent(check);
    //        }


    //        string dataType = TsType.Undefined;

    //        {
    //            foreach (char chara in value)
    //            {
    //                switch (chara)
    //                {
    //                    case '{':
    //                        dataType = TsType.Object;
    //                        break;

    //                    case '[':
    //                        dataType = TsType.Array;
    //                        value = TsType.Object;
    //                        break;

    //                    case '"':
    //                        dataType = TsType.String;
    //                        break;

    //                    case 'f':
    //                    case 't':
    //                        dataType = TsType.Boolean;
    //                        break;

    //                    default:
    //                        if (char.IsDigit(chara))
    //                        {
    //                            dataType = TsType.Number;
    //                        }
    //                        break;
    //                }
    //            }
    //        }

    //        tsClass = new(keyword, dataType, value, null);
    //        SetObjectsParent(PreviousObject);

    //        if (dataType == TsType.Object || dataType == TsType.Array) SetLocalObjects(tsClass);
    //    }


    //}

    private TsClass TraverseContent()
    {
        string? keyword;
        int opening = Content.IndexOf("\"");
        int closing = Content.IndexOf(":");
        keyword = ReturnKeyword2(opening, closing);
        TsClass tsClass;
        opening = closing;
        string value = "";
        int check = Content.IndexOf('"');
        closing = Content.IndexOf(',');
        string dataType = TsType.Undefined;

        for (int i = 0; i < Content.Length; i++)
        {

            if (check > closing)
            {
                value = Content[..closing].Trim(' ');
                TrimContent(closing);
            }
            else
            {
                value = Content[..check];
                TrimContent(check);
            }
            
            foreach (char chara in value)
            {
                switch (chara)
                {
                    case '{':
                        dataType = TsType.Object;
                        break;

                    case '[':
                        dataType = TsType.Array;
                        value = TsType.Object;
                        break;

                    case '"':
                        dataType = TsType.String;
                        break;

                    case 'f':
                    case 't':
                        dataType = TsType.Boolean;
                        break;

                    default:
                        if (char.IsDigit(chara))
                        {
                            dataType = TsType.Number;
                        }
                        break;
                }
            }
        }
        return new(keyword, dataType, value, null);
    }

    // todo
    private TsClass TraverseContent2()
    {
        int opening = Content.IndexOf("\"");
        int closing = Content.IndexOf(":");
        string keyword = Content.Substring(opening, closing - opening);
        string content = Content.Substring(closing + 1, Content.Length - closing - 1);
        Content = content;
        opening = Content.IndexOf("\"");
        closing = Content.IndexOf(",");
        string? value;

        if (opening > closing) value = Content[..closing];
        else value = Content[..opening];
        

        string? dataType = TsType.Undefined;

        opening = Content.IndexOf(":");
        for (int i = 0; i < value.Length; i++)
        {
            char chara = value[i];
            switch (chara)
            {
                case '{':
                    dataType = TsType.Object;
                    closing = Content.IndexOf("},{");
                    value = Content.Substring(i + 1, closing - i);
                    break;

                case '[':
                    dataType = TsType.Array;
                    closing = Content.IndexOf("}]");
                    value = Content.Substring(i + 1, closing - i);
                    break;

                case '"':
                    dataType = TsType.String;
                    closing = Content.IndexOf(",");
                    value = Content.Substring(i + 1, closing - i);
                    break;

                case 'f':
                case 't':
                    dataType = TsType.Boolean;
                    closing = Content.IndexOf(",");
                    value = Content.Substring(i + 1, closing - i);
                    break;

                default:
                    if (char.IsDigit(chara))
                    {
                        dataType = TsType.Number;
                        closing = Content.IndexOf(",");
                        value = Content.Substring(i + 1, closing - i);
                    }
                    break;
            }
        }
        return new(keyword.Trim('"'), dataType, value, null);
    }


    private void TraverseObjects()
    {
        for(int i = 0; i < ObjectsCount; i++)
        {
            Content = CurrentObject.GetValue();
            TsClass obj = TraverseContent();
            if (obj.GetDType() == TsType.Object || obj.GetDType() == TsType.Array)
            {
                CurrentObject.SetChild(obj);
                obj.SetParent(CurrentObject);
                PreviousObject = obj;
                CurrentObject = obj;
            }
        }
    }


    // returns keyword which precedes object
    private string ReturnKeyword2(int opening, int closing)
    {
        string content = Content;
        string keyword;
        keyword = content.Substring(opening + 1, closing - 2);
        return keyword;
    }


    // sets CurrentObject and PreviousObject
    private void SetLocalObjects(TsClass tsClass)
    {
        JsonObjects[TreeIndex] = tsClass;
        if (PreviousObject != null) PreviousObject = CurrentObject;
        if (tsClass.GetType().Equals(TsType.Array)) ArrayObject = CurrentObject; 
        CurrentObject = tsClass;
    }


    // calls objects method to set argument as an parent
    private void SetObjectsParent(TsClass member)
    {
        if (PreviousObject.GetType().Equals(TsType.Array)) CurrentObject!.SetParent(ArrayObject);
        else CurrentObject!.SetParent(member);
    }

    // finds which preceding objects is array and parent
    private TsClass? FindArrayParent()
    {
        for(int i = JsonObjects.Length; i != 0; i--)
        {
            if(JsonObjects[i].GetDType() == TsType.Array) return JsonObjects[i];
        }
        return null;
    }
}