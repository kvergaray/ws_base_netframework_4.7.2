using System;
using System.Data.SqlClient;

namespace WindowsService.Infrastructure.Helpers
{
    public static class DataReaderHelper
    {
        public static int ReadInt32(SqlDataReader reader, int ordinal, int defaultValue = 0)
        {
            if (reader.IsDBNull(ordinal))
            {
                return defaultValue;
            }

            var value = reader.GetValue(ordinal);
            switch (value)
            {
                case int intValue:
                    return intValue;
                case long longValue:
                    return checked((int)longValue);
                case short shortValue:
                    return shortValue;
                case byte byteValue:
                    return byteValue;
                case decimal decimalValue:
                    return decimal.ToInt32(decimalValue);
                default:
                    throw new InvalidOperationException(string.Format("No se pudo convertir el valor de la columna {0} a int. Tipo recibido: {1}", ordinal, value?.GetType().FullName ?? "desconocido"));
            }
        }

        public static int? ReadNullableInt32(SqlDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            return ReadInt32(reader, ordinal);
        }
    }
}
