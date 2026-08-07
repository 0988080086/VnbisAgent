using System.Data;

namespace VnbisAgent.Admin;
public enum FormTypeEnum
{
    Nomall = 1,
    PopUp = 2,        
}
public enum FormIDEnum
{
    Unknow = 0,
    FrmMain = 1,
    FrmCrmTelPopUp = 11,
    FrmCrmSmsPopUp = 12,
    FrmCrmZaloPopUp = 13,

    FrmOrderList = 21,
    FrmOrder = 22,

    FrmSaleList = 31,
    FrmSale = 32,
}
public enum SizeTypeEnum
{
    Auto = 1,
    Manual = 2,
}
public class TblForm
{
    private List<TblFormItem> _ClassList;
    public List<TblFormItem> ClassList
    {
        get { return _ClassList; }
    }
    public TblForm()
    {
        _ClassList = new List<TblFormItem>();
        //Khai báo form Popup
        TblFormItem _item = new TblFormItem();
        _item.FormType = FormTypeEnum.PopUp;
        _item.FormID = FormIDEnum.FrmCrmTelPopUp;
        _item.FormName = "FrmCrmTelPopUp";
        _item.Caption = "Thông tin cuộc gọi";
        _item.AutoSize = SizeTypeEnum.Auto;
        _item.Top = 0;
        _item.Left = 0;
        _item.Width = 0;
        _item.Height = 0;
        _item.BackColor = "";
        _item.ForeColor = "";
        _ClassList.Add (_item);
    }
    public DataTable ItemToTable(FormIDEnum _FormID)
    {
        DataTable _Dt = CreateTableStructure();

        foreach (TblFormItem _Item in _ClassList)
        {
            if (_Item.FormID == _FormID)
            {
                DataRow _Dr = _Dt.NewRow();

                _Dr["FormType"] = (long)_Item.FormType;
                _Dr["FormID"] = (long)_Item.FormID;
                _Dr["FormName"] = _Item.FormName;
                _Dr["Caption"] = _Item.Caption;
                _Dr["AutoSize"] = (long)_Item.AutoSize;
                _Dr["Top"] = _Item.Top;
                _Dr["Left"] = _Item.Left;
                _Dr["Width"] = _Item.Width;
                _Dr["Height"] = _Item.Height;
                _Dr["BackColor"] = _Item.BackColor;
                _Dr["ForeColor"] = _Item.ForeColor;

                _Dt.Rows.Add(_Dr);

                break;
            }
        }

        return _Dt;
    }
    public DataTable ItemsToTable()
    {
        DataTable _Dt = CreateTableStructure();

        foreach (TblFormItem _Item in _ClassList)
        {
            DataRow _Dr = _Dt.NewRow();

            _Dr["FormType"] = (long)_Item.FormType;
            _Dr["FormID"] = (long)_Item.FormID;
            _Dr["FormName"] = _Item.FormName;
            _Dr["Caption"] = _Item.Caption;
            _Dr["AutoSize"] = (long)_Item.AutoSize;
            _Dr["Top"] = _Item.Top;
            _Dr["Left"] = _Item.Left;
            _Dr["Width"] = _Item.Width;
            _Dr["Height"] = _Item.Height;
            _Dr["BackColor"] = _Item.BackColor;
            _Dr["ForeColor"] = _Item.ForeColor;

            _Dt.Rows.Add(_Dr);
        }

        return _Dt;
    }
    private DataTable CreateTableStructure()
    {
        DataTable _Dt = new DataTable();

        _Dt.Columns.Add("FormType", typeof(long));
        _Dt.Columns.Add("FormID", typeof(long));
        _Dt.Columns.Add("FormName", typeof(string));
        _Dt.Columns.Add("Caption", typeof(string));
        _Dt.Columns.Add("AutoSize", typeof(long));
        _Dt.Columns.Add("Top", typeof(long));
        _Dt.Columns.Add("Left", typeof(long));
        _Dt.Columns.Add("Width", typeof(long));
        _Dt.Columns.Add("Height", typeof(long));
        _Dt.Columns.Add("BackColor", typeof(string));
        _Dt.Columns.Add("ForeColor", typeof(string));

        return _Dt;
    }
}
public class TblFormItem
{
    public FormTypeEnum _FormType;
    public FormIDEnum _FormID;
    public string _FormName;
    public string _Caption;
    public SizeTypeEnum _AutoSize;
    public long _Top;
    public long _Left;
    public long _Width;
    public long _Height;
    public string _BackColor;
    public string _ForeColor;
    public TblFormItem()
    {
        _FormType = FormTypeEnum.Nomall;
        _FormID = FormIDEnum.Unknow;
        _FormName = "";
        _Caption = "";
        _AutoSize = SizeTypeEnum.Auto;
        _Top = 0;
        _Left = 0;
        _Width = 0;
        _Height = 0;
        _BackColor = null; //global::Microsoft.Maui.Graphics.Color.FromRgb(255, 255, 255);
        _ForeColor = null; //global::Microsoft.Maui.Graphics.Color.FromRgb(255, 255, 255);
    }
    public FormTypeEnum FormType { get { return _FormType; } set { _FormType = value; } }
    public FormIDEnum FormID { get { return _FormID; } set { _FormID = value; } }
    public string FormName { get { return _FormName; } set { _FormName = value; } }
    public string Caption { get { return _Caption; } set { _Caption = value; } }
    public SizeTypeEnum AutoSize { get { return _AutoSize; } set { _AutoSize = value; } }
    public long Top { get { return _Top; } set { _Top = value; } }
    public long Left { get { return _Left; } set { _Left = value; } }
    public long Width { get { return _Width; } set { _Width = value; } }
    public long Height { get { return _Height; } set { _Height = value; } }
    public string BackColor { get { return _BackColor; } set { _BackColor = value; } }
    public string ForeColor { get { return _ForeColor; } set { _ForeColor = value; } }
}
