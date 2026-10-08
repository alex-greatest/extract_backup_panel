namespace FlexRt.Model.Plc.Card;

/// <summary>Что лежит в распакованном потоке карты ПЛК; определяется по словарю, которым поток сжат.</summary>
internal enum CardStreamKind
{
    /// <summary>Таблица тегов ПЛК: XML <c>&lt;IdentContainer&gt;</c>.</summary>
    TagInterface,
    /// <summary>Комментарии тегов ПЛК: XML <c>&lt;CommentDictionary&gt;</c>, ключ — <c>LID</c> тега.</summary>
    TagComments,
    /// <summary>Интерфейс блока (DB, FB, FC, UDT): XML <c>&lt;BlockInterface&gt;</c>; тем же словарём сжаты отладочные данные <c>&lt;DebugInfo&gt;</c>.</summary>
    BlockInterface,
    /// <summary>Комментарии членов блока: XML <c>&lt;InterfaceLineComments&gt;</c>, ключ — цепочка ID.</summary>
    BlockComments
}
