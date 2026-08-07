namespace VnbisAgent.Common;
public enum HuongEnum
{
    In = 1,
    Out = 2,
    Internal = 2
}
public enum TinhTrangEnum
{
    Unknown = 0,
    Incoming = 1,
    Outgoing = 2,
    Missed = 3,
    Rejected = 4,
    Blocked = 5,
    Offhook = 6,
    Idle = 7
}
public enum SourceEnum
{
    Unknown = 0,
    CallScreening = 1,
    BroadcastReceiver = 2,
}
/// <summary>Quản lý nhật ký sự kiện cuộc gọi (Được báo ra từ CallScreeningEx và CallBroadcastReceiver)</summary>
public class CallEventManager
{
    //1. CallEventItem được sinh ra từ (CallScreeningEx, CallBroadcastReceiver)
    //2. Chỉ lưu những Event nào có: CallerID và EventTime
    //3. Nếu thiếu CallerID hoặc EventTime thì chỉ báo ra sự kiện vả không ghi nhận
    private readonly List<CallEventItem> mCallEventList;
    /// <summary>New CallEventManager</summary>
    public CallEventManager()
    {
        mCallEventList = new List<CallEventItem>();
    }
    /// <summary>Lấy danh sách Event trong CallEventManager đang lưu trữ </summary>
    public IReadOnlyList<CallEventItem> CallEventsGet
    {
        get
        {
            return mCallEventList;
        }        
    }

    /// <summary>Tìm theo Key của mỗi EventItem</summary>
    public CallEventItem? FindId(string _UID)
    {
        if (_UID == "") return null;
        CallEventItem _Tmp = null;
        foreach ( CallEventItem _Event in mCallEventList)
        {
            if (_Event.UID == _UID)
            {
                _Tmp = _Event;
                break;
            }
        }
        return _Tmp;
    }
    public CallEventItem? FindCallLogID(long _CallLogID)
    {
        if (_CallLogID <= 0) return null;
        CallEventItem _Tmp = null;
        foreach (CallEventItem _Event in mCallEventList)
        {
            if (_Event.CallLogID == _CallLogID)
            {
                _Tmp = _Event;
                break;
            }
        }
        return _Tmp;
    }
    public bool UpdateEvent(CallEventItem _Event)
    {
        if (_Event == null)
        {
            VnbisAgent.Common.LogWriter.WriteLine("UpdateEvent _Event = null");
            return false; 
        }

        //Không cập nhật CallEventItem có CallerId, Nếu rỗng thì không cần cập nhật
        if (_Event.CallerId=="")
        {
            VnbisAgent.Common.LogWriter.WriteLine("UpdateEvent _Event.CallerId = ''");
            return false;
        }    
            
        CallEventItem _Exist = FindId(_Event.UID);

        if (_Exist != null)
        {
            VnbisAgent.Common.LogWriter.WriteLine("UpdateEvent Tồn tại _Event.UID không cập nhật và không hiển thị");
            return false;
        }
        mCallEventList.Add(_Event);
        return true;
    }
    public bool UpdateCallLog(CallLogItem _CalLLogItem)
    {
        if ((_CalLLogItem == null) || (_CalLLogItem.CallerId=="") || (_CalLLogItem.CallLogID <= 0))
        {
            VnbisAgent.Common.LogWriter.WriteLine("UpdateCallLog _CalLLogItem = null");
            return false;
        };
        //Bước 1: Tìm theo CallLogID
        CallEventItem _Exist = FindCallLogID(_CalLLogItem.CallLogID);
        //Bước 2: Tìm theo khoảng thời gian
        if (_Exist == null)
        {   
            foreach (CallEventItem _Item in mCallEventList)
            {
                if ((_Item.CallLogID <= 0) &&  (_Item.Huong == _CalLLogItem.Direction) && (_Item.CallerId == _CalLLogItem.CallerId))
                {                    
                    double seconds = Math.Abs((_Item.Ngay - _CalLLogItem.StartTime).TotalSeconds);
                    if (seconds<=20)
                    {
                        _Exist = _Item; 
                        break;
                    }
                }
            }
        }
        if (_Exist != null)
        {
            _Exist.CallLogID = _CalLLogItem.CallLogID;
            _Exist.BatDau = _CalLLogItem.StartTime.ToString("HH:mm:ss");
            _Exist.KetThuc = _CalLLogItem.EndTime.ToString("HH:mm:ss");
            return true;
        }
        else
        {
            return false;
        }            
    }
}

public class CallEventItem
{
    //1. CallEventItem được sinh ra từ (CallScreeningEx, CallBroadcastReceiver)
    //2. Mỗi lần nhận được sự kiện, ghi nhận thời gian tại di động EventTime
    public long SvrID { get; set; } = 0;
    public string CpuID { get; set; } = "";
    public long PbID { get; set; } = 0;
    public long NhanVienID { get; set; } = 0;
    public long TelID { get; set; } = 0;
    //Mã duy nhất của mỗi Event
    public string UID { get; set; } = "";
    public long KenhID { get; set; } = 0;
    public string Kenh { get; set; } = "";
    public string KenhSoMay { get; set; } = "";
    public HuongEnum Huong { get; set; } = HuongEnum.Internal;
    /// <summary>Số gọi đến, gọi đi: Quan trọng nhất</summary>
    public string CallerId { get; set; } = "";
    /// <summary>Thời điểm ghi nhận</summary>
    public DateTime Ngay { get; set; }
    /// <summary>Thời gian</summary>
    public string BatDau { get; set; } = "";
    /// <summary>Nhóm hội đàm</summary>
    public string NhomHoiDam { get; set; } = "";
    /// <summary>Tệp ghi âm</summary>
    public string TepGhiAm { get; set; } = "";
    public long DtID { get; set; } = 0;
    public string DtMa { get; set; } = "";
    public string DtTen { get; set; } = "";
    public string DtDiaChi { get; set; } = "";
    public string DtDienThoai { get; set; } = "";    
    public string NoiDung { get; set; } = "";
    public long Loi { get; set; } = 0;
    public string LoiThongBao { get; set; } = "";
    /// <summary>Thời điểm kết thúc</summary>
    public string KetThuc { get; set; } = "";
    /// <summary>Tình trạng</summary>
    public TinhTrangEnum TinhTrang { get; set; }= TinhTrangEnum.Unknown;
    public long TrangThai { get; set; } = 0;
    public double NgayCn { get; set; } = 0;
    /// <summary>Tên sim</summary>
    public string SimName { get; set; }="";
    //Nguồn phát tín hiệu
    public SourceEnum Source { get; set; }    
    //Mã đọc từ nhật ký cuộc gọi
    public long CallLogID { get; set; } = 0;   

    public CallEventItem() 
    {
        Ngay = DateTime.Now;
        BatDau = "";
        KetThuc = "";
        CallerId = "";
        TinhTrang = TinhTrangEnum.Unknown;
        SimName = "";
        Source = SourceEnum.Unknown;
        UID = VnbisAgent.Common.AppData.DeviceId + ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        CallLogID = 0;
        Huong = VnbisAgent.Common.HuongEnum.Internal;
    }
}

public class CallLogItem
{
    /// <summary>CallId do Hệ điều hành cung cấp (Android: CallLog.Calls._ID, Windows: TAPI/CallHistory ID, iOS: CallKit ID</summary>
    public long CallLogID { get; set; }
    //public long RawDate { get; set; }
    /// <summary>Thời điểm bắt đầu cuộc gọi</summary>
    public DateTime StartTime { get; set; }
    /// <summary>Thời điểm kết thúc</summary>
    public DateTime EndTime { get; set; }
    /// <summary>Thời lượng (giây)</summary>
    public int DurationSeconds { get; set; }
    /// <summary>Chiều cuộc gọi</summary>
    public HuongEnum Direction { get; set; }
    /// <summary>Đã hoàn thành chưa, cuộc gọi đã kết thúc chưa</summary>
    public bool Completed { get; set; }
    /// <summary>Số điện thoại</summary>
    public string CallerId { get; set; }
    /// <summary>Tên trong danh bạ điện thoại</summary>
    public string ContactName { get; set; }
    /// <summary>Tên hiển thị</summary>
    public string DisplayName { get; set; }
    /// <summary>ID SIM</summary>
    public string SimId { get; set; }
    /// <summary>Tên SIM</summary>
    public string SimName { get; set; }
    /// <summary>Đã đồng bộ Server chưa</summary>
    public bool Synced { get; set; }
    /// <summary>Lần cập nhật cuối</summary>
    public DateTime LastUpdate { get; set; }
    public CallLogItem()
    {
        CallLogID = 0;
        //RawDate = 0;
        CallerId = "";
        ContactName = "";
        DisplayName = "";
        SimId = "";
        SimName = "";
        StartTime = DateTime.MinValue;
        EndTime = DateTime.MinValue;
        DurationSeconds = 0;
        Direction = HuongEnum.Internal;
        Completed = false;
        Synced = false;
        LastUpdate = DateTime.UtcNow;
    }
    public override string ToString()
    {
        return
            CallLogID + " | " +
            Direction + " | " +
            CallerId + " | " +
            ContactName + " | " +
            DurationSeconds + "s";
    }
}