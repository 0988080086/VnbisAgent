using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Telecom;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Data;

namespace VnbisAgent.Platforms.Android;
public class OverlayManager
{
    private readonly Context _context;
    private global::Android.Views.IWindowManager? _windowManager;
    private global::Android.Views.View? _overlayView;
    private WindowManagerLayoutParams? _layoutParam;
    private bool _isShowing = false;

    // [THÊM MỚI] Handler để đưa các tác vụ UI về Main Thread chống crash
    private readonly global::Android.OS.Handler _mainHandler;
    // [THÊM MỚI] Biến lưu tác vụ hẹn giờ đóng để có thể hủy khi bấm nút Đóng
    private Action? _autoCloseAction;

    // Khai báo các Control trên giao diện UI
    private global::Android.Widget.TextView? _lblPhone;
    private global::Android.Widget.ListView? _lstInfo;
    private global::Android.Widget.Button? _btnAnswer;
    private global::Android.Widget.Button? _btnReject;
    private global::Android.Widget.Button? _btnShare;
    private global::Android.Widget.Button? _btnClose;

    // Khởi tạo nhận Context từ Service truyền qua
    public OverlayManager(Context context)
    {
        _context = context;
        // [THÊM MỚI] Khởi tạo Handler liên kết với Looper của Main Thread
        _mainHandler = new global::Android.OS.Handler(global::Android.OS.Looper.MainLooper);
    }

    /// <summary>
    /// Hiển thị Popup Overlay cuộc gọi
    /// </summary>
    /// <param name="incomingNumber">Số điện thoại cuộc gọi đến</param>
    /// <param name="dtCustomerData">DataTable chứa 2 cột ("TieuDe", "NoiDung")</param>
    public void Show(string incomingNumber, DataTable dtData)
    {
        // [SỬA TẠI ĐÂY] Bọc toàn bộ hàm Show vào Main Thread để tránh lỗi Thread trên MAUI
        _mainHandler.Post(() =>
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

                // 2. Lấy kích thước thực tế toàn màn hình chuẩn xác
                var (screenWidth, screenHeight) = GetScreenSize();
                if (screenWidth == 0 || screenHeight == 0) return;

                // 3. Kiểm tra thiết bị có đang ở Màn hình Khóa hay không
                bool isKeyguardLocked = IsScreenLocked();

                // 4. Bơm (Inflate) View từ XML
                //LayoutInflater? inflater = LayoutInflater.From(_context);
                //if (inflater == null) return;
                // [SỬA TẠI ĐÂY] Dùng ContextThemeWrapper để giao diện trên Service không bị mất Theme/Màu
                var themeContext = new ContextThemeWrapper(_context, global::Android.Resource.Style.ThemeDeviceDefaultLight);
                LayoutInflater? inflater = LayoutInflater.From(themeContext);
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

                // 6. Phân chia vị trí dựa trên trạng thái màn hình
                if (isKeyguardLocked)
                {
                    // TRƯỜNG HỢP 1: Màn hình đang KHÓA (Cuộc gọi hệ thống là FULLSCREEN) 
                    // Đặt Popup ở chính giữa màn hình, chiều cao tự co giãn theo nội dung
                    _layoutParam.Gravity = global::Android.Views.GravityFlags.Center;
                    _layoutParam.Y = 0;
                    _layoutParam.Height = global::Android.Views.WindowManagerLayoutParams.WrapContent;
                }
                else
                {
                    // TRƯỜNG HỢP 2: Màn hình đang MỞ (Khả năng cao có Call Banner 1/4 phía trên)
                    // Đẩy Popup xuống dưới mốc 25% để né Banner cuộc gọi của hệ thống
                    _layoutParam.Gravity = GravityFlags.Top | GravityFlags.CenterHorizontal;
                    long _TopMarginPecent = VnbisAgent.Common.AppData.TopMarginPecent;          //Tỷ lệ lùi phía trên
                    long _ButomMarginPecent = VnbisAgent.Common.AppData.ButtonMarginPecent;     //Tỷ lệ dâng cao phía dưới

                    if (_TopMarginPecent > 0 || _ButomMarginPecent > 0)
                    {
                        // Né 25% phía trên
                        _layoutParam.Y = (int)(screenHeight * _TopMarginPecent / 100);
                        // Giới hạn chiều cao tối đa khoảng 60%-65% màn hình để không bị tràn mép dưới
                        _layoutParam.Height = (int)(screenHeight * (100 - _TopMarginPecent - _ButomMarginPecent));
                    }
                    else
                    {
                        //Nếu không cấu hình cách trên, dưới, thì cứ để 100% màn hình
                        _layoutParam.Gravity = global::Android.Views.GravityFlags.Center;
                        _layoutParam.Y = 0;
                        _layoutParam.Height = global::Android.Views.WindowManagerLayoutParams.WrapContent;
                    }
                }

                // 7. Ánh xạ các Controls từ file XML
                _lblPhone = _overlayView.FindViewById<global::Android.Widget.TextView>(VnbisAgent.Resource.Id.lblPhone);
                _lstInfo = _overlayView.FindViewById<global::Android.Widget.ListView>(VnbisAgent.Resource.Id.lstInfo);
                _btnAnswer = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnAnswer);
                _btnReject = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnReject);
                _btnShare = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnShare);
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
                // NÚT NGHE: Trả lời cuộc gọi
                if (_btnAnswer != null)
                {
                    _btnAnswer.Click += (s, e) =>
                    {
                        AnswerCall();                                                       //Báo trả lời cuộc gọi
                        if (_btnAnswer != null) _btnAnswer.Visibility = ViewStates.Gone;    //Ẩn nút Nghe
                        if (_btnReject != null) _btnReject.Visibility = ViewStates.Gone;    //Ản nút Huỷ
                    };
                }

                // NÚT HỦY: Từ chối/Ngắt cuộc gọi
                if (_btnReject != null)
                {
                    _btnReject.Click += (s, e) =>
                    {
                        try
                        {
                            EndCall();                                                      // Ngắt/Từ chối cuộc gọi
                        }
                        catch (Exception ex)
                        {
                            VnbisAgent.Common.LogWriter.WriteLine("Lỗi ngắt cuộc gọi: " + ex.Message);
                        }
                        finally
                        {
                            Close();
                        }
                    };
                }
                // NÚT Chia sẻ: Gửi thông tin khách trên Popup sang Zalo
                if (_btnShare != null)
                {
                    _btnShare.Click += (s, e) =>
                    {
                        ShareInfo();                                                            //Chỉ đóng Popup
                    };
                }
                // NÚT THOÁT: Tắt Popup thủ công bất cứ lúc nào
                if (_btnClose != null)
                {
                    _btnClose.Click += (s, e) =>
                    {
                        Close();                                                            //Chỉ đóng Popup
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

                // 11. Add View vào WindowManager
                _windowManager.AddView(_overlayView, _layoutParam);
                _isShowing = true;

                // 12. Hẹn giờ đóng Popup sau 60s
                //new global::Android.OS.Handler(global::Android.OS.Looper.MainLooper).PostDelayed(() => { Close(); }, 60000);
                _autoCloseAction = () => { Close(); };
                _mainHandler.PostDelayed(_autoCloseAction, 60000);
            }
            catch (Exception ex)
            {
                VnbisAgent.Common.LogWriter.WriteLine("OverlayManager Show Error: " + ex.ToString());
            }
        });        
    }
    private bool IsScreenLocked()
    {
        try
        {
            var keyguardManager = _context.GetSystemService(Context.KeyguardService)?.JavaCast<KeyguardManager>();
            return keyguardManager != null && keyguardManager.IsKeyguardLocked;
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("IsScreenLocked Error: " + ex.Message);
            return false;
        }
    }
    private (int Width, int Height) GetScreenSize()
    {
        try
        {
            var windowManager = _context.GetSystemService(Context.WindowService)?.JavaCast<IWindowManager>();
            if (windowManager == null) return (0, 0);

            // Trường hợp Android 11 (API level 30) trở lên
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                var metrics = windowManager.CurrentWindowMetrics;
                var bounds = metrics.Bounds;
                return (bounds.Width(), bounds.Height());
            }
            // Trường hợp Android 10 (API level 29) trở xuống
            else
            {
                var displayMetrics = new DisplayMetrics();

                // Dùng GetRealMetrics để lấy kích thước vật lý thực tế của màn hình 
                // (bao gồm cả vùng Tai thỏ, Status Bar và Navigation Bar)
#pragma warning disable CS0618 // Tắt cảnh báo Obsolete trên Android cũ
                windowManager.DefaultDisplay?.GetRealMetrics(displayMetrics);
#pragma warning restore CS0618

                return (displayMetrics.WidthPixels, displayMetrics.HeightPixels);
            }
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("GetScreenSize Error: " + ex.Message);
            return (0, 0);
        }
    }
    public void ShareInfo()
    {
        // [SỬA TẠI ĐÂY] Ép chạy trên Main Thread để không bị crash khi bật Intent Share
        _mainHandler.Post(() =>
        {
            try
            {
                // 1. Kiểm tra an toàn xem ListView và Bộ nạp Adapter có dữ liệu thực tế hay không
                if (_lstInfo == null || _lstInfo.Adapter == null)
                {
                    VnbisAgent.Common.LogWriter.WriteLine("ShareInfo thất bại: ListView hoặc Adapter đang trống.");
                    return;
                }

                // 2. ÉP KIỂU NGƯỢC: Lấy lại DataTable gốc nằm ẩn trong Adapter giao diện
                if (_lstInfo.Adapter is OverlayData dataAdapter)
                {
                    System.Data.DataTable currentTable = dataAdapter.ViewData;

                    if (currentTable == null || currentTable.Rows.Count == 0)
                    {
                        VnbisAgent.Common.LogWriter.WriteLine("ShareInfo: DataTable rỗng, không có gì để chia sẻ.");
                        return;
                    }

                    // 3. ĐÓNG GÓI DỮ LIỆU THÀNH CHỮ THUẦN TÚY (Dùng StringBuilder để tối ưu bộ nhớ)
                    System.Text.StringBuilder shareBuilder = new System.Text.StringBuilder();
                    shareBuilder.AppendLine("[THÔNG TIN KHÁCH HÀNG VNBIS]");

                    // Lấy nhanh số điện thoại đang hiển thị trên nhãn tiêu đề
                    string currentPhone = _lblPhone?.Text ?? "Chưa rõ số";
                    shareBuilder.AppendLine($"- {currentPhone}");
                    shareBuilder.AppendLine("---------------------------");

                    // Duyệt qua từng dòng trong DataTable thu hồi được để nối chuỗi văn bản dạng: "Tiêu đề: Nội dung"
                    foreach (DataRow row in currentTable.Rows)
                    {
                        string tieuDe = row["TieuDe"]?.ToString() ?? "";
                        string noiDung = row["NoiDung"]?.ToString() ?? "";

                        if (!string.IsNullOrEmpty(tieuDe) || !string.IsNullOrEmpty(noiDung))
                        {
                            shareBuilder.AppendLine($"- {tieuDe}: {noiDung}");
                        }
                    }

                    string finalShareContent = shareBuilder.ToString();
                    VnbisAgent.Common.LogWriter.WriteLine("Nội dung text chuẩn bị gửi sang Zalo:\n" + finalShareContent);

                    // 4. KÍCH HOẠT ANDROID INTENT CHIA SẺ SANG ZALO (Đúng chuẩn sạch lỗi hệ thống)
                    global::Android.Content.Intent shareIntent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionSend);
                    shareIntent.SetType("text/plain"); // Định dạng văn bản siêu nhẹ, truyền đi chớp nhoáng
                    shareIntent.PutExtra(global::Android.Content.Intent.ExtraText, finalShareContent);

                    // Thêm cờ bắt buộc vì đang chạy từ bối cảnh dịch vụ ngầm (Service Window Context)
                    shareIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);

                    // Tạo bảng chọn ứng dụng của Android hệ thống
                    global::Android.Content.Intent chooserIntent = global::Android.Content.Intent.CreateChooser(shareIntent, "Chia sẻ thông tin cuộc gọi");
                    chooserIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);

                    // Bật phanh màn hình chọn ứng dụng (Zalo) lên trước mặt người dùng
                    _context.StartActivity(chooserIntent);
                }
                else
                {
                    VnbisAgent.Common.LogWriter.WriteLine("ShareInfo lỗi: Adapter của ListView không phải là PopupDataAdapter.");
                }
            }
            catch (Exception ex)
            {
                VnbisAgent.Common.LogWriter.WriteLine("Lỗi thực thi trong hàm ShareInfo: " + ex.Message);
            }
        });
    }
    /// <summary>
    /// Đóng và giải phóng Popup Overlay
    /// </summary>
    public void Close()
    {
        // [SỬA TẠI ĐÂY] Ép chạy trên Main Thread
        _mainHandler.Post(() =>
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
                _btnShare = null;
                _btnClose = null;

                _overlayView?.Dispose(); // Thêm dấu ? để tránh NullReferenceException
                _overlayView = null;
                _isShowing = false;
            }
        });
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