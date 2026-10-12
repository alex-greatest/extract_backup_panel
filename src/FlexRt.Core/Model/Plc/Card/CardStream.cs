namespace FlexRt.Model.Plc.Card;

/// <summary>Распакованный поток карты ПЛК (<c>SIMATIC.S7S\OMSSTORE</c>).</summary>
/// <param name="File">Файл внутри <c>OMSSTORE</c>, относительный путь.</param>
/// <param name="Offset">Смещение сжатого потока от начала файла.</param>
/// <param name="Rid">RID объекта, в котором лежит поток (<c>0x8a0e0001</c> — DB1); 0, если перед потоком в файле нет начала объекта.</param>
/// <param name="Kind">Что в потоке.</param>
/// <param name="Text">Распакованный текст (XML).</param>
internal sealed record CardStream(string File, long Offset, long Rid, CardStreamKind Kind, string Text);
