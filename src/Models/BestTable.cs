// File: src/Models/Table.cs

// Class: Table
public class BestTable<T>
{
    // See note below after the constructor :)
    private enum TableMode
    {
        DataOnly,
        Full,
        WithHeaders,
        WithFooters,
        WithLabels
    }

    // Implement the wrapper class here :)
    public int Rows { get; set; }
    public int Columns { get; set; }
    public string[]? Headers { get; set; }
    public string[]? Footers { get; set; }
    public string[]? SideLabels { get; set; }

    private T[][] _data;

// These pragmas are only temporary while we implement this work mode feature.
#pragma warning disable CS0414
    private TableMode _workMode;
#pragma warning restore CS0414

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

    // Got this crazy idea of being able to work with the table, alongside
    // its headers, footers, and side labels together as a single entity,
    // or separately. Will implement it for funsies as I get the time to do so.
    public void UseDataView() => _workMode = TableMode.DataOnly;
    public void UseFullView() => _workMode = TableMode.Full;
    public void UseHeadersView() => _workMode = TableMode.WithHeaders;
    public void UseFootersView() => _workMode = TableMode.WithFooters;
    public void UseLabelsView() => _workMode = TableMode.WithLabels;

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
