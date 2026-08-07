using System.Data;

namespace VnbisAgent.Admin;

public class TblDataItem
{
    public FormIDEnum FormID { get; set; }

    public string ControlID { get; set; }

    public string Key { get; set; }

    public object Value { get; set; }

    public object OldValue { get; set; }

    public bool Visible { get; set; }

    public bool ReadOnly { get; set; }

    public bool Required { get; set; }

    public object[] ToObject()
    {
        return new object[]
        {
            (long)FormID,
            ControlID,
            Key,
            Value,
            OldValue,
            Visible,
            ReadOnly,
            Required
        };
    }
}