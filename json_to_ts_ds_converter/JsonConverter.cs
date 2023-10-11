using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;

namespace CsClassToTsConverter;

public class JsonConverter
{
    private string? FileName;
    private string Path { get; set; }
    private bool? FileExists { get; set;}
    private bool? IsJson { get; set; }
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
            if(TraverseValue()) 
            {
                CurrentObject = PreviousObject;
                PreviousObject = CurrentObject.GetParent();
            } 
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
                string? fieldName = SeparateFieldName();
                if(fieldName == null) return true;
                TsClass obj = SeparateObjectValues(fieldName);
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
            return false;
        }
        else throw new NullReferenceException("Value of value variable is null.");
    }


    // separates object field names from object values
    private string? SeparateFieldName()
    {
        string value = CurrentObject.GetValue();
        // int opening = value.IndexOf("\"");
        int closing = value.IndexOf(":");
        // int trimEnd = closing - opening;
        // if(trimEnd <= 0) return null;
        if(closing <= 0) return null;
        // string fieldName = value.Substring(opening, trimEnd);
        // trimEnd = value.Length - closing - 1;
        // CurrentObject.SetValue(value.Substring(closing + 1, trimEnd));

        string fieldName = value[..closing];
        int trimEnd = value.Length - closing - 1;
        CurrentObject.SetValue(value.Substring(closing + 1, trimEnd));
        return fieldName;
    }


    // separates objects from value of the current object 
    private TsClass SeparateObjectValues(string fieldName)
    {
        string value = CurrentObject.GetValue();
        // 2
        int opening = value.IndexOf("\"");
        int closing = value.IndexOf(",");
        string? newValue;
        

        if(opening <= -1) 
        {
            newValue = value;
            opening = 0;
        }
        else if (opening > closing) 
        {
            closing += 1;
            newValue = value[..closing];
        }
        else newValue = value[..opening];

        string dataType = TsType.Null;
          
        
        for (opening = 0; opening < newValue.Length && dataType == TsType.Null; opening++)
        {
            
            char chara = newValue[opening];
            switch (chara)
            {
                case '[':
                    dataType = TsType.Array;
                    closing = value.IndexOf("}],") + 3;
                    if (closing == -1) closing = value.IndexOf("}]");
                    break;

                case '{':
                    dataType = TsType.Object;
                    closing = value.IndexOf("}},") + 3;
                    if (closing > newValue.Length) closing = value.IndexOf("}") + 1;
                    break;

                case 'f':
                case 't':
                    if(newValue.Contains("true") || newValue.Contains("false")) dataType = TsType.Boolean;
                    else dataType = TsType.String;
                    closing = value.IndexOf(",") + 1;
                    break;

                case '"':
                    dataType = TsType.String;
                    closing = value.IndexOf(",") + 1;
                    break;

                default:
                    if (char.IsDigit(chara))
                    {
                        dataType = TsType.Number;
                        closing = value.IndexOf(",") + 1;
                    }
                    else if(char.IsLetter(chara))
                    {
                        dataType = TsType.String;
                        closing = value.IndexOf(",") + 1;
                    }
                    break;
            }
        }

        if(closing == 0) closing = value.LastIndexOf("\n");
        if (closing < 0) return new(fieldName.Replace("\"", ""), dataType, newValue, null);
        // if (opening == -1 && closing == -1) return new(fieldName.Replace("\"", ""), dataType, newValue, null);
        else if (closing == 0) closing = value.LastIndexOf("\"") + 1;
        else if (closing == -1) throw new Exception("Closing has not been found");
        int trimEnd = closing - opening;
        newValue = value.Substring(opening, trimEnd);
        trimEnd = value.Length - closing;
        string oldValue = value.Substring(closing + 1, trimEnd - 1);
        ChangeParentsValue(oldValue);

        if (newValue.Length == 0) return new(fieldName.Replace("\"", ""), dataType, null, null);
        else return  new(fieldName.Replace("\"", ""), dataType, newValue, null);
    }


    // calls to change parents value to new value or throws exception
    private bool ChangeParentsValue(string newValue)
    {
        if(CurrentObject != null) CurrentObject.SetValue(newValue);
        else throw new NullReferenceException("Value of CurrentObject is null.");
        return true;
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