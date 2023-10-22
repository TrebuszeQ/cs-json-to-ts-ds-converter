using System.Diagnostics;
using System.Text;

namespace CsClassToTsConverter;

public class JsonConverter
{
    private string Path;
    private Dictionary<string, string>? FileCache = new Dictionary<string, string>();
    private string Content;
    private TsClass Root;

    public JsonConverter(string path)
    {
        Path = path;
        string FileName = SeparateFileName(path);
        Console.WriteLine("Extension: " + System.IO.Path.GetExtension(FileName));
        Debug.WriteLine("Extension: " + System.IO.Path.GetExtension(FileName));
        try
        {
            bool FileExists = System.IO.Path.Exists(path) ? true : throw new ArgumentException("Path is not accessible or doesn't exist.");
            if(!FileExists) throw new Exception("File doesn't exist or is inaccessible.");
            bool IsJson = System.IO.Path.GetExtension(FileName) == ".json" ? true : throw new ArgumentException("The file is not a valid JSON file");
            if(!IsJson) throw new Exception("File doesn't exist or is inaccessible.");
        }
        catch (ArgumentException e)
        {
            throw e;
        }
        AccessCacheValue(path);
        Root = new TsClass("root", TsType.Object, Content![1..^1], null);
    }


    // separates FileName from path.
    private string SeparateFileName(string path)
    {
        int closing = path.LastIndexOf('/');
        if (closing == -1) closing = path.LastIndexOf('\\');
        return path.Substring(closing + 1, Math.Abs(path.Length - closing - 1));
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


    public bool Convert()
    {
        TsClass CurrentObject = Root;
        TsClass? PreviousObject = null;
        // string Value = CurrentObject.GetValue();
        while (true)
        {
            if (CurrentObject != null)
            {
                
                string? value = CurrentObject.GetValue();
                while (value != null && value.Length > 0)
                {
                    string typeString;
                    int closing = 0;
                    int opening = 0;
                    string fieldName;
                    bool truth;
                    string dataType;
                    int closingNew;
                    int trimEnd;
                    int trimStart;
                    string newVal;

                    // fieldName
                    closing = value.IndexOf(":");
                    if (closing <= 0) 
                    {
                        value = null;
                        CurrentObject.SetValue(value);
                        break;
                    }
                    
                    string TrimBothEnds()
                    {
                        string fieldName = value[..closing];
                        int length = fieldName.Length;
                        StringBuilder fieldNameCleared = new StringBuilder(capacity: length);
                        
                        for(int i = 0; i < length; i++)
                        {
                            char chara = fieldName[i];
                            switch(chara)
                            {
                                case '{':
                                case '[':
                                case ',':
                                case '}':
                                case ']':
                                    opening++;
                                break;
                                default:
                                    fieldNameCleared.Append(chara);
                                break;
                            }
                        }
                        return fieldNameCleared.ToString();
                    }

                    fieldName = TrimBothEnds();

                    // break on null fieldName
                    if (fieldName == null) 
                    {
                        value = null;
                        CurrentObject.SetValue(value);
                        break;
                    }

                    // dump clones
                    truth = CurrentObject.IsChildPresent(fieldName);    
                    if (truth) 
                    {
                        CurrentObject.SetValue(null);
                        break;
                    }

                    // trim value from fieldname
                    trimEnd = value.Length - closing - 1;
                    trimStart = closing + 1;
                    value = value.Substring(trimStart, Math.Abs(trimEnd));
                    //trim parent
                    CurrentObject.SetValue(value);
                    
                    //value
                    closing = value.IndexOf(",");
                    if (closing <= 0) closing = value.Length;
                    typeString = value.Substring(0, Math.Abs(closing));
                    
                    int findIndex(char desired)
                    {
                        for(int x = 0; x < value.Length - 1; x++)
                        {
                            if(desired.Equals(value[x])) return x;
                        }
                        return value.Length;
                    }
                    
                    closingNew = -2;
                    
                    string ReturnDataType()
                    {
                        dataType = TsType.Null;
                        truth = true;
                        if (typeString.Length > 0)
                        {
                            int c = 0;
                            while (truth) 
                            {
                                if(c == fieldName.Length) break;
                                char chara = typeString[c];
                                switch (chara)
                                {
                                    case '[':
                                        dataType = TsType.Array;
                                        closingNew = findIndex(']');
                                        truth = false;
                                        break;

                                    case '{':
                                        dataType = TsType.Object;
                                        closingNew = findIndex('}');
                                        truth = false;
                                        break;

                                    case 'f':
                                    case 't':
                                        if (typeString.Contains("true") || typeString.Contains("false")) dataType = TsType.Boolean;
                                        else dataType = TsType.String;
                                        truth = false;
                                        break;

                                    case '"':
                                        dataType = TsType.String;
                                        truth = false;
                                        break;

                                    case 'n':
                                        if(typeString.Contains("null")) truth = false;
                                        else 
                                        {
                                            dataType = TsType.String;
                                            truth = false;   
                                        }
                                        break;

                                    default:
                                        if (char.IsDigit(chara)) 
                                        {
                                            dataType = TsType.Number;
                                            truth = false;
                                        }
                                        else if (char.IsLetter(chara)) 
                                        {
                                            dataType = TsType.String;
                                            truth = false;
                                        }
                                        break;
                                } 
                                c++;
                            };
                            opening = opening + c;
                        }
                        return dataType;
                    }
                    // typeString = TrimBothEnds();
                    dataType = ReturnDataType();

                    if (closingNew == -2) closingNew = findIndex(',');
                    //string, digits, bool should trim starting from closingNew to valueLength - closingNew - 1
                    if(dataType == TsType.Object || dataType == TsType.Array)
                    {
                        trimEnd = closingNew - opening;
                        if (trimEnd > 0) value = value.Substring(opening, Math.Abs(trimEnd));
                        else value = null;
                        
                        TsClass obj = new TsClass(fieldName, dataType, value, CurrentObject);
                        CurrentObject!.SetChild(obj);
                        
                        PreviousObject = CurrentObject;
                        CurrentObject = obj;
                        CurrentObject!.SetParent(PreviousObject);
                        // CurrentObject.SetValue(value);

                        // remove value of current object from previous object
                        newVal = PreviousObject.GetValue();
                        if(value == null) 
                        {
                            trimEnd = newVal.Length - opening - 1;
                            trimStart = opening + 1;
                            newVal = newVal.Substring(trimStart, Math.Abs(trimEnd));
                        }
                        else 
                        {
                            trimEnd = newVal.Length - value.Length - opening;
                            trimStart = value.Length + opening;
                            newVal = newVal.Substring(trimStart, Math.Abs(trimEnd));
                        }
                        // substring cuts about 2 chars too less
                        PreviousObject.SetValue(newVal);
                    }
                    else
                    {
                        trimEnd = value.Length - closingNew - 1;
                        trimStart = closingNew + 1;
                        if(trimEnd < 0) value = null;
                        else value = value.Substring(trimStart, Math.Abs(trimEnd));
                        // SetValue should probably be earlier here
                        // value should probably be typeString
                        CurrentObject.SetValue(value);
                        // TsClass obj = new TsClass(fieldName, dataType, value, CurrentObject);
                        TsClass obj = new TsClass(fieldName, dataType, typeString, CurrentObject);
                        CurrentObject!.SetChild(obj);
                    }
                    
                }
            }
            CurrentObject = PreviousObject;
            if(CurrentObject == null) break;
            PreviousObject = CurrentObject!.GetParent();            
        }
        return true;
    }

    public TsClass? GetObject()
    {
        return Root;
    }
}