// File: src/Utilities/DataFormatters.cs
using System.Linq;
using System.Text;

// Base Class: DataFormatter
public class DataFormatter<T>
{
    protected BestTable<T> DataTable { get; }

    public DataFormatter(BestTable<T> data)
    {
        DataTable = data;
    }

    public virtual string Draw()
    {
        System.Console.WriteLine("The DataFormatter class is solely a template"
            + " base object. Use the derived implementations instead.");
        return string.Empty;
    }
}

public class TableFormatter<T> : DataFormatter<T>
{
    public TableFormatter(BestTable<T> data)
        : base(data) {}

    public override string Draw()
    {
        var tableSb = new StringBuilder();
        int[] columnLengths = GetColumnsCellLengths();

        // We're assuming our BestTable friend has headers and side labels here.
        // Will have to handle other cases some other time.
        tableSb.Append(BuildTableHeader(columnLengths));

        for (int i = 0; i < DataTable.Rows; i++)
        {
            tableSb.AppendFormat("| {0}", DataTable.SideLabels![i]
                                                    .PadRight(columnLengths[0] - 1));
            for (int j = 0; j < DataTable.Columns; j++)
            {
                tableSb.AppendFormat("| {0}", DataTable[i, j]!.ToString()!
                                                              .PadRight(columnLengths[j+1] - 1));
            }
            tableSb.Append("|\n");
        }

        tableSb.Append(BuildHeaderAndFooterOutlines(columnLengths));
        return tableSb.ToString();
    }

    private int[] GetColumnsCellLengths()
    {
        int[] cellLengths;

        // TODO: Complete this size arrangement. We can add more conditions to
        //       check the different scenarios manually, while the Table views
        //       functionality is implemented.
        if (DataTable.Headers is not null)
            cellLengths = new int[DataTable.Headers.Length];
        else
            cellLengths = new int[DataTable.Columns];

        // Let's assume our BestTable friend always has headers defined for
        // the time being.
        for (int i = 0; i < DataTable.Headers!.Length; i++)
        {
            // We add a +2 here to always leave at least 1 space at the beginning
            // and at the end of each cell. It looks much cleaner and neater.
            cellLengths[i] = DataTable.Headers![i].Length + 2;
        }

        for (int j = 0; j < DataTable.Rows; j++)
        for (int k = 0; k < DataTable.Columns; k++)
        {
            string cellValue = DataTable[j, k]!.ToString()!;
            int len = cellValue.Length + 2;

            if (len > cellLengths[k])
                cellLengths[k] = len;
        }

        // Just assuming our BestTable friend will also always have side labels
        // for the time being.
        for (int l = 0; l < DataTable.SideLabels!.Length; l++)
        {
            string cellValue = DataTable.SideLabels[l];
            int len = cellValue.Length + 2;

            if (len > cellLengths[0])
                cellLengths[0] = len;
        }

        return cellLengths;
    }

    private string BuildTableHeader(int[] columnLengths)
    {
        var headerSb = new StringBuilder();
        headerSb.Append(BuildHeaderAndFooterOutlines(columnLengths));

        for (int i = 0; i < DataTable.Headers!.Length; i++)
        {
            string header = DataTable.Headers[i];
            headerSb.AppendFormat("| {0}", header.PadRight(columnLengths[i] - 1));
        }

        headerSb.Append("|\n");
        headerSb.Append(BuildHeaderAndFooterOutlines(columnLengths));
        return headerSb.ToString();
    }

    private string BuildHeaderAndFooterOutlines(int[] columnLengths,
                                                char outlineSeparator = '+')
    {
        var outlineSb = new StringBuilder();
        outlineSb.Append(outlineSeparator);

        foreach (int colLen in columnLengths)
        {
            char[] cellBorder = Enumerable.Repeat('-', colLen).ToArray();
            outlineSb.Append(cellBorder);
            outlineSb.Append(outlineSeparator);
        }

        outlineSb.Append("\n");
        return outlineSb.ToString();
    }
}

public class CsvFormatter<T> : DataFormatter<T>
{
    public CsvFormatter(BestTable<T> data)
        : base(data) {}

    public override string Draw()
    {
        var csvSb = new StringBuilder();

        // Just like our friend the Table Formatter does, we'll be assuming
        // our BestTable friend has headers and side labels here. Will have to
        // handle other cases some other time.
        csvSb.AppendFormat("{0}\n", string.Join(',', DataTable.Headers!));

        // Since we only want to get all the values separated by commas, our
        // friend string.Join() can do it in a concise and efficient way.
        for (int i = 0; i < DataTable.Rows; i++)
        {
            T[] row = DataTable[i];
            csvSb.AppendFormat("{0},{1}\n", DataTable.SideLabels![i],
                               string.Join(',', row.Select(column =>
                                                           column!.ToString())));
        }
        return csvSb.ToString();
    }
}
