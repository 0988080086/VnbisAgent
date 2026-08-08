using System;
using System.Data;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telecom;
using Android.Views;
using Android.Widget;

namespace VnbisAgent.Platforms.Android;
public class OverlayManager
{
    private readonly Context _context;
    private global::Android.Views.IWindowManager? _windowManager;
    private global::Android.Views.View? _overlayView;
    private WindowManagerLayoutParams? _layoutParam;
    private bool _isShowing = false;

    // Khai báo các Control trên giao diện UI
    private global::Android.Widget.TextView? _lblPhone;
    private global::Android.Widget.ListView? _lstInfo;
    private global::Android.Widget.Button? _btnAnswer;
    private global::Android.Widget.Button? _btnReject;
    private global::Android.Widget.Button? _btnClose;

    // Khởi tạo nhận Context từ Service truyền qua
    public OverlayManager(Context context)
    {
        _context = context;
    }

    /// <summary>
    /// Hiển thị Popup Overlay cuộc gọi
    /// </summary>
    /// <param name="incomingNumber">Số điện thoại cuộc gọi đến</param>
    /// <param name="dtCustomerData">DataTable chứa 2 cột ("TieuDe", "NoiDung")</param>
    public void Show(string incomingNumber, DataTable dtData)
    {
        if (_isShowing) return;

        if (!global::Android.Provider.Settings.CanDrawOverlays(_context)) 
        {
            VnbisAgent.Common.LogWriter.WriteLine("OverlayManager.Show không kích hoạt do chưa cấp quyền Overlay");
            return;
        }         
                
        try
        {
            // 1. Khởi tạo WindowManager
            var serviceObj = _context.GetSystemService(Context.WindowService);
            if (serviceObj == null) return;
            _windowManager = serviceObj.JavaCast<global::Android.Views.IWindowManager>();
            if (_windowManager == null) return;

            // 2. Lấy kích thước màn hình thiết bị
            var displayMetrics = _context.Resources.DisplayMetrics;
            int screenHeight = displayMetrics.HeightPixels; // Chiều cao thực tế của màn hình (px)
            int screenWidth = displayMetrics.WidthPixels;   // Chiều rộng thực tế của màn hình (px)
            // 3. Kiểm tra trạng thái ứng dụng (Foreground / Background)
            bool isAppInForeground = IsAppInForeground();
            // 4. Bơm (Inflate) View từ XML
            LayoutInflater? inflater = LayoutInflater.From(_context);
            if (inflater == null) return;

            _overlayView = inflater.Inflate(VnbisAgent.Resource.Layout.phone_call_overlay, null);
            if (_overlayView == null) return;

#pragma warning disable CA1416
            // 5. Cấu hình WindowManagerLayoutParams
            _layoutParam = new global::Android.Views.WindowManagerLayoutParams(
                global::Android.Views.WindowManagerLayoutParams.MatchParent,
                global::Android.Views.WindowManagerLayoutParams.WrapContent,
                global::Android.Views.WindowManagerTypes.ApplicationOverlay, // Bắt buộc cho Service chạy ngầm
                global::Android.Views.WindowManagerFlags.NotTouchModal |
                global::Android.Views.WindowManagerFlags.LayoutInScreen |
                global::Android.Views.WindowManagerFlags.ShowWhenLocked | // CỜ MỚI: Ép đè lên màn hình khóa
                global::Android.Views.WindowManagerFlags.TurnScreenOn |   // CỜ MỚI: Tự động bật sáng màn hình khi chuông reo
                global::Android.Views.WindowManagerFlags.DismissKeyguard,  // CỜ MỚI: Đẩy lùi màn hình bảo mật sang phía sau
                global::Android.Graphics.Format.Opaque); //Opaque Hoặc Translucent tùy giao diện của bạn
#pragma warning restore CA1416

            // 6. Căn chỉnh vị trí & chiều cao dựa trên trạng thái App
            if (isAppInForeground && VnbisAgent.Common.AppData.TopMarginPecent > 0)
            {
                // App đang mở trên màn hình -> Thu nhỏ & né thanh cuộc gọi của hệ thống
                _layoutParam.Gravity = GravityFlags.Top | GravityFlags.CenterHorizontal;
                _layoutParam.Y = (int)(screenHeight * VnbisAgent.Common.AppData.TopMarginPecent / 100.0);
                _layoutParam.Height = (int)(screenHeight * (100 - VnbisAgent.Common.AppData.TopMarginPecent - VnbisAgent.Common.AppData.ButtonMarginPecent) / 100.0);
            }
            else
            {
                // App đang ẩn / Màn hình tắt -> Đặt chính giữa, ôm sát nội dung (WrapContent)
                _layoutParam.Gravity = global::Android.Views.GravityFlags.Center;
                _layoutParam.Y = 0;
                _layoutParam.Height = global::Android.Views.WindowManagerLayoutParams.WrapContent;
            }

            // 7. Ánh xạ các Controls từ file XML
            _lblPhone = _overlayView.FindViewById<global::Android.Widget.TextView>(VnbisAgent.Resource.Id.lblPhone);
            _lstInfo = _overlayView.FindViewById<global::Android.Widget.ListView>(VnbisAgent.Resource.Id.lstInfo);
            _btnAnswer = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnAnswer);
            _btnReject = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnReject);
            _btnClose = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnClose);

            // 8. Đổ dữ liệu vào Giao diện
            if (_lblPhone != null)
            {
                _lblPhone.Text = incomingNumber;
            }
            // Nếu dữ liệu truyền từ ngoài vào bị NULL -> Tạo dữ liệu mặc định tránh crash App
            if (dtData == null)
            {
                dtData = VnbisAgent.Common.AppData.OVerlayDataTemp;
            }
            // Gán Adapter dữ liệu vào ListView
            if (_lstInfo != null)
            {
                _lstInfo.Adapter = new OverlayData(_context, dtData);
            }

            // 9. Gán sự kiện tương tác cho các Nút bấm
            // NÚT NGHE: Kích hoạt nghe cuộc gọi và ẩn nút nghe/từ chối, GIỮ POPUP HỂNH THỊ
            if (_btnAnswer != null)
            {
                _btnAnswer.Click += (s, e) =>
                {
                    AnswerCall(); // Chỉ kích hoạt nghe cuộc gọi
                    // (Tùy chọn) Ẩn nút "Nghe" và nút "Từ chối" đi, chỉ chừa lại nút "Đóng" 
                    // để giao diện gọn hơn khi đã bắt máy:
                    if (_btnAnswer != null) _btnAnswer.Visibility = ViewStates.Gone;
                    if (_btnReject != null) _btnReject.Visibility = ViewStates.Gone;
                };
            }

            // NÚT HỦY: Từ chối/Ngắt cuộc gọi và ĐÓNG POPUP
            if (_btnReject != null)
            {
                _btnReject.Click += (s, e) =>
                {
                    try
                    {
                        EndCall(); // Ngắt/Từ chối cuộc gọi
                    }
                    catch (Exception ex)
                    {
                        VnbisAgent.Common.LogWriter.WriteLine("Lỗi ngắt cuộc gọi: " + ex.Message);
                    }
                    finally
                    {
                        Close(); // Từ chối cuộc gọi xong thì đóng Popup
                    }
                };
            }

            // NÚT THOÁT: Tắt Popup thủ công bất cứ lúc nào
            if (_btnClose != null)
            {
                _btnClose.Click += (s, e) =>
                {
                    Close(); // Đóng Popup thủ công bất cứ lúc nào
                };
            }

#pragma warning disable CA1416
            // 10. Đẩy cửa sổ hiển thị đè lên màn hình Android
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                // Ép cửa sổ nhận diện tiêu điểm hệ thống cao nhất trên màn hình khóa bảo mật
                _layoutParam.Flags |= global::Android.Views.WindowManagerFlags.KeepScreenOn;
            }
#pragma warning restore CA1416
            
            _windowManager.AddView(_overlayView, _layoutParam);
            _isShowing = true;

            // 11. SỬA LỖI: Gọi hàm Close() chuẩn để tự đóng Popup sau 1 phút
            new global::Android.OS.Handler(global::Android.OS.Looper.MainLooper).PostDelayed(() =>
            {
                Close();
            }, 60000);
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("OverlayManager Show Error: " + ex.ToString());
        }
    }

    /// <summary>
    /// Đóng và giải phóng Popup Overlay
    /// </summary>
    public void Close()
    {
        try
        {
            if (_isShowing && _windowManager != null && _overlayView != null && _overlayView.IsAttachedToWindow)
            {
                _windowManager.RemoveView(_overlayView);                
            }
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("OverlayManager Close Error: " + ex.Message);
        }
        finally
        {
            _lblPhone = null;
            _lstInfo = null;
            _btnAnswer = null;
            _btnReject = null;
            _btnClose = null;

            _overlayView?.Dispose(); // Thêm dấu ? để tránh NullReferenceException
            _overlayView = null;
            _isShowing = false;
        }
    }

    /// <summary>
    /// Kiểm tra xem ứng dụng có đang hiển thị trên màn hình (Foreground) hay không
    /// </summary>
    private static bool IsAppInForeground()
    {
        try
        {
            var appProcessInfo = new ActivityManager.RunningAppProcessInfo();
            ActivityManager.GetMyMemoryState(appProcessInfo);
            return appProcessInfo.Importance == Importance.Foreground;
        }
        catch
        {
            return false;
        }        
    }

    /// <summary>
    /// Lệnh Kích hoạt Nghe cuộc gọi (Yêu cầu Android 8.0+)
    /// </summary>
    private void AnswerCall()
    {
        try
        {
            var telecomManager = (TelecomManager)_context.GetSystemService(Context.TelecomService);
            if (telecomManager != null && Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                telecomManager.AcceptRingingCall();
            }
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("Lỗi không thể trả lời cuộc gọi: " + ex.Message);
        }
    }

    /// <summary>
    /// Lệnh Kích hoạt Ngắt/Từ chối cuộc gọi (Yêu cầu Android 9.0+)
    /// </summary>
    private void EndCall()
    {
        try
        {
            var telecomManager = (TelecomManager)_context.GetSystemService(Context.TelecomService);
            if (telecomManager != null)
            {
                // Đối với Android 9.0+ (API 28+)
                if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
                {
                    telecomManager.EndCall();
                }
                else
                {
                    // Fallback cho Android cũ hơn (Sử dụng Reflection gọi ITelephony)
                    try
                    {
                        var telephonyManager = (global::Android.Telephony.TelephonyManager)_context.GetSystemService(Context.TelephonyService);
                        var classTelephony = Java.Lang.Class.ForName(telephonyManager.Class.Name);
                        var methodGetITelephony = classTelephony.GetDeclaredMethod("getITelephony");
                        methodGetITelephony.Accessible = true;
                        var iTelephony = methodGetITelephony.Invoke(telephonyManager);
                        var classITelephony = Java.Lang.Class.ForName(iTelephony.Class.Name);
                        var methodEndCall = classITelephony.GetDeclaredMethod("endCall");
                        methodEndCall.Invoke(iTelephony);
                    }
                    catch (Exception exOld)
                    {
                        VnbisAgent.Common.LogWriter.WriteLine("EndCall Reflection Error: " + exOld.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("Lỗi ngắt cuộc gọi: " + ex.Message);
        }
    }       
}


//// 3. GÁN SỰ KIỆN TƯƠNG TÁC CHO CÁC NÚT BẤM LỆNH TRÊN POPUP
//if (btnClose != null)
//{
//    btnClose.Click += (s, e) =>
//    {
//        //try
//        //{
//        //    // 1. Thu thập dữ liệu từ các điều khiển giao diện hiện tại trên cửa sổ
//        //    string phone = lblPhone?.Text ?? "Không rõ số";
//        //    string customer = lblCustomer?.Text ?? "Khách vãng lai";
//        //    string debt = lblDebt?.Text ?? "0 đ";
//        //    string note = txtNote?.Text ?? "";

//        //    // 2. Định dạng chuỗi văn bản gửi đi sạch sẽ và chuyên nghiệp
//        //    string shareContent = $"[THÔNG TIN CUỘC GỌI VNBIS]\n" +
//        //                          $"- {phone}\n" +
//        //                          $"- {customer}\n" +
//        //                          $"- {debt}\n" +
//        //                          $"- Ghi chú nghiệp vụ: {note}";

//        //    // 3. Khởi tạo Android Intent với hành động SEND (Gửi dữ liệu)
//        //    global::Android.Content.Intent shareIntent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionSend);

//        //    // Thiết lập loại dữ liệu là văn bản thuần túy (siêu nhẹ, không tốn dữ liệu mạng)
//        //    shareIntent.SetType("text/plain");

//        //    // Nhồi chuỗi văn bản đã dựng vào Intent
//        //    shareIntent.PutExtra(global::Android.Content.Intent.ExtraText, shareContent);

//        //    // 4. RẤT QUAN TRỌNG: Vì hàm này chạy trong cấu trúc vẽ đè hệ thống (Service/Overlay Context),
//        //    // bạn phải thêm cờ NewTask thì Android mới cho phép bật một ứng dụng khác (Zalo) lên từ nền ngầm.
//        //    //shareIntent.AddFlags(global::Android.Content.Intent.FlagsActivityNewTask);
//        //    shareIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);

//        //    // Chỉ định rõ ràng mở bảng lựa chọn ứng dụng của Android
//        //    //global::Android.Content.Intent chooserIntent = global::Android.Content.Intent.CreateChooser(shareIntent, "Chia sẻ thông tin qua Zalo");
//        //    global::Android.Content.Intent chooserIntent = global::Android.Content.Intent.CreateChooser(shareIntent, "Chia sẻ qua Zalo");
//        //    //chooserIntent.AddFlags(global::Android.Content.Intent.FlagsActivityNewTask);
//        //    chooserIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);

//        //    // Kích hoạt mở luồng hệ thống
//        //    _context.StartActivity(chooserIntent);
//        //}
//        //catch (Exception ex)
//        //{
//        //    VnbisAgent.Common.LogWriter.WriteLine("Lỗi chia sẻ thông tin cuộc gọi: " + ex.Message);
//        //}

//        // Tùy chọn: Đóng cửa sổ
//        Close();
//    };

//}