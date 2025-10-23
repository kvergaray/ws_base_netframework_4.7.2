using System;
using System.Data;
using System.Globalization;

public static class DataReaderHelper
{
    public static int SafeGetOrdinal(IDataRecord record, string columnName, int fallbackIndexIfMissing)
    {
        try { return record.GetOrdinal(columnName); }
        catch (IndexOutOfRangeException) { return fallbackIndexIfMissing; }
    }
    public static int ReadInt32(IDataRecord record, int ordinal, int defaultValue = 0)
    {
        if (record.IsDBNull(ordinal))
            return defaultValue;

        var value = record.GetValue(ordinal);
        return ToInt32(value, ordinal);
    }

    public static int? ReadNullableInt32(IDataRecord record, int ordinal)
    {
        if (record.IsDBNull(ordinal))
            return null;

        return ReadInt32(record, ordinal);
    }

    // ---- Extras útiles (opcionales) ----
    public static long ReadInt64(IDataRecord record, int ordinal, long defaultValue = 0)
    {
        if (record.IsDBNull(ordinal))
            return defaultValue;

        var v = record.GetValue(ordinal);
        switch (v)
        {
            case long l: return l;
            case int i: return i;
            case short s: return s;
            case byte b: return b;
            case decimal d: return decimal.ToInt64(d);
            case double d: return checked((long)d);
            case float f: return checked((long)f);
            case string s when long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var l):
                return l;
            case bool bo: return bo ? 1L : 0L;
            case IConvertible c:
                return c.ToInt64(CultureInfo.InvariantCulture);
            default:
                throw BuildCastError(ordinal, v, "long");
        }
    }

    public static string ReadString(IDataRecord record, int ordinal, string defaultValue = "")
    {
        if (record.IsDBNull(ordinal))
            return defaultValue;

        var v = record.GetValue(ordinal);
        return v?.ToString() ?? defaultValue;
    }

    public static bool ReadBoolean(IDataRecord record, int ordinal, bool defaultValue = false)
    {
        if (record.IsDBNull(ordinal))
            return defaultValue;

        var v = record.GetValue(ordinal);
        switch (v)
        {
            case bool b: return b;
            case int i: return i != 0;
            case long l: return l != 0;
            case short s: return s != 0;
            case byte b2: return b2 != 0;
            case decimal d: return d != 0m;
            case double d2: return d2 != 0.0;
            case float f: return f != 0f;
            case string s:
                if (bool.TryParse(s, out var parsed)) return parsed;
                if (int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i2)) return i2 != 0;
                break;
            case IConvertible c:
                return c.ToBoolean(CultureInfo.InvariantCulture);
        }
        throw BuildCastError(ordinal, v, "bool");
    }

    public static DateTime? ReadNullableDateTime(IDataRecord record, int ordinal)
    {
        if (record.IsDBNull(ordinal))
            return null;

        var v = record.GetValue(ordinal);
        switch (v)
        {
            case DateTime dt: return dt;
            case string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt):
                return dt;
            case IConvertible c:
                return c.ToDateTime(CultureInfo.InvariantCulture);
            default:
                throw BuildCastError(ordinal, v, "DateTime");
        }
    }

    // ---------- Internos ----------
    private static int ToInt32(object value, int ordinal)
    {
        switch (value)
        {
            case int i: return i;
            case long l: return checked((int)l);
            case short s: return s;
            case byte b: return b;
            case decimal d: return decimal.ToInt32(d);
            case double d: return checked((int)d);
            case float f: return checked((int)f);
            case string s when int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i):
                return i;
            case bool bo: return bo ? 1 : 0;
            case IConvertible c:
                return c.ToInt32(CultureInfo.InvariantCulture);
            default:
                throw BuildCastError(ordinal, value, "int");
        }
    }

    private static InvalidOperationException BuildCastError(int ordinal, object value, string targetType)
    {
        var src = value?.GetType().FullName ?? "desconocido";
        return new InvalidOperationException(
            $"No se pudo convertir la columna {ordinal} a {targetType}. Tipo recibido: {src} | Valor: {value ?? "NULL"}");
    }
}
