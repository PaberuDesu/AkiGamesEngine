using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using AkiGames.UI;
using Microsoft.Xna.Framework.Graphics;

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

        // Черный список типов, которые нельзя сериализовать
        private static readonly HashSet<Type> _blacklistedTypes =
        [
            typeof(IntPtr),
            typeof(UIntPtr),
            typeof(GraphicsDevice),
            typeof(PresentationParameters),
            typeof(Texture2D),
            typeof(RenderTarget2D),
            typeof(SpriteBatch),
            typeof(SpriteFont),
            typeof(Effect),
            typeof(VertexBuffer),
            typeof(IndexBuffer),
            typeof(Texture),
            typeof(SurfaceFormat),
            typeof(GraphicsAdapter),
            typeof(DisplayMode),
            typeof(RasterizerState),
            typeof(DepthStencilState),
            typeof(BlendState),
            typeof(SamplerState)
        ];

        public static string SerializeToJson(GameObject gameObject)
        {
            gameObject.EnsureUniqueObjectIdsInTree();
            return JsonSerializer.Serialize(
                ConvertGameObjectToJsonObject(gameObject),
                _options
            );
        }

        private static object ConvertGameObjectToJsonObject(GameObject gameObject)
        {
            return new
            {
                gameObject.ObjectName,
                gameObject.ObjectID,
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
                .Where(p => p.CanRead && p.CanWrite && !IsBlacklisted(p.PropertyType));

            foreach (PropertyInfo property in properties)
            {
                try
                {
                    if (property.GetCustomAttribute<DontSerialize>() == null)
                    {
                        var value = property.GetValue(gameComponent);
                        if (value != null || IsSerializableTextureType(property.PropertyType))
                            dict[property.Name] = ConvertValueForSerialization(property.PropertyType, value);
                    }
                } catch{}
            }

            // Сериализуем поля
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                .Where(f => !IsBlacklisted(f.FieldType));
            foreach (var field in fields)
            {
                try
                {
                    if (field.GetCustomAttribute<DontSerialize>() == null)
                    {
                        var value = field.GetValue(gameComponent);
                        if (value != null || IsSerializableTextureType(field.FieldType))
                            dict[field.Name] = ConvertValueForSerialization(field.FieldType, value);
                    }
                }
                catch{}
            }

            return dict;
        }

        private static object ConvertValueForSerialization(Type type, object value) =>
            IsSerializableTextureType(type) ?
                Game1.GetGameTextureLink(value as Texture2D) :
                value;

        private static bool IsSerializableTextureType(Type type) =>
            type == typeof(Texture2D);

        private static bool IsBlacklisted(Type type)
        {
            if (IsSerializableTextureType(type))
                return false;

            // Проверяем базовые типы
            if (_blacklistedTypes.Contains(type))
                return true;

            // Проверяем generic-типы (например, Nullable<IntPtr>)
            if (type.IsGenericType)
            {
                foreach (var genericArg in type.GetGenericArguments())
                {
                    if (_blacklistedTypes.Contains(genericArg))
                        return true;
                }
            }

            // Проверяем массивы
            if (type.IsArray && _blacklistedTypes.Contains(type.GetElementType()))
                return true;

            return false;
        }

        public static GameObject LoadFromJson(JsonElement rootElement) => ParseGameObject(rootElement);//TODO: define scene or prefab

        public static GameObject ParseGameObject(JsonElement element)
        {
            // Parse name
            GameObject obj =  new(
                element.TryGetProperty("ObjectName", out JsonElement nameElement) ?
                    nameElement.GetString() : ""
            );

            // Parse isActive
            if (element.TryGetProperty("IsActive", out JsonElement activeElement))
                obj.IsActive = activeElement.GetBoolean();

            // Parse objectId
            if (element.TryGetProperty("ObjectID", out JsonElement objectIdElement))
                obj.ObjectID = objectIdElement.GetInt32();

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
                        if (component is UITransform uiTransform)
                        {
                            obj.uiTransform = uiTransform.Copy();
                            obj.uiTransform.gameObject = obj;
                        }
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
        public static void ClearTypeCache() => _typeCache.Clear();

        public static GameComponent CreateComponentByType(string typeName)
        {
            // Проверяем кеш
            if (!_typeCache.TryGetValue(typeName, out Type componentType))
            {
                // Если нет в кеше, ищем тип
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                componentType = assemblies
                    .SelectMany(GetLoadableTypes)
                    .FirstOrDefault(t =>
                        (t.Name == typeName || t.FullName == typeName) &&
                        typeof(GameComponent).IsAssignableFrom(t) &&
                        t.GetConstructor(Type.EmptyTypes) != null);

                // Добавляем в кеш
                _typeCache[typeName] = componentType;
            }

            return componentType != null ? (GameComponent)Activator.CreateInstance(componentType) : null;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
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

                        if (
                            (property?.GetCustomAttribute<DontSerialize>() ??
                            field?.GetCustomAttribute<DontSerialize>()) != null
                        ) continue;

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
                        else if (targetType == typeof(Texture2D))
                        {
                            value = DeserializeTexture(jsonProperty.Value);
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
                    catch (Exception ex) { Console.WriteLine($"Error: '{ex}' when tried to deserialize {jsonProperty.Name}"); }
                }
            }
        }

        private static Texture2D DeserializeTexture(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null) return null;
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new JsonException("Texture value must be a Content link string.");
            }

            string textureLink = element.GetString();
            return string.IsNullOrWhiteSpace(textureLink) ? null : Game1.LoadGameTexture(textureLink);
        }
    }
}
