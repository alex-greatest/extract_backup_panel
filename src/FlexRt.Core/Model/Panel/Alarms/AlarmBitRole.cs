namespace FlexRt.Model.Panel.Alarms;

/// <summary>Чем служит тег для дискретных алармов: элемент таблицы AREAPTR.</summary>
public enum AlarmBitRole
{
    /// <summary>Тег срабатывания (Trigger tag).</summary>
    Trigger,
    /// <summary>Тег квитирования с панели (Acknowledgment → HMI).</summary>
    HmiAcknowledgment,
    /// <summary>Тег квитирования из ПЛК (Acknowledgment → PLC).</summary>
    PlcAcknowledgment
}
