using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Input;

namespace WDE.Common.Avalonia.DnD;

public static class IDataTransferExtensions
{
    private sealed class DataTransfer<T> : IDataTransfer, IDataTransferItem
        where T : class
    {
        public T Value { get; }

        private static DataFormat CustomFormat =
            DataFormat.CreateStringApplicationFormat($"wde-{new string(typeof(T).FullName?.Where(c => char.IsAsciiLetterOrDigit(c) || c == '.' || c == '-').ToArray())}");

        public DataTransfer(T value)
        {
            Value = value;
        }

        public object? TryGetRaw(DataFormat format)
        {
            if (format == CustomFormat)
                return this;
            return null;
        }

        public IReadOnlyList<DataFormat> Formats { get; } = [CustomFormat];
        IReadOnlyList<DataFormat> IDataTransfer.Formats => Formats;

        IReadOnlyList<IDataTransferItem> IDataTransfer.Items => [this];

        public void Dispose() { }
    }

    extension(IDataTransfer)
    {
        public static IDataTransfer Create<T>(T data) where T : class => new DataTransfer<T>(data);
    }

    public static bool TryGet<T>(this IDataTransfer data, [NotNullWhen(true)] out T? value)
        where T : class
    {
        value = default;

        if (data is DataTransfer<T> wrapper)
        {
            value = wrapper.Value;
        }

        return value != null;
    }
}