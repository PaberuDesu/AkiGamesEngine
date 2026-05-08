using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using AkiGames.Core.Serialization;
using AkiGames.Events;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts.Inspector
{
    public class InspectorColorInputField : GameComponent
    {
        private readonly List<InspectorColorSlider> _sliders = [];
        private readonly Dictionary<InspectorColorChannel, InspectorColorSlider> _slidersByChannel = [];

        private Image _previewImage;
        private GameObject _menu;
        private Color _value = Color.White;
        private bool _hasValue;
        private bool _hasPendingApply;

        [DontSerialize, HideInInspector] public MemberInfo Info { private get; set; }
        [DontSerialize, HideInInspector] public GameComponent Component { private get; set; }
        [DontSerialize, HideInInspector] public bool IsEditable { private get; set; } = true;
        [DontSerialize, HideInInspector]
        public Color Value
        {
            private get => _value;
            set
            {
                _value = value;
                _hasValue = true;
                RefreshVisuals();
            }
        }

        public override void Awake()
        {
            _previewImage = gameObject.GetComponent<Image>();
            _menu = FindChild("ColorMenu");
            CollectSliders(gameObject);

            foreach (InspectorColorSlider slider in _sliders)
            {
                _slidersByChannel[slider.channel] = slider;
                slider.Bind(this);
            }

            if (!_hasValue && TryReadColor(out Color memberValue))
                _value = memberValue;

            gameObject.IsMouseTargetable = IsEditable;
            HideMenu();
            RefreshVisuals();
        }

        public override void Update()
        {
            if (
                _menu?.IsActive == true &&
                (Input.LMB.IsDown || Input.RMB.IsDown) &&
                (Input.MouseHoverTarget == null || !gameObject.IsParentFor(Input.MouseHoverTarget))
            )
            {
                HideMenu();
            }
        }

        public override void OnMouseDown()
        {
            if (!IsEditable || _menu == null) return;

            _menu.IsActive = !_menu.IsActive;
            if (_menu.IsActive)
                _menu.RefreshBounds(gameObject.uiTransform);
        }

        internal void SetChannel(InspectorColorChannel channel, int value)
        {
            if (!IsEditable) return;

            Color color = channel switch
            {
                InspectorColorChannel.R => new Color(value, _value.G, _value.B, _value.A),
                InspectorColorChannel.G => new Color(_value.R, value, _value.B, _value.A),
                InspectorColorChannel.B => new Color(_value.R, _value.G, value, _value.A),
                InspectorColorChannel.A => new Color(_value.R, _value.G, _value.B, value),
                _ => _value
            };

            if (color == _value) return;

            _value = color;
            RefreshPreview();
            RefreshSlider(channel);
            SetMemberValue(color);
            _hasPendingApply = true;
        }

        internal byte GetChannel(InspectorColorChannel channel) =>
            channel switch
            {
                InspectorColorChannel.R => _value.R,
                InspectorColorChannel.G => _value.G,
                InspectorColorChannel.B => _value.B,
                InspectorColorChannel.A => _value.A,
                _ => 0
            };

        private void RefreshVisuals()
        {
            RefreshPreview();

            foreach (InspectorColorSlider slider in _sliders)
                slider.RefreshValue();
        }

        private void RefreshPreview() =>
            _previewImage?.fillColor = _value;

        private void RefreshSlider(InspectorColorChannel channel)
        {
            if (_slidersByChannel.TryGetValue(channel, out InspectorColorSlider slider))
                slider.RefreshValue();
        }

        internal void ApplyPendingValue()
        {
            if (!_hasPendingApply) return;

            _hasPendingApply = false;
            InspectorChangeApplier.Apply(Component);
        }

        private void HideMenu() => _menu?.IsActive = false;

        private bool TryReadColor(out Color color)
        {
            color = Color.White;

            try
            {
                object value = Info switch
                {
                    FieldInfo fieldInfo => fieldInfo.GetValue(Component),
                    PropertyInfo propertyInfo => propertyInfo.GetValue(Component),
                    _ => null
                };

                if (value is Color memberColor)
                {
                    color = memberColor;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Color inspector field {Info?.Name} can't be read: {ex.Message}");
            }

            return false;
        }

        private void SetMemberValue(Color color)
        {
            if (Info is null || Component is null) return;

            try
            {
                if (Info is FieldInfo fieldInfo)
                    fieldInfo.SetValue(Component, color);

                if (Info is PropertyInfo propertyInfo)
                    propertyInfo.SetValue(Component, color);

            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Color inspector field {Info.Name} can't be changed: {ex.Message}");
            }
        }

        private GameObject FindChild(string objectName)
        {
            foreach (GameObject child in gameObject.Children)
            {
                if (child.ObjectName == objectName)
                    return child;
            }

            return null;
        }

        private void CollectSliders(GameObject root)
        {
            InspectorColorSlider slider = root.GetComponent<InspectorColorSlider>();
            if (slider != null)
                _sliders.Add(slider);

            foreach (GameObject child in root.Children)
                CollectSliders(child);
        }
    }
}
