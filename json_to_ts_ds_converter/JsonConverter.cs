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
                    // fieldName
                    int closing = value.IndexOf(":");
                    if (closing <= 0) 
                    {
                        value = null;
                        CurrentObject.SetValue(value);
                        break;
                    }

                    string fieldName = value[..closing];
                    
                    if (fieldName == null) 
                    {
                        value = null;
                        CurrentObject.SetValue(value);
                        break;
                    }
                    // else if(fieldName != null)
                    // {
                    //     StringBuilder fieldNameCleared = new StringBuilder(capacity: fieldName.Length);
                    //     for(int i = 0; i < fieldName.Length; i++)
                    //     {
                    //         char chara = fieldName[i];
                    //         if(char.IsLetter(chara)) fieldNameCleared.Append(chara);
                            
                    //     }
                    //     fieldName = fieldNameCleared.ToString();
                    // }

                    

                    // dump clones
                    bool truth = CurrentObject.IsChildPresent(fieldName);    
                    if (truth) 
                    {
                        CurrentObject.SetValue(null);
                        break;
                    }

                    // trim value from fieldname
                    int trimEnd = value.Length - closing - 1;
                    value = value.Substring(closing + 1, Math.Abs(trimEnd));
                    //trim parent
                    CurrentObject.SetValue(value);
                    
                    

                    //value
                    closing = value.IndexOf(",");
                    if (closing <= 0) closing = value.Length;
                    
                    string typeString = value.Substring(0, Math.Abs(closing));
                    int opening = 0;
                    string dataType = TsType.Null;
                
                    int closingNew = -2;

                    int findIndex(char desired)
                    {
                        for(int x = opening; x < value.Length - 1; x++)
                        {
                            if(desired.Equals(value[x])) return x;
                        }
                        return value.Length;
                    }

                    truth = true;
                    if (typeString.Length > 0)
                    {
                        while (truth) 
                        {
                            if(opening == fieldName.Length) break;
                            char chara = typeString[opening];
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
                            opening++;
                        };
                    }
                    
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
                        string newVal = PreviousObject.GetValue();
                        if(value == null) 
                        {
                            trimEnd = newVal.Length - opening - 1;
                            newVal = newVal.Substring(1 + opening, Math.Abs(trimEnd));
                        }
                        else 
                        {
                            trimEnd = newVal.Length - value.Length - opening;
                            newVal = newVal.Substring(value.Length + opening, Math.Abs(trimEnd));
                        }
                        // substring cuts about 2 chars too less
                        PreviousObject.SetValue(newVal);
                    }
                    else
                    {
                        trimEnd = value.Length - closingNew - 1;
                        if(trimEnd < 0) value = null;
                        else value = value.Substring(closingNew + 1, Math.Abs(trimEnd));
                        CurrentObject.SetValue(value);
                        TsClass obj = new TsClass(fieldName, dataType, value, CurrentObject);
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

    // public bool ConvertJson()
    // {
    //     int TreeIndex = 0;
    //     TsClass CurrentObject = this.Root;
    //     TsClass? PreviousObject = null;
    //     //string Value;

    //     // traverses through objects value, if successfull it returns true, if not it throws exception.
    //     // when it ReturnObject() returns Object or Array it calls to switch Objects and sets relation.
    //     // bool TraverseObjects()
    //     // {
    //     //     int objectsCount = CountObjects();
    //     //     for (int i = TreeIndex; i < objectsCount; i++)
    //     //     {
    //     //         if (TraverseValue())
    //     //         {
    //     //             if (PreviousObject == null) return true;
    //     //             CurrentObject = PreviousObject;
    //     //             PreviousObject = CurrentObject!.GetParent();
    //     //         }
    //     //         TreeIndex = i;
    //     //     }
    //     //     return true;
    //     // }


    //     // traverses through value variable of an object. If array or obj is found it switches to this object.
    //     // bool TraverseValue()
    //     // {
    //     //     if (CurrentObject != null)
    //     //     {
    //     //         string? value = CurrentObject.GetValue();
    //     //         if (value == null) return true;
    //     //         while (value != null)
    //     //         {
    //     //             value = CurrentObject.GetValue();
    //     //             string? fieldName = SeparateFieldName(value);
    //     //             if (fieldName == null) return true;
    //     //             value = CurrentObject.GetValue();

    //     //             string[] result = SeparateObjectValues2(value);

    //     //             TsClass obj = new TsClass(fieldName, result[1], result[0], CurrentObject);
    //     //             value = CurrentObject.GetValue();

    //     //             if (CurrentObject.IsChildPresent(obj) == false)
    //     //             {
    //     //                 CurrentObject!.SetChild(obj);
    //     //                 if (obj.GetDType() == TsType.Object || obj.GetDType() == TsType.Array)
    //     //                 {
    //     //                     SwitchObjects(obj);
    //     //                     TraverseValue();
    //     //                 }
    //     //             }
    //     //             value = CurrentObject.GetValue();
    //     //         }
    //     //         return false;
    //     //     }
    //     //     else throw new NullReferenceException("Value of value variable is null.");
    //     // }


    //     // separates object field names from object values
    //     string? SeparateFieldName(string value)
    //     {
    //         int closing = value.IndexOf(":");
    //         if (closing <= 0) return null;

    //         TrimObjectValue(value, value.Length - 1, closing + 1);

    //         return value[..closing].Replace("\"", "").Replace("{", "");
    //     }


    //     // trims current object value
    //     string? TrimObjectValue(string value, int closing, int? opening)
    //     {
    //         string newValue;
    //         if (opening == null) newValue = value[..closing];
    //         else
    //         {
    //             int trimEnd = closing - (int)opening;
    //             newValue = value.Substring((int)opening, trimEnd);
    //         }

    //         if (string.IsNullOrEmpty(newValue)) return null;
    //         CurrentObject!.SetValue(newValue);
    //         return newValue;
    //     }


    //     string[] SeparateObjectValues2(string value)
    //     {
    //         int closing = value.IndexOf("\n");
    //         if (closing <= 0) closing = value.IndexOf(",");
    //         if (closing <= 0) closing = value.Length - 1;
    //         string typeString = value.Substring(0, closing);
    //         int opening = 0;
    //         string dataType = TsType.Null;

    //         int closingNew = 0;
    //         while (dataType == TsType.Null)
    //         {
    //             char chara = typeString[opening];
    //             switch (chara)
    //             {
    //                 case '[':
    //                     dataType = TsType.Array;
    //                     opening++;
    //                     closingNew = value.IndexOf("}],") + 3;
    //                     if (closingNew == -1) closingNew = value.IndexOf("}]");
    //                     break;

    //                 case '{':
    //                     dataType = TsType.Object;
    //                     opening++;
    //                     closingNew = value.IndexOf("},") + 2;
    //                     if (closingNew == -1) closingNew = value.IndexOf('}') + 1;
    //                     break;

    //                 case 'f':
    //                 case 't':
    //                     if (typeString.Contains("true") || typeString.Contains("false")) dataType = TsType.Boolean;
    //                     else dataType = TsType.String;
    //                     closingNew = value.IndexOf(",") + 1;
    //                     break;

    //                 case '"':
    //                     dataType = TsType.String;
    //                     closingNew = value.IndexOf(",") + 1;
    //                     break;

    //                 default:
    //                     if (char.IsDigit(chara))
    //                     {
    //                         dataType = TsType.Number;
    //                         closingNew = value.IndexOf(",") + 1;
    //                     }
    //                     else if (char.IsLetter(chara))
    //                     {
    //                         dataType = TsType.String;
    //                         closingNew = value.IndexOf(",") + 1;
    //                     }
    //                     break;
    //             }
    //             opening++;
    //         }

    //         if (closingNew == -1 || closingNew == 0) closingNew = value.Length - 1;

    //         //string, digits, bool should trim starting from closingNew to valueLength - closingNew - 1
    //         if (dataType == TsType.Array || dataType == TsType.Object) value = TrimObjectValue(value, closingNew, opening);
    //         else
    //         {
    //             TrimObjectValue(value, value.Length - 1, closingNew);
    //             value = value.Substring(opening, closingNew - opening);
    //         }

    //         string[] result = { value, dataType };
    //         return result;
    //     }


    //     // sets CurrentObject and PreviousObject
    //     bool SwitchObjects(TsClass obj)
    //     {
    //         if (CurrentObject != null)
    //         {
    //             PreviousObject = CurrentObject;
    //             CurrentObject = obj;
    //             CurrentObject!.SetParent(PreviousObject);

    //             // remove value of current object from previous object
    //             string oldValue = PreviousObject.GetValue();
    //             string newValue = CurrentObject.GetValue();
    //             string value = oldValue.Substring(0, oldValue.Length - newValue.Length - 1);
    //             PreviousObject.SetValue(value);

    //             return true;
    //         }
    //         else throw new NullReferenceException("CurrentObject is null.");
    //     }

    //     return false;
    // }
    

    public TsClass? GetObject()
    {
        return Root;
    }
}