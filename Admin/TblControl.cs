using System.Data;

namespace VnbisAgent.Admin;
public enum SectionEnum
{
    Header = 1,
    Detail = 2,
    Footer = 3,
}

public enum ControlTypeEnum
{
    Label = 1,
    Text = 2,
    Date = 3,
    Time = 4,
    Memo = 5,
    Number = 6,
    RealNumber = 7,
    CheckBox = 8,
    Option = 9,
    ComboList = 10,
    ComboBox = 11,
    ComboTree = 12,
    Grid = 13,
    Image = 14,
    Button = 15,
    Line = 16,
}

public enum DataTypeEnum
{
    String = 1,
    Number = 2,
    RealNumber = 3,
    Date = 4,
    Time = 5,
    Memo = 6,
    CheckBox = 7,
    Option = 8,
}

public class TblControl
{
    private List<TblControlItem> _ClassList;

    public List<TblControlItem> ClassList
    {
        get { return _ClassList; }
    }

    public TblControl()
    {
        _ClassList = new List<TblControlItem>();

        //--------------------------------------
        // Ví dụ Control đầu tiên
        //--------------------------------------

        TblControlItem item = new TblControlItem();

        item.FormID = FormIDEnum.FrmCrmTelPopUp;

        item.TabIndex = 1;

        item.Section = SectionEnum.Header;

        item.ControlID = "Phone";

        item.ParentID = "";

        item.ControlType = ControlTypeEnum.Label;

        item.Caption = "Điện thoại";

        item.FieldName = "Phone";

        item.DataType = DataTypeEnum.String;

        item.Value = "";

        item.ReadOnly = true;

        item.Visible = true;

        item.Required = false;

        item.Hint = "";

        item.MaxLength = 20;

        item.LabelWidth = 2;

        item.ControlWidth = 8;

        item.ButtonWidth = 2;

        item.ButtonCaption = "";

        item.ButtonEvent = "";

        item.DataSource = "";

        item.DisplayMember = "";

        item.ValueMember = "";

        item.SortOrder = 1;

        item.ForeColor = "";

        item.BackColor = "";

        _ClassList.Add(item);
    }

    //--------------------------------------------
    // Tạo cấu trúc DataTable
    //--------------------------------------------

    private DataTable CreateTableStructure()
    {
        DataTable dt = new DataTable("TblControl");

        dt.Columns.Add("FormID", typeof(long));

        dt.Columns.Add("TabIndex", typeof(long));

        dt.Columns.Add("Section", typeof(long));

        dt.Columns.Add("ControlID", typeof(string));

        dt.Columns.Add("ParentID", typeof(string));

        dt.Columns.Add("ControlType", typeof(long));

        dt.Columns.Add("Caption", typeof(string));

        dt.Columns.Add("FieldName", typeof(string));

        dt.Columns.Add("DataType", typeof(long));

        dt.Columns.Add("Value", typeof(string));

        dt.Columns.Add("ReadOnly", typeof(bool));

        dt.Columns.Add("Visible", typeof(bool));

        dt.Columns.Add("Required", typeof(bool));

        dt.Columns.Add("Hint", typeof(string));

        dt.Columns.Add("MaxLength", typeof(long));

        dt.Columns.Add("LabelWidth", typeof(long));

        dt.Columns.Add("ControlWidth", typeof(long));

        dt.Columns.Add("ButtonWidth", typeof(long));

        dt.Columns.Add("ButtonCaption", typeof(string));

        dt.Columns.Add("ButtonEvent", typeof(string));

        dt.Columns.Add("DataSource", typeof(string));

        dt.Columns.Add("DisplayMember", typeof(string));

        dt.Columns.Add("ValueMember", typeof(string));

        dt.Columns.Add("SortOrder", typeof(long));

        dt.Columns.Add("ForeColor", typeof(string));

        dt.Columns.Add("BackColor", typeof(string));

        return dt;
    }

    //--------------------------------------------

    public DataTable ItemToTable(FormIDEnum formID)
    {
        DataTable dt = CreateTableStructure();

        foreach (TblControlItem item in _ClassList)
        {
            if (item.FormID == formID)
            {
                dt.Rows.Add(item.ToObject());
            }
        }

        return dt;
    }

    //--------------------------------------------

    public DataTable ItemsToTable()
    {
        DataTable dt = CreateTableStructure();

        foreach (TblControlItem item in _ClassList)
        {
            dt.Rows.Add(item.ToObject());
        }

        return dt;
    }
}

public class TblControlItem
{
    public FormIDEnum FormID { get; set; }

    public long TabIndex { get; set; }

    public SectionEnum Section { get; set; }

    public string ControlID { get; set; }

    public string ParentID { get; set; }

    public ControlTypeEnum ControlType { get; set; }

    public string Caption { get; set; }

    public string FieldName { get; set; }

    public DataTypeEnum DataType { get; set; }

    public string Value { get; set; }

    public bool ReadOnly { get; set; }

    public bool Visible { get; set; }

    public bool Required { get; set; }

    public string Hint { get; set; }

    public long MaxLength { get; set; }

    public long LabelWidth { get; set; }

    public long ControlWidth { get; set; }

    public long ButtonWidth { get; set; }

    public string ButtonCaption { get; set; }

    public string ButtonEvent { get; set; }

    public string DataSource { get; set; }

    public string DisplayMember { get; set; }

    public string ValueMember { get; set; }

    public long SortOrder { get; set; }

    public string ForeColor { get; set; }

    public string BackColor { get; set; }

    public object[] ToObject()
    {
        return new object[]
        {
            (long)FormID,
            TabIndex,
            (long)Section,
            ControlID,
            ParentID,
            (long)ControlType,
            Caption,
            FieldName,
            (long)DataType,
            Value,
            ReadOnly,
            Visible,
            Required,
            Hint,
            MaxLength,
            LabelWidth,
            ControlWidth,
            ButtonWidth,
            ButtonCaption,
            ButtonEvent,
            DataSource,
            DisplayMember,
            ValueMember,
            SortOrder,
            ForeColor,
            BackColor
        };
    }
}