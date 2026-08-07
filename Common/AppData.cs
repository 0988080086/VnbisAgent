//Lưu biến toàn cục
//Với Android, phải gọi VnbisAgent.Common.AppData.Init(); trong MainActivity.OnCreate
//Với IOS thì cũng tương tự, nhưng chưa biết gọi ở đâu, tính sau
namespace VnbisAgent.Common;

public class AppData
{
    // ID duy nhất của thiết bị
    private static string _deviceId = "";
    //Đã đăng ký sử dụng CallScreening chưa
    private static bool _IsCallScreeningEnabled = false;
    // CallID lớn nhất đã đọc được
    private static long _LastCallID = 0;
    //Biến lưu trữ Context
    private static object? _serviceContext;

    /// <summary>Thuộc tính Mã thiết bị điện thoại </summary>
    public static string DeviceId
    {
        get 
        {
            return _deviceId;
        }

        set
        {
            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = value;
            }
        }
    }
    /// <summary>Thuộc tính cho phép gọi nhận tín hiệuCallScreening </summary>
    public static bool IsCallScreeningEnabled
    {
        get
        {
            return _IsCallScreeningEnabled;
        }
        set
        {
            _IsCallScreeningEnabled = value;
        }
    }
    public static long LastCallID
    {
        get
        {
            return _LastCallID;
        }

        set
        {
            _LastCallID = value;
        }
    }

    /// <summary>Thuộc tính ReadLogs</summary>
    public static Func<List<CallLogItem>>? ReadLogs { get; set; }

    /// <summary>Thuộc tính Context dùng chung toàn ứng dụng</summary>
    public static object? ServiceContext
    {
        get => _serviceContext;
        set => _serviceContext = value;
    }

    //Tạo sự kiện: Hiển thị Popup (Gọi thật từ OverlayManager.Show)
    public static void ShowPopupTel(string _CallerId, string _DisplayName)
    {
        // 1. Kiểm tra điều kiện an toàn đầu vào
        if (string.IsNullOrEmpty(_CallerId) || string.IsNullOrEmpty(_DisplayName))
        {
            VnbisAgent.Common.LogWriter.WriteLine("ShowPopupTel lỗi: _CallerId = ''");
            return;
        }
        try
        {
#if ANDROID
                if (ServiceContext is global::Android.Content.Context androidContext)
                {   
                    // Khởi tạo hộp thư Intent trỏ thẳng đến đích đến là dịch vụ chạy ngầm AgentService
                    var intent = new global::Android.Content.Intent(androidContext, typeof(VnbisAgent.Platforms.Android.AgentService));
                    // Gán hành động định danh để hàm AgentService.OnStartCommand nhận diện đúng nhánh rẽ
                    intent.SetAction("ACTION_SHOW_OVERLAY");
                    // Nhồi dữ liệu số điện thoại và tên người gọi vào gói tin
                    intent.PutExtra("SELECTED_PHONE", _CallerId);
                    intent.PutExtra("SELECTED_NAME", _DisplayName); // Khớp với trường lưu tên của bạn
                    // Phát lệnh chạy dịch vụ Foreground Service thích ứng theo luật bảo mật hệ điều hành Android
                    if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                    {
                        androidContext.StartForegroundService(intent);
                    }
                    else
                    {
                        androidContext.StartService(intent);
                    }
                }
                else
                {
                    VnbisAgent.Common.LogWriter.WriteLine("[Lỗi] ShowPopupTel thất bại vì AppData.ServiceContext đang bị rỗng (null)!");
                    return;
                }
#endif
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("Lỗi thực thi trong hàm gộp AppData.ShowPopupTel: " + ex.Message);
        }
    }
    //Tạo sự kiện: Hiển thị Popup (Gọi thật từ OverlayManager.Show)
    public static void ShowPopupTel(VnbisAgent.Common.CallEventItem? ev)
    {
        // 1. Kiểm tra điều kiện an toàn đầu vào
        if (ev == null || string.IsNullOrEmpty(ev.CallerId))
        {
            VnbisAgent.Common.LogWriter.WriteLine("ShowPopupTel lỗi: Dữ liệu cuộc gọi hoặc Số điện thoại đang bị rỗng.");
            return;
        }
        VnbisAgent.Common.LogWriter.WriteLine("AppData.ShowPopupTel");
        try
        {
#if ANDROID
            if (ServiceContext is global::Android.Content.Context androidContext)
            {
                // Khởi tạo hộp thư Intent trỏ thẳng đến đích đến là dịch vụ chạy ngầm AgentService
                var intent = new global::Android.Content.Intent(androidContext, typeof(VnbisAgent.Platforms.Android.AgentService));
                // Gán hành động định danh để hàm AgentService.OnStartCommand nhận diện đúng nhánh rẽ
                intent.SetAction("ACTION_SHOW_OVERLAY");
                // Nhồi dữ liệu số điện thoại và tên người gọi vào gói tin
                intent.PutExtra("SELECTED_PHONE", ev.CallerId);
                intent.PutExtra("SELECTED_NAME", ev.UID ?? "CallEvent.UINull"); // Khớp với trường lưu tên của bạn
                                                                                // Phát lệnh chạy dịch vụ Foreground Service thích ứng theo luật bảo mật hệ điều hành Android
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    androidContext.StartForegroundService(intent);
                }
                else
                {
                    androidContext.StartService(intent);
                }
            }
            else
            {
                VnbisAgent.Common.LogWriter.WriteLine("[Lỗi] ShowPopupTel thất bại vì AppData.ServiceContext đang bị rỗng (null)!");
                return;
            }
#endif
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("Lỗi thực thi trong hàm gộp AppData.ShowPopupTel: " + ex.Message);
        }
    }
}