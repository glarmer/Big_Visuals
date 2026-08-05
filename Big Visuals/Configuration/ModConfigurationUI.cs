using System;
using System.Collections.Generic;
using Rewired;
using UnityEngine;
using UnityEngine.Rendering;

namespace Big_Visuals.Configuration;

public class ModConfigurationUI : MonoBehaviour
{
    private const int MouseWheelAxis = 2;

    private List<Option> _options;
    private bool _visible;
    private int _selectedIndex;

    private bool _prevCursorVisible;
    private CursorLockMode _prevCursorLock;

    private bool _waitingForBinding = false;
    private Option _bindingTarget;

    private Texture2D _whiteTex;
    private GUIStyle _titleStyle;
    private GUIStyle _rowStyle;
    private GUIStyle _hintStyle;

    private string titleText = $"Big Visuals Settings | v {MyPluginInfo.PLUGIN_VERSION}";
    private string hintText = "F2: Open/Close • Tab or ↑/↓: Move • Enter/Click: Change • Scroll Wheel or ←/→ Arrows: Adjust Numerical Values • +/-: Scale Menu";

    private int RowHeight = 32;
    private int PanelWidth = 460;
    private int Pad = 12;

    private int TitleFontSize = 22;
    private int OptionFontSize = 16;
    private int HintFontSize = 14;
    
    private Player _player;
    private Keyboard _keyboard;
    private Mouse _mouse;
    
    public ModConfigurationUI(IntPtr ptr) : base(ptr)
    {
    }

    private Color GetColor(Color c)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            return c.linear;
        return c;
    }

    private void CalculatePanelWidth()
    {
        float maxWidth = _titleStyle.CalcSize(new GUIContent(titleText)).x;

        foreach (var option in _options)
        {
            float w = _rowStyle.CalcSize(new GUIContent($"{option.Label}: {option.DisplayValue()}")).x;
            if (w > maxWidth) maxWidth = w;
        }

        int hintWidth = CalculateHintWidth();
        maxWidth = Mathf.Max(maxWidth, hintWidth);

        PanelWidth = Mathf.Clamp((int)maxWidth + Pad * 2, 460, Screen.width - Pad * 2);
    }

    private int CalculateHintWidth()
    {
        float lineHeight = _hintStyle.CalcHeight(new GUIContent("Test"), 9999);
        float maxAllowedHeight = lineHeight * 2;

        int testWidth = 200;
        while (testWidth < Screen.width - Pad * 2)
        {
            float h = _hintStyle.CalcHeight(new GUIContent(hintText), testWidth);
            if (h <= maxAllowedHeight)
                return testWidth;

            testWidth += 20;
        }
        return Screen.width - Pad * 2;
    }

    private void Scale(int scale)
    {
        if (scale < 0 && HintFontSize < 2)
            return;

        TitleFontSize += scale * 2;
        OptionFontSize += scale * 2;
        HintFontSize += scale * 2;

        RowHeight = OptionFontSize + 16;

        CalculatePanelWidth();
    }

    public void Init()
    {
        var configurationHandler = Plugin.Instance.ConfigurationHandler;

        _options = new List<Option>
        {
            Option.Float("Render Scale", configurationHandler.ConfigRenderScale, 0.1f, 2f, 0.1f),
            Option.Int(
                "Upscaling Filter",
                configurationHandler.ConfigUpscalingFilter, 0, 4,
                displayValue: () => configurationHandler.ConfigUpscalingFilter.Value switch
                {
                    0 => "Auto",
                    1 => "Linear",
                    2 => "Nearest Neighbor",
                    3 => "FSR 1.0",
                    4 => "STP",
                    _ => "Invalid Value"
                }
            ),
            Option.Bool("Anisotropic Filtering", configurationHandler.ConfigAnisotropicFiltering),
            Option.Float("LOD Quality", configurationHandler.ConfigLODQuality, 0.1f, 10f, 0.1f),
            Option.Int("Shadowmap Resolution", configurationHandler.ConfigShadowmapResolution, 1024, 8192, 1024),
            //Option.Int("Shadow Distance", configurationHandler.ConfigShadowDistance, 0, 1000, 25),
            Option.Int("Shadow Cascades", configurationHandler.ConfigShadowCascades, 1, 10),
            Option.Bool("Soft Shadows", configurationHandler.ConfigSoftShadows),
            Option.Int(
                "Camera Antialiasing",
                configurationHandler.ConfigCameraAA, 0, 3,
                displayValue: () => configurationHandler.ConfigCameraAA.Value switch
                {
                    0 => "None",
                    1 => "FXAA",
                    2 => "SMAA",
                    3 => "TAA",
                    _ => "???"
                }
            ),
            Option.Int(
                "MSAA",
                configurationHandler.ConfigMSAA, 0, 8, 2,
                displayValue: () => configurationHandler.ConfigMSAA.Value switch
                {
                    0 => "Off",
                    2 => "2x",
                    4 => "4x",
                    8 => "8x",
                    _ => configurationHandler.ConfigMSAA.Value + "x"
                }
            ),
            Option.Int("FOV", configurationHandler.ConfigFOV, 60, 150),
            Option.InputAction("Menu Key", configurationHandler.ConfigMenuKey)
        };
        _selectedIndex = 0;

        hintText =
            $"{configurationHandler.ConfigMenuKey.Value.Split("/")[^1].ToUpper()}: Open/Close • Tab or ↑/↓: Move • Enter/Click: Change • Scroll Wheel or ←/→ Arrows: Adjust Numerical Values • +/-: Scale Menu";
    }

    private void EnsureStyles()
    {
        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTex.SetPixel(0, 0, GetColor(Color.white));
            _whiteTex.Apply();
        }

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = TitleFontSize,
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold
        };

        _rowStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = OptionFontSize,
            padding = new RectOffset(10, 10, 4, 4)
        };

        _hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = HintFontSize,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
    }

   private void Update()
    {
        if (!ReInput.isReady)
            return;

        if (_player == null)
        {
            _player = ReInput.players.GetPlayer(0);
            _keyboard = ReInput.controllers.Keyboard;
            _mouse = ReInput.controllers.Mouse;
        }
        
        if (_waitingForBinding)
        {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (_keyboard.GetKeyDown(key))
                {
                    string keyName = key.ToString();

                    _bindingTarget.StringEntry.Value = keyName;

                    Plugin.Log.LogInfo(
                        $"Rebound {_bindingTarget.Label} to {keyName}"
                    );

                    _waitingForBinding = false;
                    hintText = $"{_bindingTarget.DisplayValue()}: Open/Close • Tab or ↑/↓: Move • Enter/Click: Change • Scroll Wheel or ←/→ Arrows: Adjust Numerical Values • +/-: Scale Menu";
                    _bindingTarget = null;

                    break;
                }
            }
            return;
        }
        
        if (_keyboard.GetKeyDown(
                Plugin.Instance.ConfigurationHandler.MenuKey))
        {
            _visible = !_visible;

            if (_visible)
                OnOpened();
            else
                OnClosed();
        }


        if (!_visible || _options.Count == 0)
            return;
        
        if (_keyboard.GetKeyDown(KeyCode.Tab))
        {
            bool reverse =
                _keyboard.GetKey(KeyCode.LeftShift) ||
                _keyboard.GetKey(KeyCode.RightShift);

            CycleSelection(reverse ? -1 : 1);
        }
        
        if (_keyboard.GetKeyDown(KeyCode.Equals) ||
            _keyboard.GetKeyDown(KeyCode.Plus))
        {
            Scale(1);
        }

        if (_keyboard.GetKeyDown(KeyCode.Minus))
        {
            Scale(-1);
        }

        if (_keyboard.GetKeyDown(KeyCode.UpArrow))
            CycleSelection(-1);

        if (_keyboard.GetKeyDown(KeyCode.DownArrow))
            CycleSelection(1);
        
        if (_keyboard.GetKeyDown(KeyCode.Return) ||
            _keyboard.GetKeyDown(KeyCode.KeypadEnter))
        {
            ToggleSelected();
        }
        
        if (_keyboard.GetKeyDown(KeyCode.LeftArrow))
            AdjustNumerical(-1);

        if (_keyboard.GetKeyDown(KeyCode.RightArrow))
            AdjustNumerical(1);
        
        float scroll = _mouse.GetAxis(MouseWheelAxis);

        if (scroll > 0)
            AdjustNumerical(1);
        else if (scroll < 0)
            AdjustNumerical(-1);
    }

    private void ToggleSelected()
    {
        var option = _options[_selectedIndex];
        if (option.IsDisabled()) return;

        switch (option.Type)
        {
            case Option.OptionType.Bool:
                option.BoolEntry.Value = !option.BoolEntry.Value;
                break;

            case Option.OptionType.Int:
                int next = option.IntEntry.Value + option.Step;
                if (next > option.MaxInt) next = option.MinInt;
                option.IntEntry.Value = next;
                break;

            case Option.OptionType.Float:
                float nextFloat = option.FloatEntry.Value + option.FloatStep;
                if (nextFloat > option.MaxFloat) nextFloat = option.MinFloat;
                option.FloatEntry.Value = nextFloat;
                break;

            case Option.OptionType.InputAction:
                _waitingForBinding = true;
                _bindingTarget = option;
                break;
        }
    }

    private void AdjustNumerical(int delta)
    {
        var option = _options[_selectedIndex];
        if (option.IsDisabled()) return;

        if (option.Type == Option.OptionType.Bool)
        {
            option.BoolEntry.Value = delta > 0;
        }
        else if (option.Type == Option.OptionType.Int)
        {
            AdjustInt(option, delta);
        }
        else if (option.Type == Option.OptionType.Float)
        {
            AdjustFloat(option, delta);
        }
    }

    private static void AdjustInt(Option option, int delta)
    {
        int oldValue = option.IntEntry.Value;
        int candidate = oldValue;
        int attempts = Mathf.Max(1, Mathf.CeilToInt((option.MaxInt - option.MinInt) / (float)option.Step) + 1);

        for (int i = 0; i < attempts; i++)
        {
            int nextValue = Mathf.Clamp(candidate + (delta * option.Step), option.MinInt, option.MaxInt);
            if (nextValue == candidate)
                return;

            option.IntEntry.Value = nextValue;
            if (option.IntEntry.Value != oldValue)
                return;

            candidate = nextValue;
        }
    }

    private static void AdjustFloat(Option option, int delta)
    {
        float oldValue = option.FloatEntry.Value;
        float candidate = oldValue;
        int attempts = Mathf.Max(1, Mathf.CeilToInt((option.MaxFloat - option.MinFloat) / option.FloatStep) + 1);

        for (int i = 0; i < attempts; i++)
        {
            float nextValue = Mathf.Clamp(candidate + (delta * option.FloatStep), option.MinFloat, option.MaxFloat);
            if (Mathf.Approximately(nextValue, candidate))
                return;

            option.FloatEntry.Value = nextValue;
            if (!Mathf.Approximately(option.FloatEntry.Value, oldValue))
                return;

            candidate = nextValue;
        }
    }

    private void OnOpened()
    {
        _prevCursorVisible = Cursor.visible;
        _prevCursorLock = Cursor.lockState;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (_options.Count > 0 && _options[_selectedIndex].IsDisabled())
            CycleSelection(1);
    }

    private void OnClosed()
    {
        Cursor.visible = _prevCursorVisible;
        Cursor.lockState = _prevCursorLock;
    }

    private void CycleSelection(int delta)
    {
        if (_options.Count == 0) return;

        int startIndex = _selectedIndex;

        do
        {
            _selectedIndex = (_selectedIndex + delta) % _options.Count;

            if (_selectedIndex < 0)
                _selectedIndex += _options.Count;

            if (!_options[_selectedIndex].IsDisabled())
                break;

        } while (_selectedIndex != startIndex);
    }

    private void OnGUI()
    {
        if (!_visible) return;

        EnsureStyles();
        CalculatePanelWidth();

        float titleHeight = _titleStyle.CalcHeight(new GUIContent(titleText), PanelWidth - Pad * 2);

        float rowsHeight = _options.Count * (RowHeight + 4);
        float lineHeight = _hintStyle.CalcHeight(new GUIContent("Test"), 9999);
        float hintHeight = lineHeight * 2;

        int panelHeight = Pad + (int)titleHeight + 8 + (int)rowsHeight + Pad + (int)hintHeight;

        Rect panelRect = new Rect(20, 20, PanelWidth, panelHeight);

        GUI.color = GetColor(new Color(0f, 0f, 0f, 0.75f));
        GUI.DrawTexture(panelRect, _whiteTex);
        GUI.color = Color.white;

        var titleRect = new Rect(panelRect.x + Pad, panelRect.y + Pad, panelRect.width - Pad * 2, titleHeight);
        GUI.Label(titleRect, titleText, _titleStyle);

        float y = titleRect.yMax + 8;

        for (int i = 0; i < _options.Count; i++)
        {
            var rowRect = new Rect(panelRect.x + Pad, y, panelRect.width - Pad * 2, RowHeight);
            var option = _options[i];

            bool hover = rowRect.Contains(Event.current.mousePosition);
            if (hover && !option.IsDisabled())
                _selectedIndex = i;

            if (i == _selectedIndex && !option.IsDisabled())
            {
                GUI.color = GetColor(new Color(1f, 1f, 1f, 0.24f));
                GUI.DrawTexture(rowRect, _whiteTex);
                GUI.color = Color.white;
            }

            GUI.enabled = !option.IsDisabled();

            if (option.Label == "Menu Key" && _waitingForBinding)
            {
                GUI.color = GetColor(new Color(0f, 0f, 0f, 0.6f));
                GUI.DrawTexture(rowRect, _whiteTex);
                GUI.color = Color.white;

                GUI.Button(rowRect, $"Press any key...", _rowStyle);
            }
            else if (GUI.Button(rowRect, $"{option.Label}: {option.DisplayValue()}", _rowStyle))
            {
                if (!option.IsDisabled())
                    ToggleSelected();
            }

            GUI.enabled = true;

            y += RowHeight + 4;
        }

        var hintRect = new Rect(
            panelRect.x + Pad,
            panelRect.yMax - Pad - hintHeight,
            panelRect.width - Pad * 2,
            hintHeight
        );

        GUI.Label(hintRect, hintText, _hintStyle);
    }

    private void OnDestroy()
    {
        if (_whiteTex != null)
        {
            Destroy(_whiteTex);
            _whiteTex = null;
        }
    }
}
