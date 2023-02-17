using System.Reflection;
using System.Text;


namespace EntitiesDotNet;


internal enum VerticalAlignment
{
    Top,
    Center,
    Bottom
}


internal enum HorizontalAlignment
{
    Left,
    Center,
    Right
}


internal record struct Priority
{

    public Priority(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value),
                "Priority cannot be negative");
        }

        this.Value = value;
    }


    public int Value { get; private set; }


    public Priority Increase()
    {
        ++this.Value;
        return this;
    }


    public static Priority HighestValue => new Priority(int.MaxValue);


    public static explicit operator Priority(int value) => new(value);

}


internal class StringTable
{


    #region Static


    public static StringTable FromSeq<T>(IEnumerable<T> items,
        Func<T, IEnumerable<(string Header, string Value)>> getCells)
    {
        var table = new StringTable();

        var columns = new Dictionary<string, int>();

        var rowIndex = 0;
        foreach (var item in items)
        {
            ++rowIndex;
            foreach (var cell in getCells(item))
            {
                if (!columns.TryGetValue(cell.Header, out var columnIndex))
                {
                    columnIndex = table.ColumnsCount;
                    columns[cell.Header] = columnIndex;
                    table.WriteCell(0, columnIndex, cell.Header);
                }

                table.WriteCell(rowIndex, columnIndex, cell.Value);
            }
        }

        return table;
    }


    public static StringTable FromSeq<T>(IEnumerable<T> items)
    {
        return FromSeq(items, GetCells);


        static IEnumerable<(string Header, string Value)> GetCells(T item)
        {
            var type = item!.GetType();

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                yield return (field.Name, field.GetValue(item)?.ToString() ?? "null");
            }

            var properties =
                type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var property in properties)
            {
                yield return (property.Name,
                    property.GetValue(item)?.ToString() ?? "null");
            }
        }
    }


    #endregion


    #region Public


    public int RowsCount { get; private set; }
    public int ColumnsCount { get; private set; }


    public int GetColumnWidth(int column) => this._widthByColumn[column];


    public StringTableWriter CreateWriter() => new StringTableWriter(this);


    public void WriteCell(int row, int column, string text)
    {
        var values = text.Split(new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);
        this._values[new Index(row, column)] = values;

        var rowsMinCount = row + 1;
        if (rowsMinCount > this.RowsCount)
        {
            this.RowsCount = rowsMinCount;
        }

        var columnsCount = column + 1;
        if (columnsCount > this.ColumnsCount)
        {
            this.ColumnsCount = columnsCount;
        }

        foreach (var value in values)
        {
            this.SetColumnMinWidth(column, value.Length);
        }

        this.SetRowMinHeight(row, values.Length);
    }


    public override string ToString()
    {
        if (this.RowsCount == 0 && this.ColumnsCount == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        const char VerticalBar = '│';

        AppendHorizontalLine(VerticalAlignment.Top);

        for (var row = 0; row < this.RowsCount; ++row)
        {
            var rowHeight = this.RowHeight(row);
            var rowCellLines = new string[this.ColumnsCount, rowHeight];

            for (var column = 0; column < this.ColumnsCount; ++column)
            {
                var columnWidth = this.ColumnWidth(column);

                var lines =
                    this._values.TryGetValue(new Index(row, column), out var _lines)
                        ? _lines
                        : Array.Empty<string>();

                var verticalAlignment = this.GetCellVerticalAlignment(row, column);
                var horizontalAlignment = this.GetCellHorizontalAlignment(row, column);

                var padTop = verticalAlignment switch
                {
                    VerticalAlignment.Top => 0,
                    VerticalAlignment.Center => (rowHeight - lines.Length) / 2,
                    VerticalAlignment.Bottom => rowHeight - lines.Length,
                    _ => throw new ArgumentOutOfRangeException()
                };

                var emptyLine = string.Empty.PadLeft(columnWidth);
                for (var i = 0; i < padTop; ++i)
                {
                    rowCellLines[column, i] = emptyLine;
                }

                for (var i = 0; i < lines.Length; ++i)
                {
                    var line = lines[i];
                    var padLeft = horizontalAlignment switch
                    {
                        HorizontalAlignment.Left => 0,
                        HorizontalAlignment.Center => (columnWidth - line.Length) / 2,
                        HorizontalAlignment.Right => columnWidth - line.Length,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    rowCellLines[column, i + padTop] = line.PadLeft(line.Length + padLeft)
                        .PadRight(columnWidth);
                }

                for (var i = padTop + lines.Length; i < rowHeight; ++i)
                {
                    rowCellLines[column, i] = emptyLine;
                }
            }

            for (var line = 0; line < this.RowHeight(row); ++line)
            {
                builder.AppendLine();
                builder.Append(VerticalBar);

                for (var column = 0; column < this.ColumnsCount; ++column)
                {
                    var lineStr = rowCellLines[column, line];

                    builder.Append(' ');
                    builder.Append(lineStr);
                    builder.Append(' ');
                    builder.Append(VerticalBar);
                }
            }

            builder.AppendLine();
            AppendHorizontalLine(row != this.RowsCount - 1
                ? VerticalAlignment.Center
                : VerticalAlignment.Bottom);
        }

        return builder.ToString();


        void AppendHorizontalLine(VerticalAlignment verticalPosition)
        {
            var left = verticalPosition switch
            {
                VerticalAlignment.Top => '┌',
                VerticalAlignment.Center => '├',
                VerticalAlignment.Bottom => '└',
                _ => throw new ArgumentOutOfRangeException(nameof(verticalPosition),
                    verticalPosition, null)
            };

            var middle = verticalPosition switch
            {
                VerticalAlignment.Top => '┬',
                VerticalAlignment.Center => '┼',
                VerticalAlignment.Bottom => '┴',
                _ => throw new ArgumentOutOfRangeException(nameof(verticalPosition),
                    verticalPosition, null)
            };

            var right = verticalPosition switch
            {
                VerticalAlignment.Top => '┐',
                VerticalAlignment.Center => '┤',
                VerticalAlignment.Bottom => '┘',
                _ => throw new ArgumentOutOfRangeException(nameof(verticalPosition),
                    verticalPosition, null)
            };

            builder.Append(left);
            for (var column = 0; column < this.ColumnsCount; ++column)
            {
                var width = this.ColumnWidth(column);
                for (var i = 0; i < width + 2; ++i)
                {
                    builder.Append('─');
                }

                if (column == this.ColumnsCount - 1)
                {
                    builder.Append(right);
                }
                else
                {
                    builder.Append(middle);
                }
            }
        }
    }


    public string ToStringWithoutBorders()
    {
        var builder = new StringBuilder();

        for (var row = 0; row < this.RowsCount; ++row)
        {
            for (var line = 0; line < this.RowHeight(row); ++line)
            {
                builder.AppendLine();

                for (var column = 0; column < this.ColumnsCount; ++column)
                {
                    var cell = this.GetCellLine(row, column, line);

                    if (column > 0)
                    {
                        builder.Append(' ');
                    }

                    builder.Append(cell.PadRight(this.ColumnWidth(column)));
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }


    public void SetCellAlignment(int row, int column, HorizontalAlignment horizontal,
        Priority priority)
    {
        this._cellHorizontalAlignments[new Index(row, column)] =
            new WithPriority<HorizontalAlignment>(priority.Value, horizontal);
    }


    public void SetCellAlignment(int row, int column, VerticalAlignment vertical,
        Priority priority)
    {
        this._cellVerticalAlignments[new Index(row, column)] =
            new WithPriority<VerticalAlignment>(priority.Value, vertical);
    }


    public void SetCellAlignment(
        int row, int column, HorizontalAlignment horizontal, VerticalAlignment vertical,
        Priority priority
    )
    {
        this.SetCellAlignment(row, column, vertical, priority);
        this.SetCellAlignment(row, column, horizontal, priority);
    }


    public void SetRowAlignment(int row, HorizontalAlignment horizontal,
        Priority priority)
    {
        this._rowHorizontalAlignments[row] =
            new WithPriority<HorizontalAlignment>(priority.Value, horizontal);
    }


    public void SetRowAlignment(int row, VerticalAlignment vertical, Priority priority)
    {
        this._rowVerticalAlignments[row] =
            new WithPriority<VerticalAlignment>(priority.Value, vertical);
    }


    public void SetRowAlignment(
        int row, HorizontalAlignment horizontal, VerticalAlignment vertical,
        Priority priority
    )
    {
        this.SetRowAlignment(row, vertical, priority);
        this.SetRowAlignment(row, horizontal, priority);
    }


    public void SetColumnAlignment(int column, HorizontalAlignment horizontal,
        Priority priority)
    {
        this._columnHorizontalAlignments[column] =
            new WithPriority<HorizontalAlignment>(priority.Value, horizontal);
    }


    public void SetColumnAlignment(int column, VerticalAlignment vertical,
        Priority priority)
    {
        this._columnVerticalAlignments[column] =
            new WithPriority<VerticalAlignment>(priority.Value, vertical);
    }


    public void SetColumnAlignment(
        int column, HorizontalAlignment horizontal, VerticalAlignment vertical,
        Priority priority
    )
    {
        this.SetColumnAlignment(column, vertical, priority);
        this.SetColumnAlignment(column, horizontal, priority);
    }


    public HorizontalAlignment GetCellHorizontalAlignment(int row, int column)
    {
        var columnAlignment = this.GetColumnHorizontalAlignment(column);
        var rowAlignment = this.GetRowHorizontalAlignment(row);
        var cellAlignment = this.GetCellHorizontalAlignment(new Index(row, column));
        return columnAlignment.Max(rowAlignment).Max(cellAlignment).Alignment;
    }


    public VerticalAlignment GetCellVerticalAlignment(int row, int column)
    {
        var columnAlignment = this.GetColumnVerticalAlignment(column);
        var rowAlignment = this.GetRowVerticalAlignment(row);
        var cellAlignment = this.GetCellVerticalAlignment(new Index(row, column));
        return columnAlignment.Max(rowAlignment).Max(cellAlignment).Alignment;
    }


    public void SetDefaultAlignment(HorizontalAlignment horizontal,
        VerticalAlignment vertical)
    {
        this.SetDefaultAlignment(vertical);
        this.SetDefaultAlignment(horizontal);
    }


    public void SetDefaultAlignment(HorizontalAlignment horizontal)
    {
        this.DefaultHorizontalAlignment =
            new WithPriority<HorizontalAlignment>(0, horizontal);
    }


    public void SetDefaultAlignment(VerticalAlignment vertical)
    {
        this.DefaultVerticalAlignment = new WithPriority<VerticalAlignment>(0, vertical);
    }


    public void SetColumnMinWidth(int column, int width)
    {
        var currentWidth = this.ColumnWidth(column);
        if (currentWidth < width)
        {
            this._widthByColumn[column] = width;
        }
    }


    public void SetRowMinHeight(int row, int height)
    {
        var currentHeight = this.RowHeight(row);
        if (currentHeight < height)
        {
            this._heightByRow[row] = height;
        }
    }


    #endregion


    #region Private


    private readonly Dictionary<Index, string[]> _values = new();
    private readonly Dictionary<int, int> _widthByColumn = new();
    private readonly Dictionary<int, int> _heightByRow = new();


    private readonly Dictionary<Index, WithPriority<VerticalAlignment>>
        _cellVerticalAlignments = new();


    private readonly Dictionary<Index, WithPriority<HorizontalAlignment>>
        _cellHorizontalAlignments = new();


    private readonly Dictionary<int, WithPriority<VerticalAlignment>>
        _rowVerticalAlignments = new();


    private readonly Dictionary<int, WithPriority<HorizontalAlignment>>
        _rowHorizontalAlignments = new();


    private readonly Dictionary<int, WithPriority<VerticalAlignment>>
        _columnVerticalAlignments = new();


    private readonly Dictionary<int, WithPriority<HorizontalAlignment>>
        _columnHorizontalAlignments = new();


    private WithPriority<VerticalAlignment> DefaultVerticalAlignment { get; set; } =
        new(-1, VerticalAlignment.Top);


    private WithPriority<HorizontalAlignment> DefaultHorizontalAlignment { get; set; } =
        new(-1, HorizontalAlignment.Left);


    private string GetCellLine(int row, int column, int line)
    {
        if (this._values.TryGetValue(new Index(row, column), out var values) &&
            values.Length > line)
        {
            return values[line];
        }
        else
        {
            return string.Empty;
        }
    }


    private int GetCellLine(int row, int column)
    {
        if (this._values.TryGetValue(new Index(row, column), out var values))
        {
            return values.Length;
        }
        else
        {
            return 0;
        }
    }


    private WithPriority<VerticalAlignment> GetCellVerticalAlignment(Index cell) =>
        this._cellVerticalAlignments.TryGetValue(cell, out var value)
            ? value
            : this.DefaultVerticalAlignment;


    private WithPriority<HorizontalAlignment> GetCellHorizontalAlignment(Index cell) =>
        this._cellHorizontalAlignments.TryGetValue(cell, out var value)
            ? value
            : this.DefaultHorizontalAlignment;


    private WithPriority<VerticalAlignment> GetRowVerticalAlignment(int row) =>
        this._rowVerticalAlignments.TryGetValue(row, out var value)
            ? value
            : this.DefaultVerticalAlignment;


    private WithPriority<HorizontalAlignment> GetRowHorizontalAlignment(int row) =>
        this._rowHorizontalAlignments.TryGetValue(row, out var value)
            ? value
            : this.DefaultHorizontalAlignment;


    private WithPriority<VerticalAlignment> GetColumnVerticalAlignment(int column) =>
        this._columnVerticalAlignments.TryGetValue(column, out var value)
            ? value
            : this.DefaultVerticalAlignment;


    private WithPriority<HorizontalAlignment> GetColumnHorizontalAlignment(int column) =>
        this._columnHorizontalAlignments.TryGetValue(column, out var value)
            ? value
            : this.DefaultHorizontalAlignment;


    private int ColumnWidth(int column)
    {
        return this._widthByColumn.TryGetValue(column, out var width) ? width : 0;
    }


    private int RowHeight(int row)
    {
        return this._heightByRow.TryGetValue(row, out var height) ? height : 0;
    }


    private readonly record struct WithPriority<T>(int Priority, T Alignment)
    {
        public WithPriority<T> Max(WithPriority<T> other) =>
            this.Priority > other.Priority ? this : other;
    }


    private readonly struct Index : IEquatable<Index>
    {
        public bool Equals(Index other)
        {
            return this.Row == other.Row && this.Column == other.Column;
        }


        public readonly int Row;
        public readonly int Column;


        public override bool Equals(object obj)
        {
            return obj is Index other && this.Equals(other);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                return (this.Row * 397) ^ this.Column;
            }
        }


        public static bool operator ==(Index left, Index right)
        {
            return left.Equals(right);
        }


        public static bool operator !=(Index left, Index right)
        {
            return !left.Equals(right);
        }


        public Index(int row, int column)
        {
            this.Row = row;
            this.Column = column;
        }
    }


    #endregion


}


internal class StringTableWriter
{

    public StringTableWriter() : this(new StringTable())
    {
    }


    public StringTableWriter(StringTable table)
    {
        this.Table = table;
        this._currentRow = 0;
        this._currentColumn = -1;
    }


    public StringTable Table { get; }


    public void NewRow()
    {
        ++this._currentRow;
        this._currentColumn = -1;
    }


    public void Row(params string[] cellValues)
    {
        this.Row(cellValues.AsEnumerable());
    }


    public void Row(IEnumerable<string> cellValues)
    {
        foreach (var cell in cellValues)
        {
            this.Cell(cell);
        }

        this.NewRow();
    }


    public void Cell(string text)
    {
        this.Table.WriteCell(this._currentRow, ++this._currentColumn, text);
    }


    public void SetColumnMinWidth(int width)
    {
        this.Table.SetColumnMinWidth(this._currentColumn, width);
    }


    public void SetRowMinHeight(int height)
    {
        this.Table.SetRowMinHeight(this._currentRow, height);
    }


    public void SetDefaultAlignment(HorizontalAlignment horizontal,
        VerticalAlignment vertical)
    {
        this.Table.SetDefaultAlignment(horizontal, vertical);
    }


    public void SetDefaultAlignment(HorizontalAlignment horizontal)
    {
        this.Table.SetDefaultAlignment(horizontal);
    }


    public void SetDefaultAlignment(VerticalAlignment vertical)
    {
        this.Table.SetDefaultAlignment(vertical);
    }


    public void SetRowAlignment(
        HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment,
        Priority priority
    )
    {
        this.Table.SetRowAlignment(
            this._currentRow, horizontalAlignment, verticalAlignment, priority
        );
    }


    public void SetRowAlignment(HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment)
    {
        this.SetRowAlignment(horizontalAlignment, verticalAlignment, this.NextPriority);
    }


    public void SetRowAlignment(HorizontalAlignment horizontalAlignment,
        Priority priority)
    {
        this.Table.SetRowAlignment(this._currentRow, horizontalAlignment, priority);
    }


    public void SetRowAlignment(HorizontalAlignment horizontalAlignment)
    {
        this.SetRowAlignment(horizontalAlignment, this.NextPriority);
    }


    public void SetRowAlignment(VerticalAlignment verticalAlignment, Priority priority)
    {
        this.Table.SetRowAlignment(this._currentRow, verticalAlignment, priority);
    }


    public void SetRowAlignment(VerticalAlignment verticalAlignment)
    {
        this.SetRowAlignment(verticalAlignment, this.NextPriority);
    }


    public void SetColumnAlignment(
        HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment,
        Priority priority
    )
    {
        this.Table.SetColumnAlignment(
            this._currentColumn, horizontalAlignment, verticalAlignment, priority
        );
    }


    public void SetColumnAlignment(HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment)
    {
        this.SetColumnAlignment(horizontalAlignment, verticalAlignment,
            this.NextPriority);
    }


    public void SetColumnAlignment(HorizontalAlignment horizontalAlignment,
        Priority priority)
    {
        this.Table.SetColumnAlignment(this._currentColumn, horizontalAlignment, priority);
    }


    public void SetColumnAlignment(HorizontalAlignment horizontalAlignment)
    {
        this.SetColumnAlignment(horizontalAlignment, this.NextPriority);
    }


    public void SetColumnAlignment(VerticalAlignment verticalAlignment,
        Priority priority)
    {
        this.Table.SetColumnAlignment(this._currentColumn, verticalAlignment, priority);
    }


    public void SetColumnAlignment(VerticalAlignment verticalAlignment)
    {
        this.SetColumnAlignment(verticalAlignment, this.NextPriority);
    }


    public void SetCellAlignment(
        HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment,
        Priority priority
    )
    {
        this.Table.SetCellAlignment(
            this._currentRow, this._currentColumn, horizontalAlignment, verticalAlignment,
            priority
        );
    }


    public void SetCellAlignment(HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment)
    {
        this.SetCellAlignment(horizontalAlignment, verticalAlignment, this.NextPriority);
    }


    public void SetCellAlignment(HorizontalAlignment horizontalAlignment,
        Priority priority)
    {
        this.Table.SetCellAlignment(
            this._currentRow, this._currentColumn, horizontalAlignment, priority
        );
    }


    public void SetCellAlignment(HorizontalAlignment horizontalAlignment)
    {
        this.Table.SetCellAlignment(
            this._currentRow, this._currentColumn, horizontalAlignment, this.NextPriority
        );
    }


    public void SetCellAlignment(VerticalAlignment verticalAlignment, Priority priority)
    {
        this.Table.SetCellAlignment(
            this._currentRow, this._currentColumn, verticalAlignment, priority
        );
    }


    public void SetCellAlignment(VerticalAlignment verticalAlignment)
    {
        this.Table.SetCellAlignment(
            this._currentRow, this._currentColumn, verticalAlignment, this.NextPriority
        );
    }


    public override string ToString()
    {
        return this.Table.ToString();
    }


    public string ToStringWithoutBorders()
    {
        return this.Table.ToStringWithoutBorders();
    }


    private int _currentRow;
    private int _currentColumn;
    private Priority _priority;


    private Priority NextPriority => this._priority.Increase();

}