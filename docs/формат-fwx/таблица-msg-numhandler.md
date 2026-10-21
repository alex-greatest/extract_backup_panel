# MSG_NUMHANDLER — классы алармов

Классы алармов панели (в TIA: HMI alarms → Alarm classes): ID класса, Display
name, тексты состояний, цвета, State machine. Программа читает таблицу в
`Parsing/Panel/Alarms/MsgNumHandlerParser.cs` (раскладка — `AlarmHandlerLayout.cs`, общая с
`MSG_ANALOGHANDL`, [таблица-msg-analoghandl.md](таблица-msg-analoghandl.md)); на номер
записи класса ссылается аларм в `MSG_NUMEVENT`
([таблица-msg-numevent.md](таблица-msg-numevent.md)).

Статусы — как в [таблица-datalink-readwr.md](таблица-datalink-readwr.md).

## Элемент

`Id` = 0x1b. Один элемент во всех файлах, в том числе в панелях без алармов
(Project1).

| Смещение | Значение | Статус |
|---|---|---|
| +0x00…+0x2b | `01 00 00 00 04 00 00 00 1a 00 00 00 12 00 12 80 …` — одинаково во всех файлах | наблюдение, не разобрано |
| +0x2c u16 | N — число дискретных алармов (0 в Project1) | установлено на файлах |
| +0x2e | два массива u16[N]; второй у A603A0097 — номер элемента `MSG_NUMEVENT` по возрастанию ID | наблюдение |
| +0x2e + 4·N u16 | число классов K | установлено на файлах |
| дальше | K записей по 0x24 байта до конца элемента | установлено на файлах |

## Запись класса

Сверено с TIA на шаговых копиях A603A0097 (`samples/a603-classes/`) и Project1
(V21): владелец менял одно свойство класса, компилировал, сохранял `pdata.fwc`.

| Смещение | Значение | Статус |
|---|---|---|
| +0x00 u32 | номер строки **Display name** в `STRINGSTORE`, один для всех языков | установлено: `DISP` введён только на английском |
| +0x04, +0x08, +0x0c u32 | строки текстов состояний **Incoming**, **Outgoing**, **Acknowledged** | установлено на шаговых копиях |
| +0x10, +0x14, +0x18, +0x1c u32 | 4 цвета фона: Background color "Incoming", "Incoming/Outgoing", "Incoming/Acknowledged", "Incoming/Outgoing/Acknowledged"; байты R, G, B, признак | установлено на шаговых копиях |
| +0x20 u8 | **ID класса**, как поле ID в свойствах класса в TIA | установлено, см. ниже |
| +0x21 2 байта | **State machine**: `0c 03` — Alarm with single-mode acknowledgment, `04 01` — Alarm without acknowledgment | установлено: в TIA только эти два режима |
| +0x23 | 0 | наблюдение |

Признак цвета (старший байт): 0 — цвет задан (`ff 00 00 00` — красный
`255, 0, 0`); `0x20` — в Runtime settings панели выключен флажок цветов классов
алармов, цвета в файле нет (установлено на Project1, V21: флажок переключён
туда и обратно; у CCB-A13 все цвета `00 00 00 20`). Другие значения признака не
встречались. Common alarm class и Flashing не искались.

ID сверены с TIA на A603A0097: Errors 1, Warnings 2, System 3, Diagnosis events 4,
Acknowledgement 33, No Acknowledgement 34, свои классы 64, 65. В списке панели
только классы дискретных алармов: Errors, Warnings, Acknowledgement, No
Acknowledgement и свои (System и Diagnosis events — нет; часть их данных,
возможно, в заголовке `SYSMSGHANDLER`, см.
[таблица-sysmsghandler.md](таблица-sysmsghandler.md)). В CCB-A13 свои
классы — 35…39 (`ALARM`, `FAULT`, `EVENT`, `ILOCK`, `INFO`): номера своих
классов не обязательно от 64. ID класса Safety warnings не известен.

**Имени класса (Name) в панели нет**, только Display name: у Errors он пустой
(в русском — `!`), у Warnings пустой. Непереведённый Display name в TIA равен
текущему Name, но по решению владельца Name из него не выводится.

Журнал алармов класса (Log) — в `RECMSG` и `ARCHIVES`
([таблица-recmsg.md](таблица-recmsg.md), [таблица-archives.md](таблица-archives.md)),
E-mail address — в `MSG_LOGGER` и `FUNC_SEND_EMAIL`
([таблица-msg-logger.md](таблица-msg-logger.md),
[таблица-func-send-email.md](таблица-func-send-email.md)); связь — по ID класса.

## Что делает программа

- Читает записи классов в список; каждая сразу добавляется в документ.
- Длина элемента не сходится с N и K — `FwxFormatException`: предупреждение;
  у алармов класс `?`.
- На листах алармов стандартный класс пишется по ID словом, свой — его Display
  name на первом языке панели.
- Лист «Классы алармов» строится по этому списку (если он пуст — по списку
  `MSG_ANALOGHANDL`): ID, Display name, Name (Errors, Warnings,
  Acknowledgement, No Acknowledgement по ID; свой — `?`), State machine
  (незнакомые байты — `?`), Log, E-mail address, 4 цвета `R, G, B` с заливкой
  ячейки (признак `0x20` — «цвета классов выключены», другой — `?`) и тексты
  состояний на каждом языке.
