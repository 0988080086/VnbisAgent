using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Speech.Tts;
using AndroidX.Core.Content;
using Javax.Annotation.Meta;
using VnbisAgent.Common;
using VnbisAgent.Platforms.Android;
using VnbisAgent.Platforms.Android.Services;

//Khai báo Service, cần chi tiết: [Service(Exported = false,ForegroundServiceType = ForegroundService.TypeDataSync)]
//Và cần bổ sung vào AndroidManifest.xml: <uses-permission android:name="android.permission.FOREGROUND_SERVICE_DATA_SYNC" />
namespace VnbisAgent.Platforms.Android;
[Service(Exported = false,ForegroundServiceType = ForegroundService.TypeDataSync)]
public class AgentService : Service
{
    private static global::Android.Content.Context _Context;
    //public IReadOnlyList<global::Android.Content.Context> ContextGet
    //    { get { return ContextGet; } }
    private CallLogReader? _callLogReader;    
    // KHAI BÁO MỚI: Biến quản lý hiển thị đè màn hình cuộc gọi
    private OverlayManager? _overlayManager;

    // Android tạo Service
    public override void OnCreate()
    {
        base.OnCreate();
        _overlayManager = new OverlayManager(this);
        // Đăng ký sự kiện ngắt cuộc gọi từ Receiver
        CallBroadcastReceiver.OnCallEnded += HandleCallEnded;

        //Bước 1: Khởi tạo biến môi trường        
        _Context = ApplicationContext;
        if (_Context == null) return;

        //Lưu trữ _Context để sử dụng chung
        VnbisAgent.Common.AppData.ServiceContext = _Context;

        //Bước 2: KHỞI TẠO MỚI: Gắn dịch vụ Overlay với Context hiện tại của AgentService
        _overlayManager = new OverlayManager(this);

        //Bước 3: Khởi tạo CallLogReader
        _callLogReader = new CallLogReader(_Context);
        if (ContextCompat.CheckSelfPermission(this, global::Android.Manifest.Permission.ReadCallLog) == Permission.Granted)
        {       
            //Đọc lần đầu, chuyển thành nhật ký cuộc gọi
            List<CallLogItem> _List= _callLogReader.ReadLogs();
            if ((_List != null) && (_List.Count>0))
            {
                VnbisAgent.Common.CallEventItem _EventItem;                
                foreach (VnbisAgent.Common.CallLogItem _LogItem in _List )
                {
                    _EventItem = new VnbisAgent.Common.CallEventItem();
                    _EventItem.Ngay = _LogItem.StartTime;
                    _EventItem.BatDau = _EventItem.Ngay.ToString("HH:mm:ss");
                    _EventItem.KetThuc = _LogItem.EndTime.ToString("HH:mm:ss");
                    _EventItem.UID=VnbisAgent.Common.AppData.DeviceId + ((DateTimeOffset)_EventItem.Ngay).ToUnixTimeSeconds();
                    _EventItem.CallLogID = _LogItem.CallLogID;
                    _EventItem.Huong = _LogItem.Direction;                    
                    if (_LogItem.Direction==VnbisAgent.Common.HuongEnum.In )
                    {
                        _EventItem.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Incoming;                        
                    }
                    else
                    {
                        _EventItem.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Outgoing;
                    }
                    //Gọi cập nhật vào danh sách CallEventManager
                    //VnbisAgent.Common.CallManager.Instance.
                }
            }
            VnbisAgent.Common.AppData.ReadLogs = _callLogReader.ReadLogs;
            VnbisAgent.Common.CallManager.Instance.NotifySessionChanged();
        }        
    }
    private void HandleCallEnded()
    {
        // Chạy trên Main Thread (UI Thread) để đóng Popup an toàn
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _overlayManager?.Close();
        });
    }
    public override void OnDestroy()
    {
        // Hủy đăng ký để tránh leak bộ nhớ
        CallBroadcastReceiver.OnCallEnded -= HandleCallEnded;
        base.OnDestroy();
    }
    // Hàm duy nhất: Nhận sự kiện đón nhận tất cả các lệnh điều hướng được gửi từ màn hình UI XAML hoặc từ các file xử lý quyền gửi sang.
    public override StartCommandResult OnStartCommand(Intent? intent,StartCommandFlags flags,int startId)
    {
        CreateNotification();

        //Đoạn đọc dữ liệu từ ngoài truyền vào
        if (intent != null && intent.Action == "ACTION_SHOW_OVERLAY")
        {
            // Trích xuất thông tin Số điện thoại và Tên khách hàng ra từ Intent
            string phoneNumber = intent.GetStringExtra("SELECTED_PHONE") ?? "Không rõ số";
            string customerName = intent.GetStringExtra("SELECTED_NAME") ?? "Khách hàng lạ";

            // Ra lệnh cho biến toàn cục _overlayManager của bạn vẽ file phone_call_overlay.xml lên
            _overlayManager?.Show(phoneNumber, VnbisAgent.Common.AppData.OVerlayDataTemp);
        }

        //Sự kiện đã cấp quyền đọc nhật ký, thì đọc lại
        if (intent != null && intent.Action == "ACTION_REFRESH_CALL_LOGS")
        {   
            try
            {
                // Thực thi gọi hàm đọc nhật ký cuộc gọi hệ thống của bạn tại đây
                // Lúc này đối tượng reader của bạn đã được gán biến toàn cục ở OnCreate()
                if (_callLogReader != null)
                {
                    _callLogReader.ReadLogs();
                    //Đọc lần đầu, chuyển thành nhật ký cuộc gọi
                    List<CallLogItem> _List = _callLogReader.ReadLogs();
                    if ((_List != null) && (_List.Count > 0))
                    {
                        VnbisAgent.Common.CallEventItem _EventItem;
                        foreach (VnbisAgent.Common.CallLogItem _LogItem in _List)
                        {
                            _EventItem = new VnbisAgent.Common.CallEventItem();
                            _EventItem.Ngay = _LogItem.StartTime;
                            _EventItem.BatDau = _EventItem.Ngay.ToString("HH:mm:ss");
                            _EventItem.KetThuc = _LogItem.EndTime.ToString("HH:mm:ss");
                            _EventItem.UID = VnbisAgent.Common.AppData.DeviceId + ((DateTimeOffset)_EventItem.Ngay).ToUnixTimeSeconds();
                            _EventItem.CallLogID = _LogItem.CallLogID;
                            _EventItem.Huong = _LogItem.Direction;
                            if (_LogItem.Direction == VnbisAgent.Common.HuongEnum.In)
                            {
                                _EventItem.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Incoming;
                            }
                            else
                            {
                                _EventItem.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Outgoing;
                            }
                            //Gọi cập nhật vào danh sách CallEventManager
                            //VnbisAgent.Common.CallManager.Instance.
                        }
                    }
                    VnbisAgent.Common.AppData.ReadLogs = _callLogReader.ReadLogs;
                    VnbisAgent.Common.CallManager.Instance.NotifySessionChanged();
                }
                else
                {
                    VnbisAgent.Common.LogWriter.WriteLine("AgentService: _callLogReader is Null");
                }
            }
            catch (Exception ex)
            {
                VnbisAgent.Common.LogWriter.WriteLine("AgentService lỗi ReadLogs: " + ex.Message);
            }
        }
        return StartCommandResult.Sticky;
    }
    // Không dùng Bind
    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }
    // Notification
    private void CreateNotification()
    {
        string channelId;
        channelId = "VNBIS_AGENT";
        NotificationManager manager;
        manager = (NotificationManager)GetSystemService(NotificationService);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            NotificationChannel channel;
            channel = new NotificationChannel(channelId,"Vnbis Agent",NotificationImportance.Low);
            manager.CreateNotificationChannel(channel);
        }

        Notification notification;
        notification = new Notification.Builder(this, channelId)
            .SetContentTitle("VnbisAgent")
            .SetContentText("Agent đang chạy...")
            .SetSmallIcon(VnbisAgent.Resource.Mipmap.appicon)
            .Build();
        StartForeground(1000, notification);        
    }
}