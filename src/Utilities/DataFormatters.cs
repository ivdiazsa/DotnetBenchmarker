// File: src/Utilities/DataFormatters.cs
using System.Linq;
using System.Text;

// Base Class: DataFormatter
public class DataFormatter<T>
{
    private BestTable<T> _data;

    public DataFormatter(BestTable<T> data)
    {
        _data = data;
    }

    public string Draw()
    {
        System.Console.WriteLine("The DataFormatter class is solely a template"
            + " base object. Use the derived implementations instead.");
        System.Environment.Exit(-1);
        return string.Empty;
    }
}

public class TableFormatter<T> : DataFormatter<T>
{
    public TableFormatter(BestTable<T> data)
        : base(data) {}

    public new string Draw()
    {
        var tableSb = new StringBuilder();
        return tableSb.ToString();
    }
}

public class CsvFormatter<T> : DataFormatter<T>
{
    public CsvFormatter(BestTable<T> data)
        : base(data) {}

    public new string Draw()
    {
        return string.Empty;
    }
}
