using System.Text.Json;
using System.Reflection;
using System.Text.Json.Serialization;
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

        // Черный список типов, которые нельзя сериализовать (только базовые системные)
        private static readonly HashSet<Type> _blacklistedTypes =
        [
            typeof(IntPtr),
            typeof(UIntPtr),
            // При необходимости добавьте типы Veldrid (Texture, DeviceBuffer и т.д.)
        ];

        public static string SerializeToJson(GameObject gameObject) =>
            JsonSerializer.Serialize(
                ConvertGameObjectToJsonObject(gameObject),
                _options
            );

        private static object ConvertGameObjectToJsonObject(GameObject gameObject)
        {
            return new
            {
                gameObject.ObjectName,
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

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(p => p.CanRead && p.CanWrite && !IsBlacklisted(p.PropertyType));
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    if (property.GetCustomAttribute<DontSerialize>() == null)
                    {
                        var value = property.GetValue(gameComponent);
                        if (value != null)
                            dict[property.Name] = value;
                    }
                }
                catch { }
            }

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(f => !IsBlacklisted(f.FieldType));
            foreach (var field in fields)
            {
                try
                {
                    if (field.GetCustomAttribute<DontSerialize>() == null)
                    {
                        var value = field.GetValue(gameComponent);
                        if (value != null)
                            dict[field.Name] = value;
                    }
                }
                catch { }
            }

            return dict;
        }

        private static bool IsBlacklisted(Type type)
        {
            if (_blacklistedTypes.Contains(type))
                return true;
            if (type.IsGenericType)
            {
                foreach (var genericArg in type.GetGenericArguments())
                    if (_blacklistedTypes.Contains(genericArg))
                        return true;
            }
            if (type.IsArray && _blacklistedTypes.Contains(type.GetElementType()!))
                return true;
            return false;
        }

        public static GameObject LoadFromJson(JsonElement rootElement) => ParseGameObject(rootElement);

        public static GameObject ParseGameObject(JsonElement element)
        {
            string name = element.TryGetProperty("ObjectName", out JsonElement nameElement)
                ? nameElement.GetString() ?? ""
                : "";
            GameObject obj = new GameObject(name);

            if (element.TryGetProperty("IsActive", out JsonElement activeElement))
                obj.IsActive = activeElement.GetBoolean();
            if (element.TryGetProperty("IsMouseTargetable", out JsonElement mouseTargetElement))
                obj.IsMouseTargetable = mouseTargetElement.GetBoolean();

            if (element.TryGetProperty("Components", out JsonElement componentsElement))
            {
                foreach (JsonElement componentElement in componentsElement.EnumerateArray())
                {
                    GameComponent? component = ParseComponent(componentElement);
                    if (component != null)
                    {
                        if (component is UITransform uiTransform)
                        {
                            obj.uiTransform = uiTransform.Copy();
                            obj.uiTransform.gameObject = obj;
                        }
                        obj.AddComponent(component);
                    }
                }
            }

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

        private static GameComponent? ParseComponent(JsonElement element)
        {
            if (!element.TryGetProperty("type", out JsonElement typeElement))
                return null;

            GameComponent? gameComponent = CreateComponentByType(typeElement.GetString()!);
            if (gameComponent != null)
                SetPropertiesFromJson(gameComponent, element);
            return gameComponent;
        }

        private static readonly Dictionary<string, Type?> _typeCache = [];

        public static GameComponent? CreateComponentByType(string typeName)
        {
            if (!_typeCache.TryGetValue(typeName, out Type? componentType))
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                componentType = assemblies
                    .SelectMany(assembly => assembly.GetTypes())
                    .FirstOrDefault(t =>
                        t.Name == typeName &&
                        typeof(GameComponent).IsAssignableFrom(t) &&
                        t.GetConstructor(Type.EmptyTypes) != null);
                _typeCache[typeName] = componentType;
            }
            return componentType != null ? (GameComponent?)Activator.CreateInstance(componentType) : null;
        }

        private static void SetPropertiesFromJson(GameComponent gameComponent, JsonElement element)
        {
            Type componentType = gameComponent.GetType();

            foreach (JsonProperty jsonProperty in element.EnumerateObject())
            {
                if (jsonProperty.Name == "type") continue;

                PropertyInfo? property = componentType.GetProperty(jsonProperty.Name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                FieldInfo? field = null;
                if (property == null)
                {
                    field = componentType.GetField(jsonProperty.Name,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                }

                if (property != null || field != null)
                {
                    Type targetType = property?.PropertyType ?? field!.FieldType;

                    try
                    {
                        if ((property?.GetCustomAttribute<DontSerialize>() ??
                             field?.GetCustomAttribute<DontSerialize>()) != null)
                            continue;

                        object value;

                        if (targetType.IsEnum)
                        {
                            if (jsonProperty.Value.ValueKind == JsonValueKind.String)
                                value = Enum.Parse(targetType, jsonProperty.Value.GetString()!);
                            else
                                throw new JsonException($"Invalid value for enum {targetType.Name}");
                        }
                        else if (targetType == typeof(Color))
                        {
                            value = JsonSerializer.Deserialize<Color>(jsonProperty.Value.GetRawText(), _options)!;
                        }
                        else if (targetType == typeof(Vector2))
                        {
                            value = JsonSerializer.Deserialize<Vector2>(jsonProperty.Value.GetRawText(), _options)!;
                        }
                        else
                        {
                            value = JsonSerializer.Deserialize(jsonProperty.Value.GetRawText(), targetType, _options)!;
                        }

                        if (property != null)
                            property.SetValue(gameComponent, value);
                        else
                            field!.SetValue(gameComponent, value);
                    }
                    catch (Exception ex)
                    {
                        ConsoleWindowController.Log($"Error: '{ex}' when tried to deserialize {jsonProperty.Name}");
                    }
                }
            }
        }
    }
}