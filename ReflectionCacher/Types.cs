using System;
using System.Linq;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace ReflectionCacher
{
    public class Types
    {
        private static readonly ConcurrentDictionary<Type,TypeConverter> converters = new ConcurrentDictionary<Type, TypeConverter>();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> properties = new ConcurrentDictionary<Type, PropertyInfo[]>();
        private static readonly ConcurrentDictionary<Type, FieldInfo[]> fields = new ConcurrentDictionary<Type, FieldInfo[]>();
        private static readonly ConcurrentDictionary<Type, MethodInfo[]> methods = new ConcurrentDictionary<Type, MethodInfo[]>();
        private static readonly ConcurrentDictionary<Type, object> defaultValues = new ConcurrentDictionary<Type, object>();
        private static readonly Type GenericTypesType = typeof(Types<>);

        public static TypeConverter GetConverter(Type type)
        {
            return GetInstanceFromDictionary(type, converters, "ConverterInstance");
        }

        public static object GetDefault(Type type)
        {
            return GetInstanceFromDictionary(type, defaultValues, "DefaultValue");
        }

        private static T GetInstanceFromDictionary<T>(Type type, ConcurrentDictionary<Type, T> dictionary, string property)
        {
            return dictionary.GetOrAdd(type, (theType) => GetInstance<T>(theType, property));
        }

        private static T GetInstance<T>(Type type, string property)
        {
            var instance = GenericTypesType
                .MakeGenericType(type)
                .GetProperty(property, BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty)
                .GetValue(null);
            return instance is T ? (T)instance : Types<T>.DefaultValue;
        }
    }
    public class Types<T>
    {
        private static readonly Lazy<Type> type = new Lazy<Type>(() => typeof(T));
        private static readonly Lazy<TypeConverter> converter = new Lazy<TypeConverter>(() => TypeDescriptor.GetConverter(type.Value));
        private static readonly Lazy<PropertyInfo[]> properties = new Lazy<PropertyInfo[]>(() => type.Value.GetProperties());
        private static readonly Lazy<FieldInfo[]> fields = new Lazy<FieldInfo[]>(() => type.Value.GetFields());
        private static readonly Lazy<MethodInfo[]> methods = new Lazy<MethodInfo[]>(() => type.Value.GetMethods());
        private static readonly Lazy<T> defaultValue = new Lazy<T>(() => default(T));
        private static Lazy<ConcurrentDictionary<string, Action<T, object>>> setters =
            new Lazy<ConcurrentDictionary<string, Action<T, object>>>(() => new ConcurrentDictionary<string, Action<T, object>>());
        private static Lazy<ConcurrentDictionary<string, Func<T, object>>> getters =
            new Lazy<ConcurrentDictionary<string, Func<T, object>>>(() => new ConcurrentDictionary<string, Func<T, object>>());
        public static TypeConverter ConverterInstance
        {
            get { return converter.Value; }
        }

        public static PropertyInfo[] Properties
        {
            get { return properties.Value; }
        }

        public static FieldInfo[] Fields
        {
            get { return fields.Value; }
        }

        public static MethodInfo[] Methods
        {
            get { return methods.Value; }
        }

        public static Type Type
        {
            get { return type.Value; }
        }

        public static T DefaultValue
        {
            get { return defaultValue.Value; }
        }

        public static Action<T, object> GetSetMethodDelegate(PropertyInfo property)
        {
            return setters.Value.GetOrAdd(property.Name, (propertyInfo) =>
            (Action<T, object>)Delegate.CreateDelegate(typeof(Action<T, object>), null, property.GetSetMethod()));
        }

        public static Func<T, object> GetGetMethodDelegate(PropertyInfo property)
        {
            return getters.Value.GetOrAdd(property.Name, (propertyInfo) =>
            (Func<T, object>)Delegate.CreateDelegate(typeof(Func<T, object>), null, property.GetGetMethod()));
        }

        public static Action<T, TPropType> GetSetMethodDelegate<TPropType>(string propertyName)
        {
            var property = properties.Value.FirstOrDefault(a => a.Name == propertyName);
            return PropertyDelegates<TPropType>.GetSetMethodDelegate(property);
        }

        public static Func<T, TPropType> GetGetMethodDelegate<TPropType>(string propertyName)
        {
            var property = properties.Value.FirstOrDefault(a => a.Name == propertyName);
            return PropertyDelegates<TPropType>.GetGetMethodDelegate(property);
        }

        private static class PropertyDelegates<TPropType>
        {
            private static ConcurrentDictionary<string, Action<T, TPropType>> setters = new ConcurrentDictionary<string, Action<T, TPropType>>();
            private static ConcurrentDictionary<string, Func<T, TPropType>> getters = new ConcurrentDictionary<string, Func<T, TPropType>>();
            internal static Action<T, TPropType> GetSetMethodDelegate(PropertyInfo property)
            {
                return setters.GetOrAdd(property.Name, (propertyInfo) =>
                (Action<T, TPropType>)
                   Delegate.CreateDelegate(typeof(Action<T, TPropType>), null,
                       property.GetSetMethod()));
            }

            internal static Func<T, TPropType> GetGetMethodDelegate(PropertyInfo property)
            {
                return getters.GetOrAdd(property.Name, (propertyInfo) =>
                    (Func<T, TPropType>)
                    Delegate.CreateDelegate(typeof(Func<T, TPropType>), null,
                        property.GetGetMethod()));

            }
        }

    }
}
