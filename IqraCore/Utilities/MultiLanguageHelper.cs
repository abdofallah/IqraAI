using IqraCore.Attributes;
using IqraCore.Entities.Helpers;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace IqraCore.Utilities
{
    public static class MultiLanguageHelper
    {
        private static readonly ConcurrentDictionary<Type, TypeInfo> _typeCache = new ConcurrentDictionary<Type, TypeInfo>();

        public static FunctionReturnResult<bool?> UpdateObjectMultiLanguages<T>(T obj, List<string> newLanguages, List<string> oldLanguages)
        {
            var result = new FunctionReturnResult<bool?>();
            result.Data = true;

            var typeInfo = GetOrCreateTypeInfo(typeof(T));
            if (typeInfo.HasMultiLanguageProperties)
            {
                UpdateObjectMultiLanguagesRecursive(obj, newLanguages, oldLanguages, result);
            }

            result.Success = true;
            return result;
        }

        private static TypeInfo GetOrCreateTypeInfo(Type type)
        {
            return _typeCache.GetOrAdd(type, (t) =>
            {
                var info = new TypeInfo();
                var properties = t.GetProperties();

                foreach (var property in properties)
                {
                    if (property.GetCustomAttribute<MultiLanguagePropertyAttribute>() != null)
                    {
                        info.HasMultiLanguageProperties = true;
                        info.MultiLanguageProperties.Add(property);
                    }
                    else
                    {
                        var propertyType = property.PropertyType;

                        bool propertyIsString = propertyType == typeof(string);
                        bool propertyIsType = propertyType == typeof(Type);
                        if (propertyIsString || propertyIsType) continue;

                        bool propertyIsClass = propertyType.IsClass;
                        bool propertyIsIEnumerable = typeof(IEnumerable).IsAssignableFrom(propertyType);
                        
                        if (propertyIsClass && !propertyIsIEnumerable)
                        {
                            var nestedTypeInfo = GetOrCreateTypeInfo(propertyType);
                            if (nestedTypeInfo.HasMultiLanguageProperties)
                            {
                                info.HasMultiLanguageProperties = true;
                                info.NestedMultiLanguageProperties.Add(property);
                            }
                        }
                        
                        if (propertyIsIEnumerable)
                        {
                            var propertyElementType = propertyType.IsArray ? propertyType.GetElementType() : propertyType.GetGenericArguments()[0];
                            
                            bool propertyElementIsString = propertyElementType == typeof(string);
                            bool propertyElementIsType = propertyElementType == typeof(Type);
                            if (propertyElementIsString || propertyElementIsType) continue;

                            bool propertyElementIsClass = propertyElementType.IsClass;
                            bool propertyElementIsIEnumerable = typeof(IEnumerable).IsAssignableFrom(propertyElementType);

                            if (propertyElementIsClass)
                            {
                                var nestedTypeInfo = GetOrCreateTypeInfo(propertyElementType);
                                if (nestedTypeInfo.HasMultiLanguageProperties)
                                {
                                    info.HasMultiLanguageProperties = true;
                                    info.NestedMultiLanguageProperties.Add(property);
                                }
                            }
                        }
                    }
                }

                return info;
            });
        }

        private static void UpdateObjectMultiLanguagesRecursive(object obj, List<string> newLanguages, List<string> oldLanguages, FunctionReturnResult<bool?> result)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var typeInfo = GetOrCreateTypeInfo(type);

            foreach (var property in typeInfo.MultiLanguageProperties)
            {
                UpdateMultiLanguageProperty(obj, property, newLanguages, oldLanguages, result);
                if (!result.Data.Value) return;
            }

            foreach (var property in typeInfo.NestedMultiLanguageProperties)
            {
                var value = property.GetValue(obj);
                if (value is IEnumerable<object> enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        UpdateObjectMultiLanguagesRecursive(item, newLanguages, oldLanguages, result);
                        if (!result.Data.Value) return;
                    }
                }
                else
                {
                    UpdateObjectMultiLanguagesRecursive(value, newLanguages, oldLanguages, result);
                    if (!result.Data.Value) return;
                }
            }
        }

        private static void UpdateMultiLanguageProperty(object obj, PropertyInfo property, List<string> newLanguages, List<string> oldLanguages, FunctionReturnResult<bool?> result)
        {
            var attribute = property.GetCustomAttribute<MultiLanguagePropertyAttribute>();

            Type dictionaryType = property.PropertyType;

            Type[] genericArgs = dictionaryType.GetGenericArguments();

            Type valueType = genericArgs[1];

            var value = property.GetValue(obj);
            if (value == null)
            {
                Type concreteType = dictionaryType.IsInterface || dictionaryType.IsAbstract
                ? typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType)
                : dictionaryType;

                value = Activator.CreateInstance(concreteType);
                property.SetValue(obj, value);
            }

            var dictionaryObj = value as IDictionary;

            // Remove languages that are in oldLanguages but not in newLanguages
            var languagesToRemove = oldLanguages.Except(newLanguages).ToList();
            foreach (var langToRemove in languagesToRemove)
            {
                dictionaryObj.Remove(langToRemove);
            }

            // Add new languages that are in newLanguages but not in oldLanguages
            var languagesToAdd = newLanguages.Except(oldLanguages).ToList();
            foreach (var langToAdd in languagesToAdd)
            {
                dictionaryObj[langToAdd] = GetDefaultValue(valueType);
            }

            // Check if we have the required number of languages
            if (attribute.LanguagesRequired > 0 && dictionaryObj.Count < attribute.LanguagesRequired)
            {
                result.Code = 1;
                result.Message = $"Property {property.Name} requires at least {attribute.LanguagesRequired} language(s).";
                result.Success = false;
                result.Data = false;
            }
            else if (attribute.LanguagesRequired == -1 && dictionaryObj.Count < newLanguages.Count)
            {
                result.Code = 2;
                result.Message = $"Property {property.Name} requires all languages to be present.";
                result.Success = false;
                result.Data = false;
            }
        }

        private static object GetDefaultValue(Type type)
        {
            // Check if the type is nullable
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return null;
            }

            if (type == typeof(string))
                return string.Empty;
            else if (type == typeof(int))
                return 0;
            else if (type == typeof(bool))
                return false;
            else if (type == typeof(DateTime))
                return DateTime.MinValue;
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return Activator.CreateInstance(type);
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                return Activator.CreateInstance(type);
            else if (type.IsClass)
                return null;
            else
                return Activator.CreateInstance(type);
        }

        private class TypeInfo
        {
            public bool HasMultiLanguageProperties { get; set; }
            public List<PropertyInfo> MultiLanguageProperties { get; } = new List<PropertyInfo>();
            public List<PropertyInfo> NestedMultiLanguageProperties { get; } = new List<PropertyInfo>();
        }
    }
}
