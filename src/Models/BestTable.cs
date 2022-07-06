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

    // To explain it simply, this class treats the table's contents separately
    // from the headers, footers, and side row labels you might want to add.
    // This is first, to be able to view the table data from different perspectives,
    // and second, to not have to be tied to the "string" type, since we will
    // possibly and likely want to do numerical operations with the table's contents.

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

    public T[] this[int index]
    {
        get { return _data[index]; }
    }

    // TODO: Implement the methods to switch the table's work modes.
}
