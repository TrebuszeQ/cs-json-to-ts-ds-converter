using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace CsClassToTsConverter;

public class TsClass
{
    private string? ClassName { get; set; }
    private string? DataType { get; set; }
    private string? Value { get; set; }
    private TsClass? Child { get; set; }
    private List<TsClass>? Children { get; set; }
    private TsClass? Parent { get; set; }


    public TsClass(string? className, string? type, string? value, TsClass? parent)
    {
        DataType = type;
        Parent = parent;
        ClassName = className; 
        Value = value;
    }


    // sets parent of the object
    public void SetParent(TsClass member) => Parent ??= member;


    public TsClass? GetParent() => Parent;


    // sets child or if its not null it calls another methods
    public void SetChild(TsClass child)
    {
        if (child == null) Child = child;
        else if (child != null && Children == null) InitChildren(child);
        else if (child != null && Children == null) AppendChildren(child);
    }


    // if children is null it initializes Children list
    public void InitChildren(TsClass child) => Children = new List<TsClass>() { child };


    // appends Children list
    public void AppendChildren(TsClass child) => Children!.Add(child);


    public TsClass? GetChild() => Child;


    public void SetName(string name) => ClassName = name;


    public string? GetName() => ClassName;


    public void SetType(string type) => DataType = type;


    public string GetDType() => DataType;

    public string GetValue() => Value;


    // returns true if Children already contains child.
    public bool IsChildPresent(TsClass obj)
    {
        if (Children != null && Children.Count > 0) return Children!.Contains(obj);
        else return false;
    }
}