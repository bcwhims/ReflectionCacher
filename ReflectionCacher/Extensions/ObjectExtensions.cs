using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace ReflectionCacher.Extensions
{
    public static class ObjectExtensions
    {
        private static readonly ConcurrentDictionary<Type, TypeConverter> Converters = new ConcurrentDictionary<Type, TypeConverter>();
        private static readonly ConcurrentDictionary<Type, object> Defaults = new ConcurrentDictionary<Type, object>();
        private static readonly Type TypeHelperType = typeof(Types<>);

        public static T Convert<T>(this object obj)
        {
            return (T)Convert(obj, Types<T>.ConverterInstance, Types<T>.Type, Types<T>.DefaultValue);
        }

        public static T ConvertOrDefault<T>(this object obj)
        {
            try
            {
                return (T)Convert(obj, Types<T>.ConverterInstance, Types<T>.Type, Types<T>.DefaultValue);
            }
            catch
            {
                return Types<T>.DefaultValue;
            }

        }

        public static object Convert(this object obj, Type type)
        {
            return Convert(obj, Types.GetConverter(type), type, Types.GetDefault(type));
        }

        private static object Convert(object obj, TypeConverter converter, Type type, object defaultValue)
        {
            return obj == DBNull.Value || obj == null ?
                defaultValue :
                Convert(obj, converter, type);
        }

        public static object Convert(object obj, TypeConverter converter, Type type)
        {
            return (converter.CanConvertFrom(obj.GetType()) ?
                converter.ConvertFrom(obj) :
                System.Convert.ChangeType(obj, type));
        }
    }
}
