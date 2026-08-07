using System.Data;

namespace VnbisAgent.Admin;
public enum ButtonStyleEnum
{
    Normal = 1,
    Primary = 2,
    Success = 3,
    Danger = 4,
    Warning = 5,
    Link = 6
}
public class TblButton
{
    private List<TblButtonItem> _ClassList;

    public List<TblButtonItem> ClassList
    {
        get { return _ClassList; }
    }

    public TblButton()
    {
        _ClassList = new List<TblButtonItem>();

        //------------------------------------
        // Ví dụ nút Lưu
        //------------------------------------

        TblButtonItem item = new TblButtonItem();

        item.FormID = FormIDEnum.FrmCrmTelPopUp;

        item.ButtonID = "Save";

        item.Caption = "Lưu khách";

        item.Icon = "save.png";

        item.EventName = "SAVE_CUSTOMER";

        item.Section = SectionEnum.Footer;

        item.SortOrder = 1;

        item.Visible = true;

        item.Enabled = true;

        item.Width = 3;

        item.Height = 1;

        item.ButtonStyle = ButtonStyleEnum.Success;

        item.ForeColor = "#FFFFFF";

        item.BackColor = "#4CAF50";

        _ClassList.Add(item);
    }

    //--------------------------------------------

    private DataTable CreateTableStructure()
    {
        DataTable dt = new DataTable("TblButton");

        dt.Columns.Add("FormID", typeof(long));

        dt.Columns.Add("ButtonID", typeof(string));

        dt.Columns.Add("Caption", typeof(string));

        dt.Columns.Add("Icon", typeof(string));

        dt.Columns.Add("EventName", typeof(string));

        dt.Columns.Add("Section", typeof(long));

        dt.Columns.Add("SortOrder", typeof(long));

        dt.Columns.Add("Visible", typeof(bool));

        dt.Columns.Add("Enabled", typeof(bool));

        dt.Columns.Add("Width", typeof(long));

        dt.Columns.Add("Height", typeof(long));

        dt.Columns.Add("ButtonStyle", typeof(long));

        dt.Columns.Add("ForeColor", typeof(string));

        dt.Columns.Add("BackColor", typeof(string));

        return dt;
    }

    //--------------------------------------------

    public DataTable ItemToTable(FormIDEnum formID)
    {
        DataTable dt = CreateTableStructure();

        foreach (TblButtonItem item in _ClassList)
        {
            if (item.FormID == formID)
                dt.Rows.Add(item.ToObject());
        }

        return dt;
    }

    //--------------------------------------------

    public DataTable ItemsToTable()
    {
        DataTable dt = CreateTableStructure();

        foreach (TblButtonItem item in _ClassList)
        {
            dt.Rows.Add(item.ToObject());
        }

        return dt;
    }
}

public class TblButtonItem
{
    public FormIDEnum FormID { get; set; }

    public string ButtonID { get; set; }

    public string Caption { get; set; }

    public string Icon { get; set; }

    public string EventName { get; set; }

    public SectionEnum Section { get; set; }

    public long SortOrder { get; set; }

    public bool Visible { get; set; }

    public bool Enabled { get; set; }

    public long Width { get; set; }

    public long Height { get; set; }

    public ButtonStyleEnum ButtonStyle { get; set; }

    public string ForeColor { get; set; }

    public string BackColor { get; set; }

    public object[] ToObject()
    {
        return new object[]
        {
            (long)FormID,
            ButtonID,
            Caption,
            Icon,
            EventName,
            (long)Section,
            SortOrder,
            Visible,
            Enabled,
            Width,
            Height,
            (long)ButtonStyle,
            ForeColor,
            BackColor
        };
    }
}