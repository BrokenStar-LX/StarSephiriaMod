using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SPMod;

// Presentation only: effectType, IDs, values, timers and RPC payloads stay native.
[HarmonyPatch(typeof(UI_PlayerEffectHUD))]
internal static class EffectHudLayout
{
    private static readonly List<EffectHudFlow> Flows = new();
    private static readonly MethodInfo Refresh = AccessTools.Method(typeof(UI_PlayerEffectHUD),
        "RefreshBoonAndConditionZone");

    [HarmonyPatch("OnEffectHUDCreated")]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Created(UI_PlayerEffectHUD __instance, string __0, string __1,
        Dictionary<string, UI_EffectHUD_Basic> ___instantiatedHUDs,
        Dictionary<string, UI_EffectHUD_Basic> ___instantiatedBoons,
        Dictionary<string, UI_EffectHUD_Basic> ___instantiatedConditions,
        RectTransform ___boonZone, RectTransform ___conditionZone)
    {
        var entity = EffectHUDDatabase.FindHUDById(__0);
        if (entity == null || !EffectRowPlan.IsSpm(entity.name) ||
            !___instantiatedConditions.ContainsKey(__1) ||
            !___instantiatedHUDs.TryGetValue(__1, out var icon) || icon == null) return;
        EffectHudFlow? flow = null;
        // Unity destroys components at frame end. A same-frame reconnect must not
        // reuse the inactive component that has already been scheduled for removal.
        foreach (var candidate in __instance.GetComponents<EffectHudFlow>())
            if (candidate.Active) { flow = candidate; break; }
        if (flow == null)
        {
            flow = __instance.gameObject.AddComponent<EffectHudFlow>();
            flow.Initialize(__instance, ___boonZone, ___conditionZone,
                ___instantiatedHUDs, ___instantiatedBoons, ___instantiatedConditions);
            Flows.Add(flow);
        }
        flow.Route(__1, icon);
        Refresh.Invoke(__instance, null);
    }

    [HarmonyPatch("Disconnect")]
    [HarmonyPrefix]
    private static void Disconnect(UI_PlayerEffectHUD __instance)
    {
        EffectHudFlow? flow = null;
        // Unity destroys components at frame end. A same-frame reconnect must not
        // reuse the inactive component that has already been scheduled for removal.
        foreach (var candidate in __instance.GetComponents<EffectHudFlow>())
            if (candidate.Active) { flow = candidate; break; }
        if (flow == null) return;
        flow.Restore();
        Object.Destroy(flow);
        Flows.Remove(flow);
    }

    internal static void RestoreAll()
    {
        foreach (var flow in Flows.ToArray())
        {
            if (flow == null) continue;
            flow.Restore();
            Object.Destroy(flow);
        }
        Flows.Clear();
    }

    internal static void Forget(EffectHudFlow flow) => Flows.Remove(flow);
}

internal sealed class EffectHudFlow : MonoBehaviour
{
    private UI_PlayerEffectHUD _hud = null!;
    private RectTransform _top = null!, _conditions = null!;
    private Dictionary<string, UI_EffectHUD_Basic> _boons = null!, _debuffs = null!;
    private Dictionary<string, UI_EffectHUD_Basic> _all = null!;
    private static readonly HashSet<LayoutElement> RetiringElements = new();
    private readonly Dictionary<string, UI_EffectHUD_Basic> _moved = new(StringComparer.Ordinal);
    private readonly Dictionary<RectTransform, SavedRect> _icons = new();
    private readonly List<PartyOffset> _parties = new();
    private readonly Vector3[] _corners = new Vector3[4];
    private readonly List<RectTransform> _children = new();
    private readonly List<EffectRowPlan.Box> _sizes = new();
    private LayoutGroup? _layout;
    private ContentSizeFitter? _fitter;
    private LayoutElement _element = null!;
    private bool _layoutEnabled, _fitterEnabled, _ownsElement, _active;
    private float _oldMinHeight, _oldPreferredHeight, _oldMinWidth, _oldPreferredWidth, _gapX, _gapY, _baseHeight;
    private SavedRect _topOriginal = null!;
    private PartyOffset _conditionOffset = null!;
    private float _nextPartyRefresh;
    private int _lastFrame = -1;
    private bool _layoutDirty = true;
    private int _arrangeCount;
    private readonly List<ChildState> _lastChildren = new();
    private float _lastWidth = float.NaN;
    private float _lastParentWidth = float.NaN;
    private EffectRowPlan.Box[] _positions = Array.Empty<EffectRowPlan.Box>();
    private float _packedHeight;
    private int _paddingLeft, _paddingRight, _paddingTop, _paddingBottom;

    internal bool Active => _active;
    internal int ArrangeCount => _arrangeCount;

    internal void Initialize(UI_PlayerEffectHUD hud, RectTransform top, RectTransform conditions,
        Dictionary<string, UI_EffectHUD_Basic> all,
        Dictionary<string, UI_EffectHUD_Basic> boons, Dictionary<string, UI_EffectHUD_Basic> debuffs)
    {
        _hud = hud; _top = top; _conditions = conditions; _all = all; _boons = boons; _debuffs = debuffs;
        _topOriginal = new SavedRect(top);
        _baseHeight = top.rect.height;
        // Grow downward without moving the first row into HP/MP on overflow.
        top.GetWorldCorners(_corners);
        var topEdge = _corners[1];
        top.pivot = new Vector2(top.pivot.x, 1);
        top.GetWorldCorners(_corners);
        top.position += topEdge - _corners[1];
        _layout = top.GetComponent<LayoutGroup>();
        _layoutEnabled = _layout != null && _layout.enabled;
        _gapX = _layout is GridLayoutGroup grid ? grid.spacing.x :
            _layout is HorizontalOrVerticalLayoutGroup line ? line.spacing : 0;
        _gapY = _layout is GridLayoutGroup grid2 ? grid2.spacing.y : _gapX;
        if (_layout != null)
        {
            _paddingLeft = _layout.padding.left; _paddingRight = _layout.padding.right;
            _paddingTop = _layout.padding.top; _paddingBottom = _layout.padding.bottom;
            _layout.enabled = false;
        }
        _fitter = top.GetComponent<ContentSizeFitter>();
        _fitterEnabled = _fitter != null && _fitter.enabled;
        if (_fitter != null) _fitter.enabled = false;
        RetiringElements.RemoveWhere(element => element == null);
        _element = null!;
        foreach (var element in top.GetComponents<LayoutElement>())
            if (!RetiringElements.Contains(element)) { _element = element; break; }
        _ownsElement = _element == null;
        if (_element == null) _element = top.gameObject.AddComponent<LayoutElement>();
        _oldMinHeight = _element.minHeight; _oldPreferredHeight = _element.preferredHeight;
        _oldMinWidth = _element.minWidth; _oldPreferredWidth = _element.preferredWidth;
        _conditionOffset = new PartyOffset(conditions);
        _active = true;
        _layoutDirty = true;
        Canvas.willRenderCanvases += Render;
    }

    internal void Route(string id, UI_EffectHUD_Basic icon)
    {
        if (!_active || (_moved.TryGetValue(id, out var previous) && previous == icon)) return;
        // Native Boon instances already belong here; only transfer Condition membership.
        if (!_debuffs.Remove(id)) return;
        _moved[id] = icon;
        _boons[id] = icon;
        if (icon.transform is RectTransform rect && !_icons.ContainsKey(rect))
            _icons.Add(rect, new SavedRect(rect));
        icon.transform.SetParent(_top, false);
        _layoutDirty = true;
    }

    private void Render()
    {
        if (!_active || (_lastFrame == Time.frameCount && !NeedsLayout())) return;
        _lastFrame = Time.frameCount;
        try { Arrange(); }
        catch (Exception error)
        {
            Debug.LogError("[SPMod] 效果图标布局停止，恢复原生布局: " + error);
            Restore(); // A layout fault must not become a per-frame exception storm.
        }
    }

    private bool NeedsLayout()
    {
        if (_top == null) return false;
        if (Time.unscaledTime >= _nextPartyRefresh) _layoutDirty = true;
        float parentWidth = _top.parent is RectTransform parent &&
            parent.GetComponent<VerticalLayoutGroup>() is { enabled: true, childControlWidth: true } parentLayout
            ? parent.rect.width - parentLayout.padding.left - parentLayout.padding.right
            : float.NaN;
        if (_layoutDirty || !parentWidth.Equals(_lastParentWidth)) return true;
        float width = _top.rect.width - _paddingLeft - _paddingRight;
        if (width != _lastWidth || _top.childCount != _lastChildren.Count) return true;
        for (int i = 0; i < _top.childCount; i++)
        {
            var child = (RectTransform)_top.GetChild(i);
            var state = _lastChildren[i];
            if (!ReferenceEquals(child, state.Child) || child.gameObject.activeSelf != state.Active ||
                child.rect.width != state.Width || child.rect.height != state.Height) return true;
        }
        return false;
    }

    private void CaptureLayoutInputs()
    {
        _lastParentWidth = _top.parent is RectTransform parent &&
            parent.GetComponent<VerticalLayoutGroup>() is { enabled: true, childControlWidth: true } parentLayout
            ? parent.rect.width - parentLayout.padding.left - parentLayout.padding.right
            : float.NaN;
        _lastChildren.Clear();
        for (int i = 0; i < _top.childCount; i++)
        {
            var child = (RectTransform)_top.GetChild(i);
            _lastChildren.Add(new ChildState(child, child.gameObject.activeSelf, child.rect.width, child.rect.height));
        }
        _layoutDirty = false;
    }

    private void Arrange()
    {
        _arrangeCount++;
        if (_top == null) return;
        // The native VerticalLayoutGroup controls this row's width. Disabling
        // its HorizontalLayoutGroup also removes its preferred-width provider;
        // without a replacement the parent collapses it to zero and this method
        // returns before positioning any icon. Supply the bounded row width in
        // both the layout contract and the rect, including before the first pass.
        if (_top.parent is RectTransform rowParent &&
            rowParent.GetComponent<VerticalLayoutGroup>() is { enabled: true, childControlWidth: true } parentLayout)
        {
            float available = rowParent.rect.width - parentLayout.padding.left - parentLayout.padding.right;
            if (!(available > 0)) return;
            _element.minWidth = 0;
            _element.preferredWidth = available;
            _top.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, available);
        }
        float width = _top.rect.width - _paddingLeft - _paddingRight;
        if (!(width > 0)) return; // Hidden/unlaid-out canvases are retried.
        _children.Clear(); _sizes.Clear();
        for (int i = 0; _top.gameObject.activeInHierarchy && i < _top.childCount; i++)
        {
            if (!(_top.GetChild(i) is RectTransform child) || !child.gameObject.activeSelf) continue;
            var ignore = child.GetComponent<LayoutElement>();
            if (ignore != null && ignore.ignoreLayout) continue;
            if (!_icons.TryGetValue(child, out var saved))
                _icons.Add(child, saved = new SavedRect(child));
            _children.Add(child);
            _sizes.Add(new EffectRowPlan.Box(0, 0, saved.Width, saved.Height));
        }
        bool repack = _lastWidth != width || _positions.Length != _sizes.Count;
        for (int i = 0; !repack && i < _sizes.Count; i++)
            repack = _positions[i].Width != _sizes[i].Width || _positions[i].Height != _sizes[i].Height;
        if (repack)
        {
            _positions = EffectRowPlan.Pack(_sizes, width, _gapX, _gapY, out _packedHeight);
            _lastWidth = width;
        }
        var positions = _positions;
        float height = _packedHeight;
        height = Mathf.Max(_baseHeight, height + _paddingTop + _paddingBottom);
        _element.minHeight = height; _element.preferredHeight = height;
        _top.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        for (int i = 0; i < _children.Count; i++)
        {
            var child = _children[i]; var at = positions[i];
            child.anchorMin = child.anchorMax = new Vector2(0, 1);
            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, at.Width);
            child.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, at.Height);
            child.anchoredPosition = new Vector2(_paddingLeft + at.X + at.Width * child.pivot.x,
                -_paddingTop - at.Y - at.Height * (1 - child.pivot.y));
        }
        // Native conditions must also clear the top row if it overflows.
        if (_conditions.parent is RectTransform parent && _conditions.gameObject.activeInHierarchy)
            _conditionOffset.MoveBelow(BoundsOfIcons(parent), _gapY, _corners);
        if (Time.unscaledTime >= _nextPartyRefresh)
        {
            _nextPartyRefresh = Time.unscaledTime + 0.5f;
            var canvas = _top.GetComponentInParent<Canvas>();
            if (canvas != null)
                foreach (var party in canvas.rootCanvas.GetComponentsInChildren<UI_MultiplayerHUD>(true))
                {
                    var rect = (RectTransform?)AccessTools.Field(typeof(UI_MultiplayerHUD), "contentsZone").GetValue(party);
                    if (rect != null && !_parties.Exists(p => p.Target == rect)) _parties.Add(new PartyOffset(rect));
                }
        }
        foreach (var party in _parties)
        {
            if (party.Target == null || !party.Target.gameObject.activeInHierarchy ||
                !(party.Target.parent is RectTransform partyParent)) continue;
            var occupied = BoundsOfIcons(partyParent);
            if (_conditions.gameObject.activeInHierarchy)
                occupied = Union(occupied, RectIn(_conditions, partyParent, _corners));
            party.MoveBelow(occupied, Mathf.Max(2, _gapY), _corners);
        }
        CaptureLayoutInputs();
    }

    private Rect BoundsOfIcons(RectTransform parent)
    {
        var bounds = default(Rect);
        foreach (var child in _children)
        {
            bounds = Union(bounds, RectIn(child, parent, _corners));
            // Counter text may extend below the sprite's own rectangle.
            foreach (var graphic in child.GetComponentsInChildren<Graphic>())
                bounds = Union(bounds, RectIn(graphic.rectTransform, parent, _corners));
        }
        return bounds;
    }

    private static Rect Union(Rect a, Rect b) => a.width <= 0 || a.height <= 0 ? b :
        Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

    private static Rect RectIn(RectTransform rect, RectTransform space, Vector3[] corners)
    {
        rect.GetWorldCorners(corners);
        var first = space.InverseTransformPoint(corners[0]);
        var min = new Vector2(first.x, first.y); var max = min;
        for (int i = 1; i < 4; i++)
        {
            var p = space.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, new Vector2(p.x, p.y));
            max = Vector2.Max(max, new Vector2(p.x, p.y));
        }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    internal void Restore()
    {
        if (!_active) return;
        _active = false;
        Canvas.willRenderCanvases -= Render;
        foreach (var pair in _moved)
        {
            if (pair.Value == null ||
                !_all.TryGetValue(pair.Key, out var current) || !ReferenceEquals(current, pair.Value) ||
                !_boons.TryGetValue(pair.Key, out current) || !ReferenceEquals(current, pair.Value)) continue;
            pair.Value.transform.SetParent(_conditions, false);
            _boons.Remove(pair.Key); _debuffs[pair.Key] = pair.Value;
        }
        foreach (var icon in _icons)
            if (icon.Key != null) icon.Value.Restore(icon.Key);
        foreach (var party in _parties) party.Restore();
        _conditionOffset.Restore();
        if (_top != null)
        {
            _topOriginal.Restore(_top);
            if (_element != null)
            {
                if (_ownsElement)
                {
                    _element.enabled = false;
                    RetiringElements.Add(_element);
                    Object.Destroy(_element);
                }
                else
                {
                    _element.minHeight = _oldMinHeight; _element.preferredHeight = _oldPreferredHeight;
                    _element.minWidth = _oldMinWidth; _element.preferredWidth = _oldPreferredWidth;
                }
            }
            if (_layout != null) _layout.enabled = _layoutEnabled;
            if (_fitter != null) _fitter.enabled = _fitterEnabled;
        }
        if (_hud != null)
            AccessTools.Method(typeof(UI_PlayerEffectHUD), "RefreshBoonAndConditionZone").Invoke(_hud, null);
    }

    private void OnDestroy()
    {
        Restore();
        EffectHudLayout.Forget(this);
    }

    private sealed class SavedRect
    {
        private readonly Vector2 _anchorMin, _anchorMax, _position, _size, _pivot;
        internal readonly float Width, Height;
        internal SavedRect(RectTransform rect)
        {
            _anchorMin = rect.anchorMin; _anchorMax = rect.anchorMax;
            _position = rect.anchoredPosition; _size = rect.sizeDelta; _pivot = rect.pivot;
            Width = Mathf.Max(1, LayoutUtility.GetPreferredWidth(rect), rect.rect.width);
            Height = Mathf.Max(1, LayoutUtility.GetPreferredHeight(rect), rect.rect.height);
        }
        internal void Restore(RectTransform rect)
        {
            rect.pivot = _pivot; rect.anchorMin = _anchorMin; rect.anchorMax = _anchorMax;
            rect.anchoredPosition = _position; rect.sizeDelta = _size;
        }
    }

    private readonly struct ChildState
    {
        internal readonly RectTransform Child;
        internal readonly bool Active;
        internal readonly float Width, Height;
        internal ChildState(RectTransform child, bool active, float width, float height)
        { Child = child; Active = active; Width = width; Height = height; }
    }

    private sealed class PartyOffset
    {
        internal readonly RectTransform Target;
        private Vector2 _baseline, _lastWritten;
        internal PartyOffset(RectTransform target) { Target = target; _baseline = _lastWritten = target.anchoredPosition; }
        internal void MoveBelow(Rect obstacle, float gap, Vector3[] corners)
        {
            if (Target.anchoredPosition != _lastWritten) _baseline = Target.anchoredPosition;
            var parent = (RectTransform)Target.parent;
            var rect = RectIn(Target, parent, corners);
            rect.position += _baseline - Target.anchoredPosition;
            float delta = EffectRowPlan.DownwardClearance(
                new EffectRowPlan.Box(obstacle.x, obstacle.y, obstacle.width, obstacle.height),
                new EffectRowPlan.Box(rect.x, rect.y, rect.width, rect.height), gap);
            _lastWritten = _baseline - new Vector2(0, delta);
            Target.anchoredPosition = _lastWritten;
        }
        internal void Restore()
        {
            if (Target != null && Target.anchoredPosition == _lastWritten) Target.anchoredPosition = _baseline;
        }
    }
}
