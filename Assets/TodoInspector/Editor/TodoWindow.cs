﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TodoInspector
{
    internal sealed class TodoWindow : EditorWindow
    {
        private string _searchText = string.Empty;
        private TodoPriority _priorityFilter = TodoPriority.None;

        private List<TodoEntry> _allEntries = new List<TodoEntry>();
        private readonly List<TodoEntryViewModel> _viewModels = new List<TodoEntryViewModel>();
        private readonly GUIContent _buttonContent = new GUIContent("\u21BB", "Refresh (Full Scan)");

        private Vector2 _scrollPosition;

        private const float ItemHeight = 52f;
        private const float ToolbarHeight = 30f;
        private const float StatusBarHeight = 20f;
        private const float IndicatorWidth = 4f;

        private static readonly Color HighColor = new Color(0.90f, 0.30f, 0.25f, 1f);
        private static readonly Color HighestColor = new Color(0.95f, 0.15f, 0.10f, 1f);
        private static readonly Color MediumColor = new Color(0.95f, 0.75f, 0.20f, 1f);
        private static readonly Color LowColor = new Color(0.40f, 0.75f, 0.40f, 1f);
        private static readonly Color DefaultIndicatorColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);

        private static readonly Color ToolbarBgColor = new Color(0.22f, 0.22f, 0.22f, 1f);
        private static readonly Color StatusBgColor = new Color(0.20f, 0.20f, 0.20f, 1f);
        private static readonly Color BorderColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static readonly Color MessageTextColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color MetaTextColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        private static readonly Color UserTextColor = new Color(0.55f, 0.70f, 0.90f, 1f);
        private static readonly Color StatusTextColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        private GUIStyle _messageStyle;
        private GUIStyle _metaStyle;
        private GUIStyle _userStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _statusStyle;
        private Texture2D _toolbarBgTex;
        private Texture2D _statusBgTex;
        private Texture2D _borderTex;
        private Texture2D _indicatorTex;
        private Texture2D _badgeBgTex;
        private bool _stylesInitialized;
        private bool _viewModelWidthsDirty;

        [MenuItem("Tools/TODO Inspector")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<TodoWindow>();
            wnd.titleContent = new GUIContent("TODO Inspector", EditorGUIUtility.IconContent("d_UnityEditor.ConsoleWindow").image);
            wnd.minSize = new Vector2(420, 260);
        }

        private void OnEnable()
        {
            TodoScannerManager.OnTodosUpdated += OnTodosUpdated;
            RefreshData();
        }

        private void OnDisable()
        {
            TodoScannerManager.OnTodosUpdated -= OnTodosUpdated;
            DestroyTextures();
        }

        private void OnGUI()
        {
            InitStyles();
            CacheViewModelWidths();
            DrawToolbar();
            DrawListView();
            DrawStatusBar();
        }

        private void InitStyles()
        {
            if (_stylesInitialized && _toolbarBgTex != null && _borderTex != null && _indicatorTex != null)
                return;

            DestroyTextures();

            _toolbarBgTex = MakeTex(1, 1, ToolbarBgColor);
            _statusBgTex = MakeTex(1, 1, StatusBgColor);
            _borderTex = MakeTex(1, 1, BorderColor);
            _indicatorTex = MakeTex(1, 1, Color.white);
            _badgeBgTex = MakeTex(1, 1, Color.white);

            _messageStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = MessageTextColor },
                alignment = TextAnchor.MiddleLeft
            };

            _metaStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = MetaTextColor },
                alignment = TextAnchor.MiddleLeft
            };

            _userStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = UserTextColor },
                alignment = TextAnchor.MiddleLeft
            };

            _badgeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 9,
                wordWrap = false,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(5, 5, 1, 1),
                normal = { textColor = Color.white }
            };

            _statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = StatusTextColor },
                padding = new RectOffset(8, 8, 0, 0)
            };

            _stylesInitialized = true;
            _viewModelWidthsDirty = true;
        }

        private void DrawToolbar()
        {
            Rect toolbarRect = new Rect(0, 0, position.width, ToolbarHeight);
            GUI.DrawTexture(toolbarRect, _toolbarBgTex);

            Rect borderRect = new Rect(0, ToolbarHeight, position.width, 1);
            GUI.DrawTexture(borderRect, _borderTex);

            float x = 6f;
            float y = 5f;
            float fieldHeight = 20f;

            float refreshWidth = 28f;
            float priorityDropdownWidth = 90f;
            float priorityLabelWidth = 50f;
            float spacing = 8f;

            float searchWidth = position.width - x - spacing - priorityLabelWidth - 4 - priorityDropdownWidth - 6 - refreshWidth - 6 - 6;
            if (searchWidth < 80f) searchWidth = 80f;

            Rect searchRect = new Rect(x, y, searchWidth, fieldHeight);
            string newSearch = EditorGUI.TextField(searchRect, _searchText);
            if (string.IsNullOrEmpty(newSearch) && string.IsNullOrEmpty(_searchText))
            {
                Color prevColor = GUI.color;
                GUI.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                GUI.Label(new Rect(searchRect.x + 4, searchRect.y, searchRect.width - 4, searchRect.height), "Search TODOs...", EditorStyles.miniLabel);
                GUI.color = prevColor;
            }
            if (newSearch != _searchText)
            {
                _searchText = newSearch ?? string.Empty;
                ApplyFilters();
            }
            x += searchWidth + spacing;

            Rect filterLabelRect = new Rect(x, y, priorityLabelWidth, fieldHeight);
            EditorGUI.LabelField(filterLabelRect, "Priority:", _metaStyle);
            x += priorityLabelWidth + 4;

            Rect priorityRect = new Rect(x, y, priorityDropdownWidth, fieldHeight);
            TodoPriority newPriority = (TodoPriority)EditorGUI.EnumPopup(priorityRect, _priorityFilter);
            if (newPriority != _priorityFilter)
            {
                _priorityFilter = newPriority;
                ApplyFilters();
            }
            x += priorityDropdownWidth + 6;

            Rect refreshRect = new Rect(x, y, refreshWidth, fieldHeight);
            if (GUI.Button(refreshRect, _buttonContent))
            {
                TodoScannerManager.RequestFullScan();
            }
        }
        
        private void DrawListView()
        {
            float startY = ToolbarHeight + 1;
            float availableHeight = position.height - startY - StatusBarHeight - 1;
            Rect listArea = new Rect(0, startY, position.width, availableHeight);

            float totalHeight = _viewModels.Count * ItemHeight;
            Rect viewRect = new Rect(0, 0, listArea.width - 16, totalHeight);

            _scrollPosition = GUI.BeginScrollView(listArea, _scrollPosition, viewRect);

            int firstVisible = Mathf.Max(0, Mathf.FloorToInt(_scrollPosition.y / ItemHeight));
            int lastVisible = Mathf.Min(_viewModels.Count - 1, Mathf.CeilToInt((_scrollPosition.y + availableHeight) / ItemHeight));

            for (int i = firstVisible; i <= lastVisible; i++)
            {
                Rect itemRect = new Rect(0, i * ItemHeight, viewRect.width, ItemHeight);
                DrawItem(itemRect, _viewModels[i], i);
            }

            GUI.EndScrollView();
        }

        private void DrawItem(Rect rect, TodoEntryViewModel vm, int index)
        {
            if (index % 2 == 1)
            {
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.3f);
                GUI.Box(rect, GUIContent.none);
                GUI.backgroundColor = prevBg;
            }

            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && rect.Contains(Event.current.mousePosition))
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(vm.Entry.FilePath, vm.Entry.LineNumber);
                Event.current.Use();
            }

            Color indicatorColor = vm.Entry.Priority >= TodoPriority.High ? GetPriorityColor(vm.Entry.Priority) : DefaultIndicatorColor;
            Color prevGuiColor = GUI.color;
            GUI.color = indicatorColor;
            GUI.DrawTexture(new Rect(rect.x, rect.y + 2, IndicatorWidth, rect.height - 4), _indicatorTex);
            GUI.color = prevGuiColor;

            float contentX = rect.x + IndicatorWidth + 8;
            float contentWidth = rect.width - IndicatorWidth - 16;

            Rect messageRect = new Rect(contentX, rect.y + 6, contentWidth, 18);
            GUI.Label(messageRect, vm.Entry.Message, _messageStyle);

            float metaY = rect.y + 26;

            float fileWidth = contentWidth - vm.BadgeWidth - vm.UserWidth;
            if (fileWidth < 50f) fileWidth = 50f;

            Rect fileRect = new Rect(contentX, metaY, fileWidth, 16);
            GUI.Label(fileRect, vm.FileText, _metaStyle);

            float rightX = contentX + fileWidth;

            if (vm.UserText != null)
            {
                Rect userRect = new Rect(rightX, metaY, vm.UserWidth, 16);
                GUI.Label(userRect, vm.UserText, _userStyle);
                rightX += vm.UserWidth;
            }

            if (vm.BadgeText != null)
            {
                Color badgeColor = GetPriorityColor(vm.Entry.Priority);
                Rect badgeRect = new Rect(rightX, metaY, vm.BadgeWidth, 16);

                Color prevC = GUI.color;
                GUI.color = badgeColor;
                GUI.DrawTexture(badgeRect, _badgeBgTex);
                GUI.color = prevC;

                Color prevTextColor = _badgeStyle.normal.textColor;
                _badgeStyle.normal.textColor = vm.Entry.Priority >= TodoPriority.High ? Color.white : new Color(0.1f, 0.1f, 0.1f, 1f);
                GUI.Label(badgeRect, vm.BadgeText, _badgeStyle);
                _badgeStyle.normal.textColor = prevTextColor;
            }
        }

        private void DrawStatusBar()
        {
            float y = position.height - StatusBarHeight;
            Rect borderRect = new Rect(0, y - 1, position.width, 1);
            GUI.DrawTexture(borderRect, _borderTex);

            Rect statusRect = new Rect(0, y, position.width, StatusBarHeight);
            GUI.DrawTexture(statusRect, _statusBgTex);

            string statusText;
            if (_viewModels.Count == _allEntries.Count)
                statusText = _allEntries.Count + " TODO items";
            else
                statusText = _viewModels.Count + " / " + _allEntries.Count + " TODO items (filtered)";

            GUI.Label(statusRect, statusText, _statusStyle);
        }

        private void RefreshData()
        {
            var scanner = TodoScannerManager.GetScanner();
            _allEntries = scanner != null ? scanner.GetAllEntries() : new List<TodoEntry>();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            _viewModels.Clear();

            bool hasSearch = !string.IsNullOrWhiteSpace(_searchText);
            bool hasPriorityFilter = _priorityFilter != TodoPriority.None;

            for (int i = 0; i < _allEntries.Count; i++)
            {
                var entry = _allEntries[i];

                if (hasPriorityFilter && entry.Priority != _priorityFilter)
                    continue;

                if (hasSearch)
                {
                    bool match = entry.Message.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0
                              || (!string.IsNullOrEmpty(entry.User) && entry.User.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                              || entry.FilePath.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (!match)
                        continue;
                }

                _viewModels.Add(new TodoEntryViewModel
                {
                    Entry = entry,
                    FileText = ShortenPath(entry.FilePath) + " : " + entry.LineNumber,
                    BadgeText = entry.Priority != TodoPriority.None ? entry.Priority.ToString().ToUpperInvariant() : null,
                    UserText = !string.IsNullOrEmpty(entry.User) ? "@" + entry.User : null,
                    WidthsCached = false
                });
            }

            _viewModelWidthsDirty = true;
            Repaint();
        }

        private void CacheViewModelWidths()
        {
            if (!_viewModelWidthsDirty || !_stylesInitialized)
                return;

            for (int i = 0; i < _viewModels.Count; i++)
            {
                var vm = _viewModels[i];

                vm.BadgeWidth = vm.BadgeText != null
                    ? _badgeStyle.CalcSize(new GUIContent(vm.BadgeText)).x + 10
                    : 0f;

                vm.UserWidth = vm.UserText != null
                    ? _userStyle.CalcSize(new GUIContent(vm.UserText)).x + 8
                    : 0f;

                vm.WidthsCached = true;
                _viewModels[i] = vm;
            }

            _viewModelWidthsDirty = false;
        }

        private void OnTodosUpdated()
        {
            RefreshData();
        }

        private static Color GetPriorityColor(TodoPriority priority)
        {
            switch (priority)
            {
                case TodoPriority.Highest: return HighestColor;
                case TodoPriority.High: return HighColor;
                case TodoPriority.Medium: return MediumColor;
                case TodoPriority.Low: return LowColor;
                default: return DefaultIndicatorColor;
            }
        }

        private static string ShortenPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath)) return string.Empty;

            int assetsIndex = fullPath.IndexOf("/Assets/", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
                return fullPath.Substring(assetsIndex + 1);

            int lastSlash = fullPath.LastIndexOf('/');
            return lastSlash >= 0 ? fullPath.Substring(lastSlash + 1) : fullPath;
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private void DestroyTextures()
        {
            if (_toolbarBgTex != null) DestroyImmediate(_toolbarBgTex);
            if (_statusBgTex != null) DestroyImmediate(_statusBgTex);
            if (_borderTex != null) DestroyImmediate(_borderTex);
            if (_indicatorTex != null) DestroyImmediate(_indicatorTex);
            if (_badgeBgTex != null) DestroyImmediate(_badgeBgTex);
            _stylesInitialized = false;
        }
    }
}