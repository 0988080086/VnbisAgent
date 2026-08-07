using Android.Content;
using Android.Views;
using Android.Runtime;
using Android.Widget;
using System;

namespace VnbisAgent.Platforms.Android;
public class OverlayManager
{
    private readonly Context _context; // Đổi thành Context chung
    private global::Android.Views.IWindowManager? _windowManager;
    private global::Android.Views.View? _overlayView;
    private WindowManagerLayoutParams? _layoutParam;
    private bool _isShowing = false;

    // Các biến UI điều khiển giữ nguyên của bạn
    private global::Android.Widget.TextView? lblPhone;
    private global::Android.Widget.TextView? lblCustomer;
    private global::Android.Widget.TextView? lblDebt;
    private global::Android.Widget.EditText? txtNote;
    private global::Android.Widget.Button? btnAnswer; // Hết lỗi CS0104
    private global::Android.Widget.Button? btnCancel;
    private global::Android.Widget.Button? btnClose;

    // Khởi tạo nhận Context từ Service truyền qua
    public OverlayManager(Context context)
    {
        _context = context;
    }

    // NÂNG CẤP HÀM: Nhận thêm biến thông tin công nợ từ hệ thống chuyển qua
    public void Show(string incomingNumber = "Chưa rõ số", string customerName = "Khách hàng lạ", string debtInfo = "0 đ")
    {
        if (_isShowing) return;

        if (!global::Android.Provider.Settings.CanDrawOverlays(_context)) return;
        VnbisAgent.Common.LogWriter.WriteLine("OverlayManager.Show");

        try
        {
            // Sửa lỗi ép kiểu Java.Lang.Object bằng JavaCast như bài trước
            var serviceObj = _context.GetSystemService(Context.WindowService);
            if (serviceObj == null) return;
            _windowManager = serviceObj.JavaCast<global::Android.Views.IWindowManager>();
            if (_windowManager == null) return;

            LayoutInflater? inflater = LayoutInflater.From(_context);
            if (inflater == null) return;

            _overlayView = inflater.Inflate(VnbisAgent.Resource.Layout.phone_call_overlay, null);
            if (_overlayView == null) return;

#pragma warning disable CA1416
            //_layoutParam = new global::Android.Views.WindowManagerLayoutParams(
            //    global::Android.Views.WindowManagerLayoutParams.MatchParent,
            //    global::Android.Views.WindowManagerLayoutParams.MatchParent,
            //    global::Android.Views.WindowManagerTypes.ApplicationOverlay,
            //    global::Android.Views.WindowManagerFlags.NotTouchModal | global::Android.Views.WindowManagerFlags.LayoutInScreen,
            //    global::Android.Graphics.Format.Opaque);    // SỬA TẠI ĐÂY: Đổi Translucent thành Opaque. Trong file xml đổi android:background="#FFFFFF"
            _layoutParam = new global::Android.Views.WindowManagerLayoutParams(
                global::Android.Views.WindowManagerLayoutParams.MatchParent,
                global::Android.Views.WindowManagerLayoutParams.MatchParent,
                global::Android.Views.WindowManagerTypes.ApplicationOverlay, // Bắt buộc cho Service chạy ngầm
                global::Android.Views.WindowManagerFlags.NotTouchModal |
                global::Android.Views.WindowManagerFlags.LayoutInScreen |
                global::Android.Views.WindowManagerFlags.ShowWhenLocked | // CỜ MỚI: Ép đè lên màn hình khóa
                global::Android.Views.WindowManagerFlags.TurnScreenOn |   // CỜ MỚI: Tự động bật sáng màn hình khi chuông reo
                global::Android.Views.WindowManagerFlags.DismissKeyguard,  // CỜ MỚI: Đẩy lùi màn hình bảo mật sang phía sau
                global::Android.Graphics.Format.Opaque); // Hoặc Translucent tùy giao diện của bạn
#pragma warning restore CA1416

            _layoutParam.Gravity = global::Android.Views.GravityFlags.Top;

            // 1. THỰC HIỆN ÁNH XẠ ĐẦY ĐỦ 100% CÁC ID TỪ FILE XML SANG C#
            lblPhone = _overlayView.FindViewById<global::Android.Widget.TextView>(VnbisAgent.Resource.Id.lblPhone);
            lblCustomer = _overlayView.FindViewById<global::Android.Widget.TextView>(VnbisAgent.Resource.Id.lblCustomer);
            lblDebt = _overlayView.FindViewById<global::Android.Widget.TextView>(VnbisAgent.Resource.Id.lblDebt);
            //pnlBusiness = _overlayView.FindViewById<global::Android.Widget.LinearLayout>(VnbisAgent.Resource.Id.pnlBusiness);
            txtNote = _overlayView.FindViewById<global::Android.Widget.EditText>(VnbisAgent.Resource.Id.txtNote);
            btnAnswer = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnAnswer);
            btnCancel = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnCancel);
            btnClose = _overlayView.FindViewById<global::Android.Widget.Button>(VnbisAgent.Resource.Id.btnClose);

            // 2. ĐỔ DỮ LIỆU ĐỘNG VÀO CÁC NHÃN TEXT
            if (lblPhone != null) lblPhone.Text = "Số điện thoại: " + incomingNumber;
            if (lblCustomer != null) lblCustomer.Text = "Tên khách hàng: " + customerName;
            if (lblDebt != null) lblDebt.Text = "Tổng công nợ: " + debtInfo;
            if (txtNote != null) txtNote.Text = "Nội dung: " + "Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - Không có gì - ";

            // 3. GÁN SỰ KIỆN TƯƠNG TÁC CHO CÁC NÚT BẤM LỆNH TRÊN POPUP
            if (btnClose != null)
            {
                btnClose.Click += (s, e) =>
                {
                    try
                    {
                        // 1. Thu thập dữ liệu từ các điều khiển giao diện hiện tại trên cửa sổ
                        string phone = lblPhone?.Text ?? "Không rõ số";
                        string customer = lblCustomer?.Text ?? "Khách vãng lai";
                        string debt = lblDebt?.Text ?? "0 đ";
                        string note = txtNote?.Text ?? "";

                        // 2. Định dạng chuỗi văn bản gửi đi sạch sẽ và chuyên nghiệp
                        string shareContent = $"[THÔNG TIN CUỘC GỌI VNBIS]\n" +
                                              $"- {phone}\n" +
                                              $"- {customer}\n" +
                                              $"- {debt}\n" +
                                              $"- Ghi chú nghiệp vụ: {note}";

                        // 3. Khởi tạo Android Intent với hành động SEND (Gửi dữ liệu)
                        global::Android.Content.Intent shareIntent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionSend);

                        // Thiết lập loại dữ liệu là văn bản thuần túy (siêu nhẹ, không tốn dữ liệu mạng)
                        shareIntent.SetType("text/plain");

                        // Nhồi chuỗi văn bản đã dựng vào Intent
                        shareIntent.PutExtra(global::Android.Content.Intent.ExtraText, shareContent);

                        // 4. RẤT QUAN TRỌNG: Vì hàm này chạy trong cấu trúc vẽ đè hệ thống (Service/Overlay Context),
                        // bạn phải thêm cờ NewTask thì Android mới cho phép bật một ứng dụng khác (Zalo) lên từ nền ngầm.
                        //shareIntent.AddFlags(global::Android.Content.Intent.FlagsActivityNewTask);
                        shareIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);

                        // Chỉ định rõ ràng mở bảng lựa chọn ứng dụng của Android
                        //global::Android.Content.Intent chooserIntent = global::Android.Content.Intent.CreateChooser(shareIntent, "Chia sẻ thông tin qua Zalo");
                        global::Android.Content.Intent chooserIntent = global::Android.Content.Intent.CreateChooser(shareIntent, "Chia sẻ qua Zalo");
                        //chooserIntent.AddFlags(global::Android.Content.Intent.FlagsActivityNewTask);
                        chooserIntent.AddFlags(global::Android.Content.ActivityFlags.NewTask);

                        // Kích hoạt mở luồng hệ thống
                        _context.StartActivity(chooserIntent);
                    }
                    catch (Exception ex)
                    {
                        VnbisAgent.Common.LogWriter.WriteLine("Lỗi chia sẻ thông tin cuộc gọi: " + ex.Message);
                    }
                    // Tùy chọn: Sau khi bấm chia sẻ thì thực hiện đóng popup cửa sổ lơ lửng luôn
                    Close();
                };
            }


            if (btnCancel != null)
            {
                btnCancel.Click += (s, e) => {
                    //VnbisAgent.Common.LogWriter.WriteLine("Người dùng chọn Huỷ/Từ chối cuộc gọi.");
                    // Bạn có thể chèn thêm lệnh dập máy Android Native tại đây
                    Close(); // Đóng popup sau khi huỷ
                };
            }

            if (btnAnswer != null)
            {
                btnAnswer.Click += (s, e) => {
                    //VnbisAgent.Common.LogWriter.WriteLine("Người dùng chọn Chấp nhận nghe cuộc gọi.");
                    // Bạn có thể chèn thêm lệnh mở loa ngoài hoặc bắt máy Telecom tại đây
                };
            }

            // 4. ĐẨY CỬA SỔ HIỂN THỊ LÊN MÀN HÌNH ĐIỆN THOẠI
#pragma warning disable CA1416
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                // Ép cửa sổ nhận diện tiêu điểm hệ thống cao nhất trên màn hình khóa bảo mật
                _layoutParam.Flags |= global::Android.Views.WindowManagerFlags.KeepScreenOn;
            }
#pragma warning restore CA1416
            _windowManager.AddView(_overlayView, _layoutParam);
            _isShowing = true;
            //VnbisAgent.Common.LogWriter.WriteLine("OverlayManager: Đã ánh xạ đủ và hiển thị popup.");
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("OverlayManager ShowLayout: " + ex.ToString());
        }
    }
    
    public void Close()
    {
        try
        {
            if (_windowManager != null && _overlayView != null)
            {
                // Thu thập nội dung text người dùng gõ trong ô Ghi chú trước khi đóng
                if (txtNote != null && !string.IsNullOrEmpty(txtNote.Text))
                {
                    //VnbisAgent.Common.LogWriter.WriteLine("Nội dung ghi chú cuộc gọi thu thập được: " + txtNote.Text);
                    // Thực thi lưu txtNote.Text vào DB hoặc file Log của bạn trước khi hủy View
                }

                _windowManager.RemoveView(_overlayView);
                _overlayView.Dispose();
                _overlayView = null;
            }

            // Giải phóng sạch các ô nhớ tránh rò rỉ RAM chạy ngầm trên máy Samsung
            lblPhone = null; lblCustomer = null; lblDebt = null;
            txtNote = null; //pnlBusiness = null;
            btnAnswer = null; btnCancel = null; btnClose = null;
            _isShowing = false;
        }
        catch { }
    }
}