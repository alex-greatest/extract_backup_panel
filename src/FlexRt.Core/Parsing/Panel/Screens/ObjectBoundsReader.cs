using FlexRt.Binary;
using FlexRt.Model.Panel;
using FlexRt.Model.Panel.Screens;

namespace FlexRt.Parsing.Panel.Screens;

/// <summary>
/// Положение объекта экрана в его элементе VIEW_*. Начало элемента — как у <see cref="NamedElement"/>: u16 вид, имя,
/// u32 R, R ссылок; дальше — по виду элемента (сверено с TIA: 413 объектов A603A0097 через Openness, f(x) trend view
/// A603A0097 и Project1 по Layout).
/// </summary>
internal static class ObjectBoundsReader
{
    /// <summary>Вид элемента со сжатыми настройками контрола (тренды, окна алармов, рецептов и т. п.).</summary>
    public const int ControlKind = 0x0b;

    /// <summary>Таблица, у которой между ссылками и стилем лежат ещё 4 байта (наблюдение: всегда <c>00 00 ff ff</c>).</summary>
    private const string OcxTable = "VIEW_OCX";

    /// <summary>Размер поля перед стилем у элементов <see cref="OcxTable"/>.</summary>
    private const int OcxPrefixSize = 4;

    /// <summary>
    /// Размер u32 стиля за ссылками (наблюдение: <c>0x50000000</c> или <c>0x50010000</c>, у объектов окна алармов Global
    /// screen <c>0xD0000000</c>; гипотеза — стиль окна Win32); за ним — рамка.
    /// </summary>
    private const int StyleSize = 4;

    /// <summary>Размер рамки: i16 Left, Top, Right, Bottom.</summary>
    private const int RectSize = 8;

    /// <summary>
    /// Смещение старшего байта стиля в сжатом блоке элемента вида <see cref="ControlKind"/> от конца ссылок: перед ним
    /// u32 размер блока, u32, u32 и начало сжатых данных; наблюдение на всех 131 таких элементах образцов.
    /// </summary>
    private const int ControlStyleOffset = 0x18;

    /// <summary>Старший байт стиля в сжатом блоке (наблюдение: всегда 0x50).</summary>
    private const int ControlStyleHigh = 0x50;

    /// <summary>
    /// Смещение байта флагов сжатия перед рамкой: 0 — восемь следующих байт лежат как есть (рамка читается); иначе среди
    /// них есть ссылка назад и рамки без распаковки не прочитать (гипотеза: флаг на 8 элементов, старший бит первым).
    /// </summary>
    private const int ControlFlagsOffset = 0x19;

    /// <summary>Смещение рамки в сжатом блоке от конца ссылок: сразу за байтом флагов.</summary>
    private const int ControlRectOffset = 0x1a;

    /// <summary>Первое слово элемента с обычной раскладкой: 3 — объект с именем, 1 — без имени.</summary>
    private static readonly int[] PlainKinds = [3, 1];

    /// <summary>
    /// Прочитать положение объекта. Вид 3 и 1: за ссылками (у VIEW_OCX — ещё 4 байта) u32 стиль и рамка i16 Left,
    /// Top, Right, Bottom. Вид <see cref="ControlKind"/>: рамка в начале сжатого блока, если её байты лежат без сжатия
    /// (так у f(x) trend view; у Trend view — нет, его рамка — соседний объект без имени в VIEW_MOVE).
    /// </summary>
    /// <param name="b">Байты файла.</param>
    /// <param name="table">Имя таблицы элемента.</param>
    /// <param name="item">Элемент.</param>
    /// <param name="kind">Первое слово элемента (<see cref="NamedElement.Kind"/>).</param>
    /// <returns>Положение и размер; вид другой, начало не сходится, рамка не помещается или сжата — <c>null</c>.</returns>
    public static ScreenRect? TryRead(FwxBinary b, string table, TableItem item, int? kind)
    {
        if (kind is null || NamedElement.TryBodyOffset(b, item) is not { } body)
        {
            return null;
        }
        if (!PlainKinds.Contains(kind.Value))
        {
            return kind == ControlKind ? ControlRect(b, item, body) : null;
        }
        var prefix = table == OcxTable ? OcxPrefixSize : 0;
        return Rect(b, item, body + prefix + StyleSize);
    }

    /// <summary>Рамка в сжатом блоке элемента вида <see cref="ControlKind"/>, если стиль на месте и флаги сжатия — 0.</summary>
    /// <returns>Положение и размер; блок короче или байты рамки сжаты — <c>null</c>.</returns>
    private static ScreenRect? ControlRect(FwxBinary b, TableItem item, long body)
    {
        if (body + ControlRectOffset + RectSize > item.Length)
        {
            return null;
        }
        var start = item.Offset + body;
        return b.D1(start + ControlStyleOffset) == ControlStyleHigh && b.D1(start + ControlFlagsOffset) == 0
            ? Rect(b, item, body + ControlRectOffset)
            : null;
    }

    /// <summary>Рамка i16 Left, Top, Right, Bottom на <paramref name="offset"/> от начала элемента.</summary>
    /// <returns>Положение и размер (ширина = Right − Left, высота = Bottom − Top); не помещается — <c>null</c>.</returns>
    private static ScreenRect? Rect(FwxBinary b, TableItem item, long offset)
    {
        if (offset + RectSize > item.Length)
        {
            return null;
        }
        var p = item.Offset + offset;
        int left = (short)b.D2(p);
        int top = (short)b.D2(p + 2);
        int right = (short)b.D2(p + 4);
        int bottom = (short)b.D2(p + 6);
        return new ScreenRect(left, top, right - left, bottom - top);
    }
}
