namespace CsClassToTsConverter;

public class TsClassFile
{
    private string _path { get; set; }
    private string _className { get; set; }
    private string _fileName { get; set; }
    private string _keywords { get; set; }
    private string _content { get; set; }

    public TsClassFile(string path, string className, string keywords, string content)
    {
        _path = ValidatePath(path);
        _className = ValidateFileName(className);
        _keywords = keywords;
        _content = content;

    }

    // checks if path is exists and is accessible. 
    private string ValidatePath(string path)
    {
        bool truth = Path.Exists(path);
        if (truth) return path;
        throw new FileNotFoundException("Path doesn't exist or is inaccessible.");
    }
    
    
    // returns validated filename or throws error if path is wrong
    private string ValidateFileName(string className)
    {
        string fileName = _className; 
        int c = 0;
        string path = Path.Join(_path, fileName);
        bool truth = File.Exists(path);
        do
        {
            c++;
            _fileName += c;
            path = Path.Join(_path, fileName);
            truth = File.Exists(path);
        }
        while (truth);

        return fileName;
    }
    
    
}