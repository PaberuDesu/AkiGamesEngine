using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using AkiGames.UI;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Core
{
    public static class JsonProjectSerializer
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new ColorConverter(),
                new Vector2Converter()
            },
            WriteIndented = true
        };

        public static string SerializeToJson(GameObject gameObject) =>
            JsonSerializer.Serialize(
                ConvertGameObjectToJsonObject(gameObject),
                _options
            );

        private static object ConvertGameObjectToJsonObject(GameObject gameObject)
        {
            return new
            {
                name = gameObject.ObjectName,
                gameObject.IsActive,
                gameObject.IsMouseTargetable,
                Components = gameObject.Components?.Select(ConvertComponentToJsonObject).ToArray(),
                Children = gameObject.Children?.Select(ConvertGameObjectToJsonObject).ToArray()
            };
        }

        private static Dictionary<string, object> ConvertComponentToJsonObject(GameComponent gameComponent)
        {
            var type = gameComponent.GetType();
            var dict = new Dictionary<string, object>
            {
                ["type"] = type.Name
            };

            // Сериализуем свойства
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (PropertyInfo property in properties)
            {
                var value = property.GetValue(gameComponent);
                if (value != null)
                    dict[property.Name] = value;
            }

            // Сериализуем поля
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var field in fields)
            {
                var value = field.GetValue(gameComponent);
                if (value != null)
                    dict[field.Name] = value;
            }

            return dict;
        }

        public static GameObject LoadFromJson(JsonElement rootElement) => ParseGameObject(rootElement);//TODO: define scene or prefab

        public static GameObject ParseGameObject(JsonElement element)
        {
            // Parse name
            GameObject obj =  new(
                element.TryGetProperty("name", out JsonElement nameElement) ?
                    nameElement.GetString() : ""
            );

            // Parse isActive
            if (element.TryGetProperty("IsActive", out JsonElement activeElement))
                obj.IsActive = activeElement.GetBoolean();

            // Parse isMouseTargetable
            if (element.TryGetProperty("IsMouseTargetable", out JsonElement mouseTargetElement))
                obj.IsMouseTargetable = mouseTargetElement.GetBoolean();

            // Parse components
            if (element.TryGetProperty("Components", out JsonElement componentsElement))
            {
                foreach (JsonElement componentElement in componentsElement.EnumerateArray())
                {
                    GameComponent component = ParseComponent(componentElement);

                    if (component != null)
                    {
                        if (component is UITransform uiTransform) obj.uiTransform = uiTransform.Copy();
                        obj.AddComponent(component);
                    }
                }
            }

            // Parse children
            if (element.TryGetProperty("Children", out JsonElement childrenElement))
            {
                foreach (JsonElement childElement in childrenElement.EnumerateArray())
                {
                    GameObject child = ParseGameObject(childElement);
                    obj.AddChild(child);
                }
            }

            return obj;
        }

        private static GameComponent ParseComponent(JsonElement element)
        {
            if (!element.TryGetProperty("type", out JsonElement typeElement)) return null;

            GameComponent gameComponent = CreateComponentByType(typeElement.GetString());
            if (gameComponent != null) SetPropertiesFromJson(gameComponent, element);
            
            return gameComponent;
        }

        private static readonly Dictionary<string, Type> _typeCache = [];
        public static GameComponent CreateComponentByType(string typeName)
        {
            // Проверяем кеш
            if (!_typeCache.TryGetValue(typeName, out Type componentType))
            {
                // Если нет в кеше, ищем тип
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                componentType = assemblies
                    .SelectMany(assembly => assembly.GetTypes())
                    .FirstOrDefault(t =>
                        t.Name == typeName &&
                        typeof(GameComponent).IsAssignableFrom(t) &&
                        t.GetConstructor(Type.EmptyTypes) != null);

                // Добавляем в кеш
                _typeCache[typeName] = componentType;
            }

            return componentType != null ? (GameComponent)Activator.CreateInstance(componentType) : null;
        }

        private static void SetPropertiesFromJson(GameComponent gameComponent, JsonElement element)
        {
            Type componentType = gameComponent.GetType();

            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonStringEnumConverter(),
                    new ColorConverter(),
                    new Vector2Converter()
                }
            };

            foreach (JsonProperty jsonProperty in element.EnumerateObject())
            {
                if (jsonProperty.Name == "type") continue;

                // Попробуем найти свойство
                PropertyInfo property = componentType.GetProperty(jsonProperty.Name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                // Если свойство не найдено, попробуем найти поле
                FieldInfo field = null;
                if (property == null)
                {
                    field = componentType.GetField(jsonProperty.Name,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                }

                if (property != null || field != null)
                {
                    Type targetType = property?.PropertyType ?? field.FieldType;

                    try
                    {
                        object value;

                        // Особенная обработка для Enum
                        if (targetType.IsEnum)
                        {
                            // Пытаемся десериализовать как строку
                            if (jsonProperty.Value.ValueKind == JsonValueKind.String)
                            {
                                string enumString = jsonProperty.Value.GetString();
                                value = Enum.Parse(targetType, enumString);
                            }
                            else throw new JsonException($"Invalid value for enum {targetType.Name}");
                        }
                        // Особенная обработка для Color
                        else if (targetType == typeof(Color))
                        {
                            value = JsonSerializer.Deserialize<Color>(
                                jsonProperty.Value.GetRawText(),
                                options
                            );
                        }
                        // Особенная обработка для Vector2
                        else if (targetType == typeof(Vector2))
                        {
                            value = JsonSerializer.Deserialize<Vector2>(
                                jsonProperty.Value.GetRawText(),
                                options
                            );
                        }
                        else
                        {
                            // Для остальных типов используем стандартную десериализацию
                            value = JsonSerializer.Deserialize(
                                jsonProperty.Value.GetRawText(),
                                targetType,
                                options
                            );
                        }

                        if (property != null)
                            property.SetValue(gameComponent, value);
                        else
                            field.SetValue(gameComponent, value);
                    }
                    catch (Exception ex) { ConsoleWindowController.Log($"Error: '{ex}' when tried to deserialize {jsonProperty.Name}"); }
                }
            }
        }
    }
}