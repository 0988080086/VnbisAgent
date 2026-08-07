namespace VnbisAgent.Common;
/// <summary>
/// Trung tâm quản lý cuộc gọi của ứng dụng: Quản lý sự kiện PHONE_STATE và Danh sách CALL_LOG.
/// </summary>
public class CallManager
{
    /// <summary>Quản lý sự kiện có CallLogItem mới được sinh ra</summary>
    public event Action? SessionChanged;
    /// <summary>Khai báo private để không sinh thêm một instance CallManager khác trong ứng dụng</summary>
    private static readonly CallManager _instance = new CallManager();

    private int _reading; //Trạng thái đang đọc CALL_LOG.
    private bool _pending;  //Mỗi lần đọc CALL_LOG trong điện thoại, thì gán _pending yêu cầu đọc lại lần 2.    

    private readonly CallEventManager mCallEventManager;

    /// <summary>Cái này thì như vb.net</summary>
    public static CallManager Instance
    {
        get { return _instance; }
    }
    private CallManager()
    {   
        mCallEventManager = new CallEventManager();
        _reading = 0;
        _pending = false;   //Khởi động chưa có Event, thì không cho đọc lại, khi có 1 event đọc lần 1 thì cho phép đọc lần 2 và kết thúc.        
    }

    /// <summary>Trả về danh sách nhật ký Event</summary>
    public IReadOnlyList<CallEventItem> CallEventGet
    {
        get { return mCallEventManager.CallEventsGet; }
    }

    /// <summary>BroadcastReceiver Bắn sự kiện CALL_LOG vào đây. Mỗi khi Android phát sinh sự kiện.</summary>
    public void PushEvent(CallEventItem? ev)
    {
        if (ev == null) 
        {
            VnbisAgent.Common.LogWriter.WriteLine("PushEvent ev null ");
            return;
        }
        //ev.Direction=VnbisAgent.Common.
        //1. Nếu 
        //1. Nếu sự kiện từ BroadcastReceiver: không có callerid thì: Gọi WakeUp để đọc nhật ký mới và báo ra.
        //2. Nếu sự kiện từ BroadcastReceiver: Có callerid, thì cập nhật vào mCallEventManager

        if (ev.CallerId=="")
        {
            VnbisAgent.Common.LogWriter.WriteLine("PushEvent CallerId=='" + ev.CallerId + "'");
            return;            
        }
        else
        {
            bool _Value = mCallEventManager.UpdateEvent(ev);            
            if (_Value == true)
            {   
                //Bước 1: Hiển thị lên màn hình Popup
                VnbisAgent.Common.AppData.ShowPopupTel(ev);
                //Bước 2: Báo về PC có callerid mới

                //Bước 3: Gọi WakeUp đọc nhật ký cuộc gọi (Trong WakeUp sẽ tự lấy CallLog và Update Popup)
                WakeUp();
            }
            else
            {
                VnbisAgent.Common.LogWriter.WriteLine("PushEvent mCallEventManager.UpdateEvent(ev) fail");
            }
        }
    }    

    /// <summary>Kích hoạt sự kiện có CALL_LOG mới cho ứng dụng => Để đọc CallLog.</summary>
    private void WakeUp()
    {   
        // Nếu đang đọc: đánh dấu đọc lại => Thoát
        if (Interlocked.Exchange(ref _reading, 1) == 1)
        {   
            _pending = true;
            return;
        }

        //Báo cho lịch kích hoạt chạy đọc CALL_LOG ở một Threat khác
        Task.Run(() =>
        {
            try
            {
                SyncLatestCallLogs();
            }
            finally
            {
                Interlocked.Exchange(ref _reading, 0);
                if (_pending)
                {
                    WakeUp();
                }
            }
        });
    }

    /// <summary>Đọc CALL_LOG và Đồng bộ nhật ký</summary>
    private void SyncLatestCallLogs()
    {
        bool _HaveNewCallLogItem=false;
        string _incomingNumber = "";
        string _SELECTED_NAME = "";
        do
        {
            _pending = false;
            if (AppData.ReadLogs == null)
            {
                VnbisAgent.Common.LogWriter.WriteLine("SyncLatestCallLogs AppData.ReadLogs == null");
                return;
            } 

            //Gọi Invoke này, để CallLogReader.cs gọi đọc nhật ký cuộc gọi
            //Thông thường sẽ đọc ra 1 cuộc gọi có callid lớn nhất. Nhưng nếu lần đầu đọc sẽ ra 5 cuộc vì lần đầu callid = 0.
            List<CallLogItem> _CallLogs = AppData.ReadLogs.Invoke();

            //Cập nhật tất cả các CallLogItem đọc được vào danh sách
            foreach (CallLogItem log in _CallLogs)
            {
                //mCallLogManager.UpdateInSertLog(log);

                _HaveNewCallLogItem = true;
                _incomingNumber = log.CallerId;
                _SELECTED_NAME = log.ContactName;
            }

        } while (_pending);
        //Nếu có cập nhật mới CallLogItem mới được cập nhật, thì tạo sự kiện cho trang nhật ký UI hiển thị lại
        if (_HaveNewCallLogItem == true)
        {
            //Làm mới nhật ký CallLog trên PhoneTestPage
            NotifySessionChanged();
            
            //Ra lệnh để AgentService.OnStartCommand (Đường hầm ứng dụng) mở cửa sổ Popup
        }
    }    
    public void NotifySessionChanged()
    {
        SessionChanged?.Invoke();
    }
}