// File: src/Models/Table.cs

// Class: Table
public class BestTable<T>
{
    private enum TableMode
    {
        DataOnly,
        Full,
        WithHeaders,
        WithHeadersAndFooters,
        WithHeadersAndLabels,
        WithLabels
    }

    // Implement the wrapper class here :)
    public int Rows { get; set; }
    public int Columns { get; set; }
    public string[]? Headers { get; set; }
    public string[]? Footers { get; set; }
    public string[]? SideLabels { get; set; }

    private T[][] _data;
    private TableMode _workMode;

    public BestTable(int rows, int columns, string[]? headers = null,
                     string[]? footers = null, string[]? sides = null)
    {
        _data = new T[rows][];
        _workMode = TableMode.DataOnly;

        for (int i = 0; i < rows; i++)
        {
            _data[i] = new T[columns];
        }

        Rows = rows;
        Columns = columns;
        Headers = headers;
        Footers = footers;
        SideLabels = sides;
    }

    // TODO: Implement the rest of the indexing modes.
    public T this[int rowIndex, int colIndex]
    {
        get
        {
            return _data[rowIndex][colIndex];
        }

        set
        {
            _data[rowIndex][colIndex] = value;
        }
    }

    // TODO: Implement the methods to switch the table's work modes.
}
