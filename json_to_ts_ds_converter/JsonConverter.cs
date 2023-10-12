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


    // counts every brace opening in the value
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
                if(PreviousObject == null) return true;
                CurrentObject = PreviousObject;
                PreviousObject = CurrentObject!.GetParent();
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
            string? value = CurrentObject.GetValue();
            if(value == null) return true;
            while(value != null)
            {    
                string? fieldName = SeparateFieldName(value);
                if(fieldName == null) return true;
                value = CurrentObject.GetValue();
                
                string[] result = SeparateObjectValues2(value);
                
                TsClass obj = new TsClass(fieldName, result[1], result[0], CurrentObject);
                value = CurrentObject.GetValue();

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
    private string? SeparateFieldName(string value)
    {
        int closing = value.IndexOf(":");
        if(closing <= 0) return null;

        TrimObjectValue(value, value.Length - 1, closing + 1);

        return value[..closing].Replace("\"", "").Replace("{", "");
    }


    // trims current object value
    private string TrimObjectValue(string value, int closing, int? opening)
    {
        string newValue;
        if(opening == null) newValue = value[..closing];
        else 
        {
            int trimEnd = closing - (int) opening;
            newValue = value.Substring((int) opening, trimEnd);
        }
        
        CurrentObject!.SetValue(newValue);
        return newValue;
    }


    private string[] SeparateObjectValues2(string value)
    {
        int closing = value.IndexOf("\n");
        if (closing <= 0) closing = value.IndexOf(",");
        if (closing <= 0) closing = value.Length - 1;
        string typeString = value.Substring(0, closing);
        int opening = 0;
        string dataType = TsType.Null;
        
        int closingNew = 0;
        while(dataType == TsType.Null)
        {
            char chara = typeString[opening];
            value = value.Substring(opening);
            switch(chara)
            {
                case '[':
                    dataType = TsType.Array;
                    opening++;
                    closingNew = value.IndexOf("}],") + 3;
                    if (closingNew == -1) closingNew = value.IndexOf("}]");
                    break;

                case '{':
                    dataType = TsType.Object;
                    opening++;
                    closingNew = value.IndexOf("},") + 2;
                    if (closingNew == -1) closingNew = value.IndexOf('}') + 1;            
                    break;

                case 'f':
                case 't':
                    if(typeString.Contains("true") || typeString.Contains("false")) dataType = TsType.Boolean;
                    else dataType = TsType.String;
                    closingNew = value.IndexOf(",") + 1;
                    break;

                case '"':
                    dataType = TsType.String;
                    closingNew = value.IndexOf(",") + 1;
                    break;

                default:
                    if (char.IsDigit(chara))
                    {
                        dataType = TsType.Number;
                        closingNew = value.IndexOf(",") + 1;
                    }
                    else if(char.IsLetter(chara))
                    {
                        dataType = TsType.String;
                        closingNew = value.IndexOf(",") + 1;
                    }
                    break;
            }
            opening++;
        }
        //string, digits, bool should trim starting from closingNew to valueLength - closingNew - 1
        if(dataType == TsType.Array || dataType == TsType.Object) value = TrimObjectValue(value, closingNew, opening);
        else value = TrimObjectValue(value, value.Length - 1, closingNew);
        
        string[] result = {value, dataType};
        return result;
    }


    // separates objects from value of the current object 
    private TsClass SeparateObjectValues(string fieldName, string value)
    {
        // 2
        int closingObj = value.IndexOf("\"");
        if (closingObj <= 0) closingObj = value.IndexOf(",");
         
        string? newValue = value[..closingObj];
        string dataType = TsType.Null;
          
        int opening = 0;
        int closingNew = 0;
        for (; opening < newValue.Length && dataType == TsType.Null; opening++)
        {
            char chara = newValue[opening];
            switch (chara)
            {
                case '[':
                    dataType = TsType.Array;
                    closingNew = value.IndexOf("}],") + 3;
                    if (closingNew == -1) closingNew = value.IndexOf("}]");
                    break;

                case '{':
                    dataType = TsType.Object;
                    closingNew = value.IndexOf("}},") + 3;
                    if (closingNew > newValue.Length) closingNew = value.IndexOf("}") + 1;
                    break;

                case 'f':
                case 't':
                    if(newValue.Contains("true") || newValue.Contains("false")) dataType = TsType.Boolean;
                    else dataType = TsType.String;
                    closingNew = value.IndexOf(",") + 1;
                    break;

                case '"':
                    dataType = TsType.String;
                    closingNew = value.IndexOf(",") + 1;
                    break;

                default:
                    if (char.IsDigit(chara))
                    {
                        dataType = TsType.Number;
                        closingNew = value.IndexOf(",") + 1;
                    }
                    else if(char.IsLetter(chara))
                    {
                        dataType = TsType.String;
                        closingNew = value.IndexOf(",") + 1;
                    }
                    break;
            }
        }
    
        // opening += 1;
        switch (closingNew)
        {
            case 0:
                closingNew = value.LastIndexOf("\"") + 1;
                break;                
            case -1:
                newValue = TrimObjectValue(value, value.Length - 1, null);
                return new(fieldName, dataType, newValue, null);
        }
         
        newValue = TrimObjectValue(value, closingNew, opening);

        if (newValue.Length == 0) return new(fieldName.Replace("\"", ""), dataType, null, null);
        else return  new(fieldName.Replace("\"", ""), dataType, newValue, null);
    }


    // calls to change parents value to new value or throws exception
    private bool ChangeParentsValue(string? newValue)
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