using System;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using AkiGames.UI;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.Core.Serialization
{
    public static class JsonProjectSerializer
    {
        private static readonly HashSet<string> _prefabLinksBeingLoaded =
            new(StringComparer.OrdinalIgnoreCase);

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
            if (!string.IsNullOrWhiteSpace(gameObject.SourcePrefabLink))
            {
                GameObject prefabObject = CreatePrefabObjectFromLink(gameObject.SourcePrefabLink);
                if (prefabObject != null)
                    return ConvertLinkedGameObjectToJsonObject(gameObject, prefabObject);
            }

            return ConvertFullGameObjectToJsonObject(gameObject);
        }

        private static Dictionary<string, object> ConvertFullGameObjectToJsonObject(GameObject gameObject)
        {
            return new Dictionary<string, object>
            {
                ["ObjectName"] = gameObject.ObjectName,
                ["ObjectID"] = gameObject.ObjectID,
                ["IsActive"] = gameObject.IsActive,
                ["IsMouseTargetable"] = gameObject.IsMouseTargetable,
                ["Components"] = gameObject.Components?.Select(ConvertComponentToJsonObject).ToArray(),
                ["Children"] = gameObject.Children?.Select(ConvertGameObjectToJsonObject).ToArray()
            };
        }

        private static Dictionary<string, object> ConvertLinkedGameObjectToJsonObject(
            GameObject gameObject,
            GameObject sourceObject
        )
        {
            Dictionary<string, object> dict = new()
            {
                ["Link"] = gameObject.SourcePrefabLink
            };

            AddGameObjectOverrideProperties(dict, gameObject, sourceObject, includeObjectName: true);

            Dictionary<string, object>[] componentOverrides =
                ConvertComponentOverridesToJsonObjects(gameObject, sourceObject);
            if (componentOverrides.Length > 0)
                dict["Components"] = componentOverrides;

            object[] childOverrides = ConvertChildOverridesToJsonObjects(gameObject, sourceObject);
            if (childOverrides != null)
                dict["Children"] = childOverrides;

            return dict;
        }

        private static Dictionary<string, object> ConvertGameObjectOverrideToJsonObject(
            GameObject gameObject,
            GameObject sourceObject
        )
        {
            Dictionary<string, object> dict = new();
            AddGameObjectOverrideProperties(dict, gameObject, sourceObject, includeObjectName: true);

            Dictionary<string, object>[] componentOverrides =
                ConvertComponentOverridesToJsonObjects(gameObject, sourceObject);
            if (componentOverrides.Length > 0)
                dict["Components"] = componentOverrides;

            object[] childOverrides = ConvertChildOverridesToJsonObjects(gameObject, sourceObject);
            if (childOverrides != null)
                dict["Children"] = childOverrides;

            return dict;
        }

        private static void AddGameObjectOverrideProperties(
            Dictionary<string, object> dict,
            GameObject gameObject,
            GameObject sourceObject,
            bool includeObjectName
        )
        {
            if (includeObjectName || gameObject.ObjectName != sourceObject.ObjectName)
                dict["ObjectName"] = gameObject.ObjectName;

            if (gameObject.ObjectID > 0 && gameObject.ObjectID != sourceObject.ObjectID)
                dict["ObjectID"] = gameObject.ObjectID;

            if (gameObject.IsActive != sourceObject.IsActive)
                dict["IsActive"] = gameObject.IsActive;

            if (gameObject.IsMouseTargetable != sourceObject.IsMouseTargetable)
                dict["IsMouseTargetable"] = gameObject.IsMouseTargetable;
        }

        private static Dictionary<string, object>[] ConvertComponentOverridesToJsonObjects(
            GameObject gameObject,
            GameObject sourceObject
        )
        {
            List<Dictionary<string, object>> overrides = [];
            HashSet<GameComponent> usedSourceComponents = [];

            foreach (GameComponent component in gameObject.Components)
            {
                GameComponent sourceComponent = FindSourceComponentForSerialization(
                    component,
                    sourceObject.Components,
                    usedSourceComponents
                );

                if (sourceComponent == null)
                {
                    overrides.Add(ConvertComponentToJsonObject(component));
                    continue;
                }

                usedSourceComponents.Add(sourceComponent);
                Dictionary<string, object> componentOverride =
                    ConvertComponentOverrideToJsonObject(component, sourceComponent);
                if (componentOverride.Count > 1)
                    overrides.Add(componentOverride);
            }

            return [.. overrides];
        }

        private static GameComponent FindSourceComponentForSerialization(
            GameComponent component,
            List<GameComponent> sourceComponents,
            HashSet<GameComponent> usedSourceComponents
        )
        {
            Type componentType = component.GetType();
            return sourceComponents.FirstOrDefault(sourceComponent =>
                !usedSourceComponents.Contains(sourceComponent) &&
                componentType.IsAssignableFrom(sourceComponent.GetType())
            );
        }

        private static Dictionary<string, object> ConvertComponentOverrideToJsonObject(
            GameComponent component,
            GameComponent sourceComponent
        )
        {
            Dictionary<string, object> componentJson = ConvertComponentToJsonObject(component);
            Dictionary<string, object> sourceJson = ConvertComponentToJsonObject(sourceComponent);
            Dictionary<string, object> overrideJson = new()
            {
                ["type"] = componentJson["type"]
            };

            foreach (KeyValuePair<string, object> pair in componentJson)
            {
                string key = pair.Key;
                object value = pair.Value;
                if (key == "type") continue;

                if (
                    !sourceJson.TryGetValue(key, out object sourceValue) ||
                    !SerializedValuesEqual(value, sourceValue)
                )
                {
                    overrideJson[key] = value;
                }
            }

            return overrideJson;
        }

        private static object[] ConvertChildOverridesToJsonObjects(
            GameObject gameObject,
            GameObject sourceObject
        )
        {
            List<GameObject> children = gameObject.Children;
            List<GameObject> sourceChildren = sourceObject.Children;

            if (children.Count == 0 && sourceChildren.Count > 0)
                return [];

            List<object> overrides = [];
            HashSet<GameObject> usedSourceChildren = [];

            foreach (GameObject child in children)
            {
                GameObject sourceChild = FindSourceChildForSerialization(
                    child,
                    sourceChildren,
                    usedSourceChildren
                );

                if (sourceChild == null)
                {
                    overrides.Add(ConvertGameObjectToJsonObject(child));
                    continue;
                }

                usedSourceChildren.Add(sourceChild);
                Dictionary<string, object> childOverride =
                    ConvertGameObjectOverrideToJsonObject(child, sourceChild);
                if (childOverride.Count > 1)
                    overrides.Add(childOverride);
            }

            if (overrides.Count == 0) return null;
            return [.. overrides];
        }

        private static GameObject FindSourceChildForSerialization(
            GameObject child,
            List<GameObject> sourceChildren,
            HashSet<GameObject> usedSourceChildren
        )
        {
            return sourceChildren.FirstOrDefault(sourceChild =>
                !usedSourceChildren.Contains(sourceChild) &&
                string.Equals(sourceChild.ObjectName, child.ObjectName, StringComparison.Ordinal)
            );
        }

        private static bool SerializedValuesEqual(object value, object sourceValue) =>
            JsonSerializer.Serialize(value, _options) ==
            JsonSerializer.Serialize(sourceValue, _options);

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
                        if (value != null || IsSerializableLinkType(property.PropertyType))
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
                        if (value != null || IsSerializableLinkType(field.FieldType))
                            dict[field.Name] = ConvertValueForSerialization(field.FieldType, value);
                    }
                }
                catch{}
            }

            return dict;
        }

        private static object ConvertValueForSerialization(Type type, object value)
        {
            if (IsSerializableTextureType(type))
                return Game1.GetGameTextureLink(value as Texture2D);

            if (IsSerializableGameObjectReferenceType(type))
                return (value as GameObject)?.ObjectID ?? 0;

            if (IsSerializableGameComponentReferenceType(type))
                return (value as GameComponent)?.gameObject?.ObjectID ?? 0;

            return value;
        }

        private static bool IsSerializableLinkType(Type type) =>
            IsSerializableTextureType(type) ||
            IsSerializableGameObjectReferenceType(type) ||
            IsSerializableGameComponentReferenceType(type);

        private static bool IsSerializableTextureType(Type type) =>
            type == typeof(Texture2D);

        private static bool IsSerializableGameObjectReferenceType(Type type) =>
            type == typeof(GameObject);

        private static bool IsSerializableGameComponentReferenceType(Type type) =>
            typeof(GameComponent).IsAssignableFrom(type);

        private static bool IsBlacklisted(Type type)
        {
            if (IsSerializableLinkType(type))
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

        public static GameObject LoadFromJson(JsonElement rootElement) => ParseGameObject(rootElement);

        public static GameObject LoadPrefabLink(string prefabLink)
        {
            List<PendingGameObjectReference> pendingGameObjectReferences = [];
            GameObject linkedObject = CreateGameObjectFromPrefabLink(
                prefabLink,
                createFallback: false
            );
            if (linkedObject == null) return null;

            ResolveGameObjectReferences(linkedObject, pendingGameObjectReferences);
            return linkedObject;
        }

        private static bool TryGetJsonProperty(
            JsonElement element,
            string propertyName,
            out JsonElement value
        )
        {
            if (element.TryGetProperty(propertyName, out value))
                return true;

            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static string GetLink(JsonElement element)
        {
            if (
                element.TryGetProperty("Link", out JsonElement linkElement) &&
                linkElement.ValueKind == JsonValueKind.String
            )
            {
                return linkElement.GetString();
            }

            return "";
        }

        private static GameObject CreatePrefabObjectFromLink(string prefabLink)
        {
            string normalizedLink = NormalizePrefabLink(prefabLink);
            if (string.IsNullOrWhiteSpace(normalizedLink)) return null;

            if (!_prefabLinksBeingLoaded.Add(normalizedLink))
            {
                Console.WriteLine($"Prefab link {normalizedLink} can't be loaded: cyclic prefab link.");
                return null;
            }

            try
            {
                string jsonString = LoadAkiJsonString(normalizedLink);
                if (!string.IsNullOrWhiteSpace(jsonString))
                {
                    JsonElement prefabElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    return LoadFromJson(prefabElement).Copy();
                }

                string prefabKey = GetPrefabDictionaryKey(normalizedLink);
                if (!string.IsNullOrWhiteSpace(prefabKey) &&
                    Game1.Prefabs.TryGetValue(prefabKey, out GameObject loadedPrefab))
                {
                    return loadedPrefab.Copy();
                }

                Console.WriteLine($"Prefab link {normalizedLink} can't be loaded: prefab wasn't found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Prefab link {normalizedLink} can't be loaded because of an error: {ex.Message}");
                return null;
            }
            finally
            {
                _prefabLinksBeingLoaded.Remove(normalizedLink);
            }
        }

        private static string LoadAkiJsonString(string prefabLink)
        {
            string contentRelativePath = GetContentRelativeAkiPath(prefabLink);
            if (string.IsNullOrWhiteSpace(contentRelativePath)) return null;

            string assetName = Path.ChangeExtension(contentRelativePath, null)
                .Replace('\\', '/');

            if (Game1.ProjectContent != null)
            {
                try { return Game1.ProjectContent.Load<string>(assetName); }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(Game1.ContentRoot))
            {
                string rawPath = Path.Combine(
                    Game1.ContentRoot,
                    contentRelativePath.Replace('/', Path.DirectorySeparatorChar)
                );
                if (File.Exists(rawPath))
                    return File.ReadAllText(rawPath);
            }

            return null;
        }

        private static string NormalizePrefabLink(string prefabLink)
        {
            if (string.IsNullOrWhiteSpace(prefabLink)) return "";

            string normalizedPath = prefabLink.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(normalizedPath))
            {
                int contentIndex = normalizedPath.LastIndexOf("/Content/", StringComparison.OrdinalIgnoreCase);
                if (contentIndex >= 0)
                    normalizedPath = normalizedPath[(contentIndex + 1)..];
                else
                    return "";
            }

            if (!normalizedPath.Contains('/'))
                normalizedPath = $"Prefabs/{normalizedPath}";

            if (normalizedPath.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
                normalizedPath = normalizedPath["Content/".Length..];

            if (string.Equals(Path.GetExtension(normalizedPath), ".aki", StringComparison.OrdinalIgnoreCase))
                normalizedPath = Path.ChangeExtension(normalizedPath, null).Replace('\\', '/');

            return $"Content/{normalizedPath.TrimStart('/')}";
        }

        private static string GetContentRelativeAkiPath(string prefabLink)
        {
            string normalizedLink = NormalizePrefabLink(prefabLink);
            if (
                !normalizedLink.StartsWith("Content/", StringComparison.OrdinalIgnoreCase) ||
                normalizedLink.Length <= "Content/".Length
            )
            {
                return "";
            }

            string relativePath = normalizedLink["Content/".Length..];

            if (string.IsNullOrWhiteSpace(Path.GetExtension(relativePath)))
                relativePath += ".aki";

            return relativePath;
        }

        private static string GetPrefabDictionaryKey(string prefabLink) =>
            Path.GetFileNameWithoutExtension(GetContentRelativeAkiPath(prefabLink));

        public static GameObject ParseGameObject(JsonElement element)
        {
            List<PendingGameObjectReference> pendingGameObjectReferences = [];
            GameObject rootObject = ParseGameObject(element, pendingGameObjectReferences);
            ResolveGameObjectReferences(rootObject, pendingGameObjectReferences);
            return rootObject;
        }

        private static GameObject ParseGameObject(
            JsonElement element,
            List<PendingGameObjectReference> pendingGameObjectReferences
        )
        {
            string link = GetLink(element);
            if (!string.IsNullOrWhiteSpace(link))
            {
                GameObject linkedObject = CreateGameObjectFromPrefabLink(
                    link,
                    createFallback: true
                );
                ApplyGameObjectOverrides(
                    linkedObject,
                    element,
                    pendingGameObjectReferences,
                    preserveExistingChildren: true
                );
                return linkedObject;
            }

            // Parse name
            GameObject obj = new(
                TryGetJsonProperty(element, "ObjectName", out JsonElement nameElement) ?
                    nameElement.GetString() : ""
            );

            ApplyGameObjectOverrides(
                obj,
                element,
                pendingGameObjectReferences,
                preserveExistingChildren: false
            );

            return obj;
        }

        private static GameObject CreateGameObjectFromPrefabLink(
            string prefabLink,
            bool createFallback
        )
        {
            GameObject linkedObject = CreatePrefabObjectFromLink(prefabLink);
            if (linkedObject == null)
            {
                if (!createFallback) return null;
                linkedObject = new(GetPrefabDictionaryKey(prefabLink));
            }

            string normalizedLink = NormalizePrefabLink(prefabLink);
            linkedObject.SourcePrefabLink = string.IsNullOrWhiteSpace(normalizedLink) ?
                prefabLink :
                normalizedLink;

            return linkedObject;
        }

        private static void ApplyGameObjectOverrides(
            GameObject obj,
            JsonElement element,
            List<PendingGameObjectReference> pendingGameObjectReferences,
            bool preserveExistingChildren
        )
        {
            if (TryGetJsonProperty(element, "ObjectName", out JsonElement nameElement))
                obj.ObjectName = nameElement.GetString() ?? "";

            if (TryGetJsonProperty(element, "IsActive", out JsonElement activeElement))
                obj.IsActive = activeElement.GetBoolean();

            if (TryGetJsonProperty(element, "ObjectID", out JsonElement objectIdElement))
                obj.ObjectID = objectIdElement.GetInt32();

            if (TryGetJsonProperty(element, "IsMouseTargetable", out JsonElement mouseTargetElement))
                obj.IsMouseTargetable = mouseTargetElement.GetBoolean();

            // Parse components
            if (TryGetJsonProperty(element, "Components", out JsonElement componentsElement))
            {
                ApplyComponentOverrides(obj, componentsElement, pendingGameObjectReferences);
            }

            // Parse children
            if (TryGetJsonProperty(element, "Children", out JsonElement childrenElement))
            {
                if (preserveExistingChildren)
                    ApplyChildOverrides(obj, childrenElement, pendingGameObjectReferences);
                else
                    ReplaceChildren(obj, childrenElement, pendingGameObjectReferences);
            }
        }

        private static void ReplaceChildren(
            GameObject obj,
            JsonElement childrenElement,
            List<PendingGameObjectReference> pendingGameObjectReferences
        )
        {
            obj.Children = [];
            foreach (JsonElement childElement in childrenElement.EnumerateArray())
            {
                GameObject child = ParseGameObject(
                    childElement,
                    pendingGameObjectReferences
                );
                obj.AddChild(child);
            }
        }

        private static void ApplyChildOverrides(
            GameObject obj,
            JsonElement childrenElement,
            List<PendingGameObjectReference> pendingGameObjectReferences
        )
        {
            if (childrenElement.GetArrayLength() == 0)
            {
                obj.Children = [];
                return;
            }

            HashSet<GameObject> usedChildren = [];
            foreach (JsonElement childElement in childrenElement.EnumerateArray())
            {
                GameObject child = FindChildForOverride(obj.Children, childElement, usedChildren);
                if (child == null)
                {
                    obj.AddChild(ParseGameObject(childElement, pendingGameObjectReferences));
                    continue;
                }

                usedChildren.Add(child);
                ApplyGameObjectOverrides(
                    child,
                    childElement,
                    pendingGameObjectReferences,
                    preserveExistingChildren: true
                );
            }
        }

        private static GameObject FindChildForOverride(
            List<GameObject> children,
            JsonElement childElement,
            HashSet<GameObject> usedChildren
        )
        {
            if (
                TryGetJsonProperty(childElement, "ObjectName", out JsonElement nameElement) &&
                nameElement.ValueKind == JsonValueKind.String
            )
            {
                string objectName = nameElement.GetString() ?? "";
                GameObject child = children.FirstOrDefault(existingChild =>
                    !usedChildren.Contains(existingChild) &&
                    string.Equals(existingChild.ObjectName, objectName, StringComparison.Ordinal)
                );
                if (child != null) return child;
            }

            if (
                TryGetJsonProperty(childElement, "ObjectID", out JsonElement idElement) &&
                idElement.ValueKind == JsonValueKind.Number
            )
            {
                int objectId = idElement.GetInt32();
                if (objectId <= 0) return null;

                return children.FirstOrDefault(existingChild =>
                    !usedChildren.Contains(existingChild) &&
                    existingChild.ObjectID == objectId
                );
            }

            return null;
        }

        private static void ApplyComponentOverrides(
            GameObject obj,
            JsonElement componentsElement,
            List<PendingGameObjectReference> pendingGameObjectReferences
        )
        {
            HashSet<GameComponent> usedComponents = [];

            foreach (JsonElement componentElement in componentsElement.EnumerateArray())
            {
                if (!componentElement.TryGetProperty("type", out JsonElement typeElement)) continue;

                string typeName = typeElement.GetString();
                if (string.IsNullOrWhiteSpace(typeName)) continue;

                Type componentType = ResolveComponentType(typeName);
                GameComponent component = componentType == null ?
                    null :
                    obj.Components.FirstOrDefault(existingComponent =>
                        !usedComponents.Contains(existingComponent) &&
                        componentType.IsAssignableFrom(existingComponent.GetType())
                    );
                bool isNewComponent = component == null;

                if (isNewComponent)
                {
                    component = CreateComponentByType(typeName);
                    if (component == null) continue;
                }
                else
                {
                    usedComponents.Add(component);
                }

                SetPropertiesFromJson(component, componentElement, pendingGameObjectReferences);
                if (isNewComponent)
                {
                    AddParsedComponent(obj, component);
                    usedComponents.Add(component);
                }
                else if (component is UITransform uiTransform)
                {
                    obj.uiTransform = uiTransform;
                    obj.uiTransform.gameObject = obj;
                }
            }
        }

        private static void AddParsedComponent(GameObject obj, GameComponent component)
        {
            if (component is UITransform uiTransform)
            {
                obj.uiTransform = uiTransform.Copy();
                obj.uiTransform.gameObject = obj;
            }

            obj.AddComponent(component);
        }

        private static readonly Dictionary<string, Type> _typeCache = [];
        public static void ClearTypeCache() => _typeCache.Clear();

        public static GameComponent CreateComponentByType(string typeName)
        {
            // Проверяем кеш
            if (!_typeCache.TryGetValue(typeName, out Type componentType))
            {
                // Если нет в кеше, ищем тип
                componentType = AppDomain.CurrentDomain.GetAssemblies()
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

        private static Type ResolveComponentType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            if (!_typeCache.TryGetValue(typeName, out Type componentType))
            {
                componentType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(GetLoadableTypes)
                    .FirstOrDefault(t =>
                        (t.Name == typeName || t.FullName == typeName) &&
                        typeof(GameComponent).IsAssignableFrom(t) &&
                        t.GetConstructor(Type.EmptyTypes) != null);

                _typeCache[typeName] = componentType;
            }

            return componentType;
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

        private static void SetPropertiesFromJson(
            GameComponent gameComponent,
            JsonElement element,
            List<PendingGameObjectReference> pendingGameObjectReferences
        )
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
                        else if (targetType == typeof(GameObject))
                        {
                            value = DeserializeGameObjectReference(
                                jsonProperty.Value,
                                gameComponent,
                                property ?? (MemberInfo)field,
                                targetType,
                                pendingGameObjectReferences
                            );
                        }
                        else if (IsSerializableGameComponentReferenceType(targetType))
                        {
                            value = DeserializeGameObjectReference(
                                jsonProperty.Value,
                                gameComponent,
                                property ?? (MemberInfo)field,
                                targetType,
                                pendingGameObjectReferences
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
                    catch (Exception ex) { Console.WriteLine($"Error: '{ex}' when tried to deserialize {jsonProperty.Name}"); }
                }
            }
        }

        private static GameObject DeserializeGameObjectReference(
            JsonElement element,
            GameComponent component,
            MemberInfo memberInfo,
            Type targetType,
            List<PendingGameObjectReference> pendingGameObjectReferences
        )
        {
            int objectId = element.ValueKind switch
            {
                JsonValueKind.Number => element.GetInt32(),
                JsonValueKind.String when int.TryParse(element.GetString(), out int parsedId) => parsedId,
                JsonValueKind.Null => 0,
                _ => throw new JsonException("GameObject value must be an object id.")
            };

            if (objectId <= 0) return null;

            pendingGameObjectReferences.Add(
                new PendingGameObjectReference(component, memberInfo, targetType, objectId)
            );
            return null;
        }

        private static void ResolveGameObjectReferences(
            GameObject rootObject,
            List<PendingGameObjectReference> pendingGameObjectReferences
        )
        {
            if (rootObject == null || pendingGameObjectReferences.Count == 0) return;

            Dictionary<int, GameObject> objectsById = [];
            AddGameObjectsById(rootObject, objectsById);

            foreach (PendingGameObjectReference reference in pendingGameObjectReferences)
            {
                try
                {
                    if (!objectsById.TryGetValue(reference.ObjectId, out GameObject linkedObject))
                    {
                        Console.WriteLine(
                            $"{reference.TargetType.Name} reference {reference.ObjectId} for {reference.MemberInfo.Name} can't be resolved."
                        );
                        continue;
                    }

                    object linkedValue = reference.TargetType == typeof(GameObject) ?
                        linkedObject :
                        linkedObject.GetComponent(reference.TargetType);

                    if (linkedValue == null)
                    {
                        Console.WriteLine(
                            $"{reference.TargetType.Name} reference {reference.ObjectId} for {reference.MemberInfo.Name} can't be resolved: component wasn't found."
                        );
                        continue;
                    }

                    SetMemberValue(reference.Component, reference.MemberInfo, linkedValue);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"{reference.TargetType.Name} reference {reference.ObjectId} for {reference.MemberInfo.Name} can't be resolved because of an error: {ex.Message}"
                    );
                }
            }
        }

        private static void AddGameObjectsById(
            GameObject gameObject,
            Dictionary<int, GameObject> objectsById
        )
        {
            if (gameObject.ObjectID > 0 && !objectsById.ContainsKey(gameObject.ObjectID))
                objectsById[gameObject.ObjectID] = gameObject;

            foreach (GameObject child in gameObject.Children)
                AddGameObjectsById(child, objectsById);
        }

        private static void SetMemberValue(
            GameComponent component,
            MemberInfo memberInfo,
            object value
        )
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                propertyInfo.SetValue(component, value);
                return;
            }

            if (memberInfo is FieldInfo fieldInfo)
                fieldInfo.SetValue(component, value);
        }

        private sealed class PendingGameObjectReference(
            GameComponent component,
            MemberInfo memberInfo,
            Type targetType,
            int objectId
        )
        {
            public GameComponent Component { get; } = component;
            public MemberInfo MemberInfo { get; } = memberInfo;
            public Type TargetType { get; } = targetType;
            public int ObjectId { get; } = objectId;
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
