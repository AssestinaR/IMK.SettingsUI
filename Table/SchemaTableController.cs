using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace IMK.SettingsUI.Table
{
    internal sealed class SchemaTableController : MonoBehaviour
    {
        private ITableSchema _schema;
        private ITableDataSet _data;
        private RectTransform _viewport;
        private RectTransform _content;
        private ScrollRect _scroll;
        private RectTransform _header;
        private readonly List<RowView> _rows = new List<RowView>();
        private bool _dirtyFull;
        private bool _computedWidths;
        private float[] _colWidths;
        private float[] _baseColWidths; // measured once, then scaled to available width
        private bool _measuredBase;
        private float _totalWidth;
        private const float HeaderHeight = 26f;
        private const float RowHeight = 24f;
        private float _lastAppliedAvail = -1f;
        private int _selectedIndex = -1;

        private class Cell
        {
            public RectTransform Root;
            public InputField Input;
            public Dropdown Dropdown;
            public Toggle Toggle;
            public Text Readonly;
            // dropdown/toggle visuals removed/simplified
        }
        private class RowView
        {
            public RectTransform Root;
            public Image Bg;
            public RectTransform Highlight; // overlay for selection
            public IRowAdapter Adapter;
            public int Index;
            public List<Cell> Cells = new List<Cell>();
        }

        public int SelectedIndex => _selectedIndex;

        public void Init(ITableSchema schema, ITableDataSet data, RectTransform viewport)
        {
            _schema = schema; _data = data; _viewport = viewport;
            _content = new GameObject("RowsContent").AddComponent<RectTransform>(); _content.SetParent(_viewport,false);
            _content.anchorMin=new Vector2(0f,1f); _content.anchorMax=new Vector2(0f,1f); _content.pivot=new Vector2(0f,1f);
            _content.anchoredPosition = Vector2.zero; _content.sizeDelta = new Vector2(0f,0f);
            _scroll = _viewport.gameObject.GetComponent<ScrollRect>(); if (_scroll==null) _scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            var mask = _viewport.gameObject.GetComponent<RectMask2D>(); if (mask==null) mask = _viewport.gameObject.AddComponent<RectMask2D>();
            _scroll.viewport=_viewport; _scroll.content=_content; _scroll.horizontal=false; _scroll.vertical=true; _scroll.movementType=ScrollRect.MovementType.Clamped; _scroll.inertia=true; _scroll.scrollSensitivity = IMK.SettingsUI.Theme.ThemeMetrics.ScrollSensitivity * 30f;
            var vpImage = _viewport.GetComponent<Image>(); if (vpImage==null){ vpImage = _viewport.gameObject.AddComponent<Image>(); vpImage.color = new Color(0f,0f,0f,0f); } vpImage.raycastTarget = true;
            _dirtyFull = true; _computedWidths = false; _measuredBase = false; _totalWidth = 0f; _lastAppliedAvail = -1f;
            if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log($"[SchemaTable] Init schemaCols={schema?.Columns?.Count ?? 0} count={data?.Count ?? -1} vp=({_viewport.rect.width}x{_viewport.rect.height})");
        }
        void OnEnable(){ _dirtyFull = true; }
        void OnRectTransformDimensionsChange(){ _dirtyFull = true; }
        void Update()
        {
            if (_viewport!=null && !_computedWidths){ var w=_viewport.rect.width; var h=_viewport.rect.height; if (w>1f && h>1f) _dirtyFull=true; }
            if (_dirtyFull) RebuildAll(); else UpdateVisibleRows();
        }
        private void RebuildAll()
        {
            _dirtyFull = false;
            ComputeColumnWidths();
            if (!_computedWidths) return;
            float vpW = Mathf.Max(0f, _viewport.rect.width);
            _lastAppliedAvail = vpW;
            _content.sizeDelta = new Vector2(Mathf.Max(_totalWidth, vpW), _content.sizeDelta.y);
            BuildHeaderOrUpdate();
            float preservedY = _content.anchoredPosition.y;
            float vpH = Mathf.Max(0f, _viewport.rect.height);
            int visible = Mathf.Max(1, Mathf.FloorToInt((vpH - HeaderHeight)/RowHeight));
            float total = HeaderHeight + (_data?.Count ?? 0) * RowHeight;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, total);
            int need = Mathf.Min((_data?.Count ?? 0), visible + 2);
            while (_rows.Count < need) _rows.Add(CreateRowView());
            for (int i=0;i<_rows.Count;i++) _rows[i].Root.gameObject.SetActive(i<need);
            // re-apply column geometry for existing rows (in case widths changed)
            for (int i=0;i<_rows.Count;i++) ApplyColumnGeometryToRow(_rows[i]);
            if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log($"[SchemaTable] RebuildAll need={need} totalRows={_data?.Count ?? -1} vp=({_viewport.rect.width}x{_viewport.rect.height}) totalW={_totalWidth}");
            UpdateVisibleRows();

            // clamp preservedY to valid scroll range and reapply to content
            float maxScroll = Mathf.Max(0f, total - _viewport.rect.height);
            preservedY = Mathf.Clamp(preservedY, 0f, maxScroll);
            _content.anchoredPosition = new Vector2(0f, preservedY);
        }
        private void ComputeColumnWidths()
        {
            var cols = _schema?.Columns; if (cols == null || cols.Count==0){ _computedWidths=false; return; }
            float avail = _viewport.rect.width; if (avail <= 1f){ _computedWidths=false; return; }
            // measure base widths once (titles + sample data); avoid per-frame allocations during transitions
            if (!_measuredBase)
            {
                _baseColWidths = new float[cols.Count];
                float MeasureTitle(string s)
                {
                    if (string.IsNullOrEmpty(s)) return 10f;
                    var font = IMK.SettingsUI.Theme.ThemeColors.DefaultFont; int fontSize = 14;
                    // Use TextGenerator to avoid creating GameObjects
                    var gen = new TextGenerator();
                    var settings = new TextGenerationSettings
                    {
                        font = font,
                        fontSize = fontSize,
                        fontStyle = FontStyle.Normal,
                        color = Color.white,
                        richText = false,
                        lineSpacing = 1f,
                        scaleFactor = 1f,
                        resizeTextForBestFit = false,
                        verticalOverflow = VerticalWrapMode.Overflow,
                        horizontalOverflow = HorizontalWrapMode.Overflow,
                        generationExtents = new Vector2(10000f, 10000f),
                        pivot = Vector2.zero,
                        alignByGeometry = false,
                        textAnchor = TextAnchor.UpperLeft
                    };
                    float w = gen.GetPreferredWidth(s, settings) / settings.scaleFactor;
                    return w + 14f;
                }
                for (int i=0;i<cols.Count;i++)
                {
                    var c=cols[i]; float w = MeasureTitle(string.IsNullOrEmpty(c.Title)? c.Id : c.Title);
                    if (c.WidthHint.HasValue) w = Mathf.Max(w, c.WidthHint.Value);
                    _baseColWidths[i] = Mathf.Clamp(w, 50f, 300f);
                }
                int sample = Mathf.Min(_data?.Count ?? 0, 20);
                for (int si=0; si<sample; si++)
                {
                    var r = _data.GetRow(si);
                    for (int i=0;i<cols.Count;i++)
                    {
                        var c = cols[i]; var v = r.Get(c.Id); string s = v==null? string.Empty : (c.Formatter!=null? c.Formatter(v) : v.ToString());
                        // approximate measure by chars to avoid allocations
                        float charW = 7.5f; float w = Mathf.Min(300f, 14f + (s?.Length ?? 0) * charW);
                        _baseColWidths[i] = Mathf.Max(_baseColWidths[i], w);
                    }
                }
                _measuredBase = true;
            }
            // scale or distribute based on available width
            _colWidths = (float[])_baseColWidths.Clone();
            _totalWidth = 0f; for (int i=0;i<_colWidths.Length;i++) _totalWidth += _colWidths[i];
            if (_totalWidth > avail)
            {
                float scale = avail / Mathf.Max(1f,_totalWidth);
                for (int i=0;i<_colWidths.Length;i++) _colWidths[i] = Mathf.Max(40f, _colWidths[i]*scale);
                _totalWidth = 0f; for (int i=0;i<_colWidths.Length;i++) _totalWidth += _colWidths[i];
            }
            else
            {
                float extra = avail - _totalWidth; if (extra > 1f)
                {
                    float weightSum=0f; for (int i=0;i<_colWidths.Length;i++) weightSum+=_colWidths[i]; if (weightSum <= 0f) weightSum = 1f;
                    for (int i=0;i<_colWidths.Length;i++) _colWidths[i] += extra*(_colWidths[i]/weightSum);
                    _totalWidth = avail; // fill to available
                }
            }
            _computedWidths = true;
            if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log($"[SchemaTable] ComputeColumnWidths cols={cols.Count} sum={_totalWidth} available={avail}");
        }
        private void BuildHeaderOrUpdate()
        {
            if (_header==null)
            {
                _header = new GameObject("Header").AddComponent<RectTransform>(); _header.SetParent(_content,false);
                _header.anchorMin=new Vector2(0f,1f); _header.anchorMax=new Vector2(0f,1f); _header.pivot=new Vector2(0f,1f); _header.sizeDelta=new Vector2(_totalWidth, HeaderHeight); _header.anchoredPosition=Vector2.zero;
                var img = _header.gameObject.AddComponent<Image>(); img.color = new Color(0.2f,0.2f,0.2f,0.9f);
            }
            else
            {
                _header.sizeDelta = new Vector2(_totalWidth, HeaderHeight);
                // clear existing label children if column count changed
                if (_header.childCount != (_schema?.Columns?.Count ?? 0))
                {
                    for (int i=_header.childCount-1;i>=0;i--) DestroyImmediate(_header.GetChild(i).gameObject);
                }
            }
            float x=0f; var cols=_schema.Columns;
            for (int i=0;i<cols.Count;i++)
            {
                RectTransform cell = null; Text t = null;
                if (i < _header.childCount)
                {
                    cell = _header.GetChild(i) as RectTransform; t = cell.GetComponent<Text>();
                }
                if (cell == null)
                {
                    cell = new GameObject("HCell"+i).AddComponent<RectTransform>(); cell.SetParent(_header,false);
                    t = cell.gameObject.AddComponent<Text>(); t.font=IMK.SettingsUI.Theme.ThemeColors.DefaultFont; t.color=Color.white; t.alignment=TextAnchor.MiddleCenter;
                }
                cell.anchorMin=new Vector2(0f,0f); cell.anchorMax=new Vector2(0f,1f); cell.pivot=new Vector2(0f,0.5f); cell.anchoredPosition=new Vector2(x,0f); float w=_colWidths[i]; cell.sizeDelta=new Vector2(w,0f);
                t.text = string.IsNullOrEmpty(cols[i].Title)? cols[i].Id : cols[i].Title;
                x += w;
            }
        }
        private RowView CreateRowView()
        {
            var rv = new RowView();
            rv.Root = new GameObject("Row").AddComponent<RectTransform>(); rv.Root.SetParent(_content,false); rv.Root.anchorMin=new Vector2(0f,1f); rv.Root.anchorMax=new Vector2(1f,1f); rv.Root.pivot=new Vector2(0f,1f); rv.Root.sizeDelta=new Vector2(_totalWidth, RowHeight); // width set to total
            rv.Bg = rv.Root.gameObject.AddComponent<Image>(); rv.Bg.color = new Color(0.1f,0.1f,0.1f,0.6f);
            // highlight overlay (above cells)
            rv.Highlight = new GameObject("Highlight").AddComponent<RectTransform>(); rv.Highlight.SetParent(rv.Root,false); rv.Highlight.anchorMin=new Vector2(0f,0f); rv.Highlight.anchorMax=new Vector2(1f,1f); rv.Highlight.pivot=new Vector2(0.5f,0.5f); rv.Highlight.sizeDelta=Vector2.zero; var hiImg = rv.Highlight.gameObject.AddComponent<Image>(); hiImg.color = new Color(0.25f,0.55f,0.95f,0.5f); rv.Highlight.gameObject.SetActive(false);
            // left-click select without consuming drag events
            var clickTrig = rv.Root.gameObject.AddComponent<EventTrigger>(); var entry = new EventTrigger.Entry{ eventID=EventTriggerType.PointerClick }; entry.callback.AddListener(_=> SelectIndex(rv.Index)); clickTrig.triggers.Add(entry);
            rv.Cells = new List<Cell>(); var cols=_schema.Columns; for (int i=0;i<cols.Count;i++){ rv.Cells.Add(CreateCell(rv.Root, cols[i], _colWidths[i])); }
            // move highlight to be last so it's above backgrounds but below interactive inputs: set sibling index after creating cells
            rv.Highlight.SetAsLastSibling();
            ApplyColumnGeometryToRow(rv);
            return rv;
        }
        private Cell CreateCell(RectTransform row, TableColumn col, float w)
        {
            var cell = new Cell(); cell.Root = new GameObject("Cell"+col.Id).AddComponent<RectTransform>(); cell.Root.SetParent(row,false); cell.Root.anchorMin=new Vector2(0f,0f); cell.Root.anchorMax=new Vector2(0f,1f); cell.Root.pivot=new Vector2(0f,0.5f); cell.Root.sizeDelta=new Vector2(w,0f);
            switch(col.Kind)
            {
                case TableCellKind.Readonly:
                    cell.Readonly = MakeText(cell.Root); break;
                case TableCellKind.Dropdown:
                    // simplified: display as readonly text only with consistent background
                    var ddBg = cell.Root.gameObject.AddComponent<Image>(); ddBg.color = new Color(0.3f,0.3f,0.3f,0.65f);
                    cell.Readonly = MakeText(cell.Root); break;
                case TableCellKind.Toggle:
                    // consistent background identical to other editable cells
                    var toggleBgImage = cell.Root.gameObject.AddComponent<Image>();
                    toggleBgImage.color = new Color(0.3f,0.3f,0.3f,0.65f); // same as text/number cells
                    var toggle = cell.Root.gameObject.AddComponent<Toggle>(); cell.Toggle = toggle; toggle.isOn=false; toggle.interactable = !col.ReadOnly;
                    var boxGO = new GameObject("Box"); boxGO.transform.SetParent(cell.Root,false);
                    var boxRT = boxGO.AddComponent<RectTransform>(); boxRT.anchorMin=new Vector2(0.5f,0.5f); boxRT.anchorMax=new Vector2(0.5f,0.5f); boxRT.pivot=new Vector2(0.5f,0.5f); boxRT.anchoredPosition=Vector2.zero; boxRT.sizeDelta=new Vector2(18,18);
                    var border = boxGO.AddComponent<Image>(); border.color = Color.white;
                    var fillGO = new GameObject("Fill"); fillGO.transform.SetParent(boxGO.transform,false);
                    var fillRT = fillGO.AddComponent<RectTransform>(); fillRT.anchorMin=Vector2.zero; fillRT.anchorMax=Vector2.one; fillRT.offsetMin=new Vector2(2,2); fillRT.offsetMax=new Vector2(-2,-2);
                    var fillImg = fillGO.AddComponent<Image>(); fillImg.color = new Color(0.16f,0.16f,0.16f,1f);
                    var markGO = new GameObject("Mark"); markGO.transform.SetParent(boxGO.transform,false);
                    var markRT = markGO.AddComponent<RectTransform>(); markRT.anchorMin=new Vector2(0.5f,0.5f); markRT.anchorMax=new Vector2(0.5f,0.5f); markRT.pivot=new Vector2(0.5f,0.5f); markRT.anchoredPosition=Vector2.zero; markRT.sizeDelta=new Vector2(12,12);
                    var markImg = markGO.AddComponent<Image>(); markImg.color = Color.white;
                    toggle.targetGraphic = toggleBgImage; // background for state transitions (if any)
                    toggle.graphic = markImg;
                    break;
                default:
                    var img = cell.Root.gameObject.AddComponent<Image>(); img.color = new Color(0.3f,0.3f,0.3f,0.65f); cell.Input = cell.Root.gameObject.AddComponent<InputField>(); var txt = MakeText(cell.Root); cell.Input.textComponent=txt; if (col.ValueType != null && (col.ValueType == typeof(int) || col.ValueType == typeof(float) || col.ValueType == typeof(double) || col.ValueType == typeof(long) || col.ValueType == typeof(short) || col.ValueType == typeof(byte))) cell.Input.contentType = InputField.ContentType.DecimalNumber; else cell.Input.contentType = InputField.ContentType.Standard; break;
            }
            return cell;
        }
        private Text MakeText(RectTransform parent)
        {
            var txt = new GameObject("Text").AddComponent<Text>(); txt.transform.SetParent(parent,false); txt.font=IMK.SettingsUI.Theme.ThemeColors.DefaultFont; if (txt.font==null){ var any = GameObject.FindObjectOfType<Text>(); if (any!=null) txt.font = any.font; }
            txt.color=Color.white; txt.alignment=TextAnchor.MiddleCenter; var tr=txt.GetComponent<RectTransform>(); tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=new Vector2(4,0); tr.offsetMax=new Vector2(-4,0); return txt;
        }
        private void ApplyColumnGeometryToRow(RowView rv)
        {
            if (rv == null || rv.Cells == null || _colWidths == null) return;
            float x=0f; for (int i=0;i<rv.Cells.Count && i<_colWidths.Length;i++)
            {
                var cell = rv.Cells[i]; if (cell?.Root==null) continue; cell.Root.anchoredPosition = new Vector2(x,0f); cell.Root.sizeDelta = new Vector2(_colWidths[i],0f); x += _colWidths[i];
            }
            // ensure root width matches total for highlight sizing
            rv.Root.sizeDelta = new Vector2(_totalWidth, RowHeight);
        }
        private void UpdateVisibleRows()
        {
            if (_data == null || _schema == null || !_computedWidths) return;
            if (_data.Count <= 0) return;
            float scrollY = _content.anchoredPosition.y; int first = Mathf.Max(0, Mathf.FloorToInt((scrollY - HeaderHeight)/RowHeight)); if (scrollY <= HeaderHeight) first = 0; int last = Mathf.Min(_data.Count -1, first + _rows.Count -1);
            if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log($"[SchemaTable] UpdateVisible first={first} last={last} rows={_rows.Count} total={_data.Count}");
            for (int i=0;i<_rows.Count;i++)
            {
                int idx = first + i; var rv = _rows[i]; if (idx > last){ rv.Root.gameObject.SetActive(false); continue; }
                rv.Root.gameObject.SetActive(true); rv.Index = idx; rv.Adapter = _data.GetRow(idx); rv.Root.anchoredPosition = new Vector2(0f, -(HeaderHeight + idx*RowHeight));
                // refresh click trigger
                var et = rv.Root.GetComponent<EventTrigger>(); if (et!=null){ et.triggers.RemoveAll(e=> e.eventID==EventTriggerType.PointerClick); var ne = new EventTrigger.Entry{ eventID=EventTriggerType.PointerClick }; ne.callback.AddListener(_=> SelectIndex(idx)); et.triggers.Add(ne); }
                bool selected = (idx == _selectedIndex);
                rv.Highlight.gameObject.SetActive(selected);
                rv.Bg.color = selected ? new Color(0.12f,0.18f,0.28f,0.75f) : new Color(0.1f,0.1f,0.1f,0.6f);
                for (int c=0;c<_schema.Columns.Count;c++)
                {
                    var col = _schema.Columns[c]; var cell = rv.Cells[c]; var val = rv.Adapter.Get(col.Id);
                    switch (col.Kind)
                    {
                        case TableCellKind.Readonly:
                            cell.Readonly.text = col.Formatter!=null? col.Formatter(val) : (val?.ToString() ?? string.Empty);
                            break;
                        case TableCellKind.Dropdown:
                            if (cell.Readonly!=null) cell.Readonly.text = val?.ToString() ?? string.Empty;
                            break;
                        case TableCellKind.Toggle:
                            bool b=false; try{ b = System.Convert.ToBoolean(val);}catch{} if (cell.Toggle.isOn != b) cell.Toggle.isOn=b; cell.Toggle.onValueChanged.RemoveAllListeners(); if (!col.ReadOnly) cell.Toggle.onValueChanged.AddListener(v=> { SelectIndex(idx); rv.Adapter.Set(col.Id, v); });
                            // ensure pointer click also selects (in case value unchanged)
                            var tTrig = cell.Root.GetComponent<UnityEngine.EventSystems.EventTrigger>(); if (tTrig==null) tTrig = cell.Root.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                            tTrig.triggers.RemoveAll(e=> e.eventID == UnityEngine.EventSystems.EventTriggerType.PointerClick);
                            var te = new UnityEngine.EventSystems.EventTrigger.Entry{ eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick }; te.callback.AddListener(_=> SelectIndex(idx)); tTrig.triggers.Add(te);
                            break;
                        default:
                            if (cell.Input != null)
                            {
                                string txtCur = col.Formatter!=null? col.Formatter(val) : (val==null? string.Empty : val.ToString());
                                if (!cell.Input.isFocused && cell.Input.text != txtCur) cell.Input.text = txtCur;
                                cell.Input.onEndEdit.RemoveAllListeners();
                                cell.Input.onEndEdit.AddListener(t=>{ object parsed = Parse(t, col.ValueType); if (col.Validator==null || col.Validator(parsed)) { SelectIndex(idx); rv.Adapter.Set(col.Id, parsed); } });
                                // also select on focus/click
                                var iTrig = cell.Root.GetComponent<UnityEngine.EventSystems.EventTrigger>(); if (iTrig==null) iTrig = cell.Root.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                                iTrig.triggers.RemoveAll(e=> e.eventID == UnityEngine.EventSystems.EventTriggerType.PointerClick || e.eventID == UnityEngine.EventSystems.EventTriggerType.Select);
                                var ie = new UnityEngine.EventSystems.EventTrigger.Entry{ eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick }; ie.callback.AddListener(_=> SelectIndex(idx)); iTrig.triggers.Add(ie);
                                var selEntry = new UnityEngine.EventSystems.EventTrigger.Entry{ eventID = UnityEngine.EventSystems.EventTriggerType.Select }; selEntry.callback.AddListener(_=> SelectIndex(idx)); iTrig.triggers.Add(selEntry);
                            }
                            break;
                    }
                }
            }
        }
        private void SelectIndex(int idx)
        {
            _selectedIndex = Mathf.Clamp(idx, -1, (_data?.Count ?? 0)-1);
            // refresh highlight only
            for (int i=0;i<_rows.Count;i++) if (_rows[i].Root.gameObject.activeSelf) _rows[i].Bg.color = ((_rows[i].Index == _selectedIndex)? new Color(0.18f,0.18f,0.28f,0.85f): new Color(0.1f,0.1f,0.1f,0.6f));
        }
        private object Parse(string t, Type target)
        {
            if (target == typeof(string) || target == null) return t;
            if (target == typeof(int) || target == typeof(int?)) { int v; if (int.TryParse(t, out v)) return v; return 0; }
            if (target == typeof(float) || target == typeof(float?)) { float v; if (float.TryParse(t, out v)) return v; return 0f; }
            if (target == typeof(double) || target == typeof(double?)) { double v; if (double.TryParse(t, out v)) return v; return 0.0; }
            if (target == typeof(bool) || target == typeof(bool?)) { bool v; if (bool.TryParse(t, out v)) return v; return false; }
            return t;
        }
        private float totalContentHeight(){ return HeaderHeight + (_data?.Count ?? 0) * RowHeight; }
        public void AddNew(){ _data?.AddNew(); _dirtyFull = true; if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log("[SchemaTable] AddNew done"); _measuredBase=false; }
        public void Save(){ if (_data?.Commit() == true) { if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log("[SchemaTable] Save Commit ok"); } else { if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log("[SchemaTable] Save Commit failed"); } _measuredBase=false; }
        public void Reload(){ if (_data?.Reload() == true) { _dirtyFull = true; if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log("[SchemaTable] Reload ok count="+_data.Count); } else { if (Diagnostics.DebugFlags.TableDiagEnabled) Debug.Log("[SchemaTable] Reload failed"); } _measuredBase=false; }
        public bool RemoveSelected(){ if (_selectedIndex>=0 && _selectedIndex<(_data?.Count ?? 0)){ var ok = _data.RemoveAt(_selectedIndex); if (ok){ _selectedIndex = Mathf.Min(_selectedIndex, (_data.Count-1)); _dirtyFull = true; } return ok; } return false; }
        public bool MoveSelectedUp(){ if (_selectedIndex>0){ var ok = _data.Move(_selectedIndex, _selectedIndex-1); if (ok){ _selectedIndex--; _dirtyFull=true; } return ok; } return false; }
        public bool MoveSelectedDown(){ int cnt = _data?.Count ?? 0; if (_selectedIndex>=0 && _selectedIndex<cnt-1){ var ok = _data.Move(_selectedIndex, _selectedIndex+1); if (ok){ _selectedIndex++; _dirtyFull=true; } return ok; } return false; }
        void LateUpdate()
        {
            if (_viewport == null || !_computedWidths || _content == null) return;
            // Mouse wheel fallback scrolling (in case ScrollRect drag is intercepted)
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(_viewport, Input.mousePosition))
                {
                    float delta = wheel * 40f; // tune scroll speed
                    float maxY = Mathf.Max(0f, totalContentHeight() - _viewport.rect.height);
                    var pos = _content.anchoredPosition; pos.y = Mathf.Clamp(pos.y - delta, 0f, maxY); _content.anchoredPosition = pos;
                    _dirtyFull = false; // do not rebuild, just refresh visible rows
                    UpdateVisibleRows();
                }
            }
        }
    }
}
