using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace ReflectionSeConsoleApp
{
    public static class ReflectionUtil
    {
        private static readonly StringComparer defComparer = StringComparer.OrdinalIgnoreCase;

        public static Object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        public static Type FindType(String fullName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        public static void CopyTo<T>(this T sourceObject, ref T destObject, params String[] exeptProperties) where T : class
        {
            var hashSet = new HashSet<String>();
            if (exeptProperties != null)
            {
                hashSet.UnionWith(exeptProperties);
            }

            sourceObject.CopyTo(ref destObject, hashSet);
        }

        public static void CopyTo<T>(this T sourceObject, ref T destObject, ISet<String> exeptProperties) where T : class
        {
            if (sourceObject == null)
            {
                throw new ArgumentNullException("destObject");
            }

            var type = typeof(T);
            if (destObject == null)
            {
                destObject = Activator.CreateInstance<T>();
            }

            var properties = type.GetProperties();

            foreach (var propertyInfo in properties)
            {
                if ((exeptProperties != null && exeptProperties.Contains(propertyInfo.Name)) || !propertyInfo.CanRead || !propertyInfo.CanWrite)
                {
                    continue;
                }

                var value = propertyInfo.GetValue(sourceObject, null);
                propertyInfo.SetValue(destObject, value, null);
            }
        }

        public static void CopyFrom<T>(this T destObject, T sourceObject, params String[] exeptProperties) where T : class
        {
            var hashSet = new HashSet<String>();
            if (exeptProperties != null)
            {
                hashSet.UnionWith(exeptProperties);
            }

            destObject.CopyFrom(sourceObject, hashSet);
        }
        public static void CopyFrom<T>(this T destObject, T sourceObject, ISet<String> exeptProperties) where T : class
        {
            if (destObject == null)
            {
                throw new ArgumentNullException("destObject");
            }

            if (sourceObject == null)
            {
                throw new ArgumentNullException("sourceObject");
            }

            var type = typeof(T);
            var properties = type.GetProperties();

            foreach (var propertyInfo in properties)
            {
                if ((exeptProperties != null && exeptProperties.Contains(propertyInfo.Name)) || !propertyInfo.CanRead || !propertyInfo.CanWrite)
                {
                    continue;
                }

                var value = propertyInfo.GetValue(sourceObject, null);
                propertyInfo.SetValue(destObject, value, null);
            }
        }

        public static Object GetValue(Object obj, String propertyName, params Object[] args)
        {
            if (obj == null)
            {
                return null;
            }

            var type = obj.GetType();

            var propInfo = type.GetProperty(propertyName);
            if (propInfo == null)
            {
                return null;
            }

            return propInfo.GetValue(obj, args);
        }

        public static void SetValue(Object obj, String propertyName, Object value, params Object[] args)
        {
            if (obj == null)
            {
                return;
            }

            var type = obj.GetType();

            var propInfo = type.GetProperty(propertyName);
            if (propInfo == null)
            {
                return;
            }

            propInfo.SetValue(obj, value, args);
        }

        public static Object Call(Object obj, String methodName, params Object[] args)
        {
            if (obj == null)
            {
                return null;
            }

            var type = obj.GetType();

            var methodInfo = type.GetMethod(methodName);
            if (methodInfo == null)
            {
                return null;
            }

            return methodInfo.Invoke(obj, args);
        }

        public static Type MakeTypeNullable(Type type)
        {
            if (type.IsValueType)
            {
                return typeof(Nullable<>).MakeGenericType(type);
            }

            return type;
        }

        public static Object GetPropertyValue(Object obj, String propertyPath)
        {
            Object result;
            if (!TryGetPropertyValue(obj, propertyPath, out result))
            {
                throw new Exception("Unable to get object");
            }

            return result;
        }

        public static void SetPropertyValue(Object obj, String propertyPath, Object value)
        {
            if (!TrySetPropertyValue(obj, propertyPath, value))
            {
                throw new Exception("Unable to get object");
            }
        }

        public static bool TryGetPropertyValue(Object obj, String propertyPath, out Object result)
        {
            result = null;

            var properties = propertyPath.Split('.');

            foreach (var propertyName in properties)
            {
                Object value;
                if (!FindPropertyValue(obj, propertyName, out value))
                {
                    return false;
                }

                obj = value;
            }

            result = obj;
            return true;
        }

        public static bool TrySetPropertyValue(Object obj, String propertyPath, Object value)
        {
            var properties = propertyPath.Split('.');

            var lastObj = obj;

            for (int i = 0; i < properties.Length - 1; i++)
            {
                var propertyName = properties[i];

                Object result;
                if (!FindPropertyValue(lastObj, propertyName, out result))
                {
                    return false;
                }

                lastObj = result;
            }

            var lastPropertyName = properties[properties.Length - 1];

            if (lastObj == null || String.IsNullOrWhiteSpace(lastPropertyName))
            {
                return false;
            }

            var objType = obj.GetType();

            if (obj is IDictionary)
            {
                if (lastPropertyName.StartsWith("@"))
                    lastPropertyName = lastPropertyName.Substring(1);

                var dict = (IDictionary)obj;
                dict[lastPropertyName] = value;

                return true;
            }

            if (IsDictionary(obj))
            {
                var prop = GetProperty(objType, "Item");
                if (prop == null)
                {
                    return false;
                }

                prop.SetValue(obj, value, new Object[] { lastPropertyName });
            }
            else if (obj is IEnumerable)
            {
                var list = TryCastToList(obj);

                var itemIndex = GetItemIndexByName(lastPropertyName, list);
                if (itemIndex == null)
                {
                    return false;
                }

                list[itemIndex.Value] = value;
            }
            else
            {
                var prop = GetProperty(objType, lastPropertyName);
                if (prop == null)
                {
                    return false;
                }

                prop.SetValue(obj, value);
            }

            return true;
        }

        private static bool FindPropertyValue(Object obj, String propertyName, out Object result)
        {
            result = default(Object);

            if (obj == null || String.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            var objType = obj.GetType();

            if (obj is IDictionary)
            {
                if (propertyName.StartsWith("@"))
                    propertyName = propertyName.Substring(1);

                var dict = (IDictionary)obj;
                if (!dict.Contains(propertyName))
                {
                    return false;
                }

                result = dict[propertyName];
            }
            else if (IsDictionary(obj))
            {
                var prop = GetProperty(objType, "Item");
                if (prop == null)
                {
                    return false;
                }

                result = prop.GetValue(obj, new Object[] { propertyName });
            }
            else if (obj is IEnumerable)
            {
                var list = TryCastToList(obj);

                var itemIndex = GetItemIndexByName(propertyName, list);
                if (itemIndex == null)
                {
                    return false;
                }

                result = list[itemIndex.Value];
            }
            else
            {
                var prop = GetProperty(objType, propertyName);
                if (prop == null)
                {
                    return false;
                }

                result = prop.GetValue(obj);
            }

            return true;
        }

        public static IList TryCastToList(Object obj)
        {
            var list = obj as IList;
            if (list != null)
            {
                return list;
            }

            if (obj is IEnumerable)
            {
                var collection = obj as IEnumerable;

                var array = collection.Cast<Object>().ToArray();
                return array;
            }

            return null;
        }

        public static bool ContainsProperty(Object obj, String propertyPath)
        {
            var properties = propertyPath.Split('.');

            foreach (var propertyName in properties)
            {
                Object result;
                if (!FindPropertyValue(obj, propertyName, out result))
                {
                    return false;
                }

                obj = result;
            }

            return true;
        }

        public static bool IsList(Object obj)
        {
            if (obj == null)
                return false;

            return IsSubclassOf(obj, typeof(IList<>));
        }

        public static bool IsDictionary(Object obj)
        {
            if (obj == null)
                return false;

            return IsSubclassOf(obj, typeof(IDictionary<,>));
        }

        public static bool IsSubclassOf(Object obj, Type type)
        {
            var objType = obj.GetType();
            return IsSubclassOf(objType, type);
        }

        public static bool IsSubclassOf(Type objType, Type baseType)
        {
            var simpleBaseType = GetSimpleType(baseType);
            if (objType == baseType || objType == simpleBaseType)
            {
                return true;
            }

            var interfacesTypes = objType.GetInterfaces();
            foreach (var interfaceType in interfacesTypes)
            {
                var simpleInterfaceType = GetSimpleType(interfaceType);
                if (simpleBaseType.IsAssignableFrom(simpleInterfaceType))
                {
                    return true;
                }
            }

            var objBaseSimpleType = GetSimpleType(objType.BaseType);
            if (objType.BaseType != typeof(Object) && (baseType.IsAssignableFrom(objType.BaseType) || baseType.IsAssignableFrom(objBaseSimpleType)))
            {
                return true;
            }

            return false;
        }

        public static bool IsEnumerableType(Object obj)
        {
            return (obj is IEnumerable);
        }

        public static bool IsGenericType(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var type = obj.GetType();
            return (type.IsGenericType && !type.IsGenericTypeDefinition);
        }

        public static Object GetGenericOrElementType(Object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var type = obj.GetType();
            if (IsGenericType(obj))
            {
                return type.GetGenericArguments()[0];
            }

            if (IsEnumerableType(obj))
            {
                var collection = (IEnumerable)obj;
                var types = GetAllTypes(collection);
                if (types.Count > 1)
                {
                    throw new Exception();
                }

                return types.First();
            }

            return obj;
        }

        private static PropertyInfo GetProperty(Type type, String name)
        {
            var properties = (from n in type.GetProperties()
                              where n.Name == name
                              select n).ToList();

            if (properties.Count > 1)
            {
                properties = (from n in properties
                              where n.DeclaringType == type
                              select n).ToList();
            }

            return properties.FirstOrDefault();
        }

        private static ISet<Type> GetAllTypes(IEnumerable collection)
        {
            var typesSet = new SortedSet<Type>();
            foreach (var item in collection)
            {
                if (item != null)
                {
                    typesSet.Add(item.GetType());
                }
            }

            if (typesSet.Count == 0)
            {
                typesSet.Add(typeof(Object));
            }

            return typesSet;
        }

        private static Type GetSimpleType(Type type)
        {
            if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition();
            }

            return type;
        }

        private static int? TryConvertToInt(String value)
        {
            int @int;
            if (int.TryParse(value, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out @int))
                return @int;

            return null;
        }

        private static int? GetItemIndexByName(String propertyName, IList list)
        {
            var numeralIndex = -1;
            if (defComparer.Equals(propertyName, "@first"))
            {
                return 0;
            }

            if (defComparer.Equals(propertyName, "@last"))
            {
                return list.Count - 1;
            }

            if (propertyName.StartsWith("@"))
            {
                var strIndex = propertyName.Substring(1);

                var index = TryConvertToInt(strIndex);
                if (index != null && index >= 0 || index < list.Count)
                {
                    return index.Value;
                }
            }

            return null;
        }
    }
}
