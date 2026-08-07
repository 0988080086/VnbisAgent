using Android.App;
using Android.Content;
using Android.Net;
using Android.Runtime;
using Android.Telecom;
using Microsoft.Extensions.Logging;

namespace VnbisAgent.Platforms.Android;

[Service(
    Permission = "android.permission.BIND_SCREENING_SERVICE",
    Exported = true,
    Enabled = true)]
[IntentFilter(new[] { "android.telecom.CallScreeningService" })]
public class CallScreeningEx : CallScreeningService
{
    public override void OnScreenCall(Call.Details callDetails)
    {
        if (callDetails == null) return;
        DateTime _now = DateTime.Now;
        // Cấu hình bộ phản hồi mặc định (Cho phép đổ chuông bình thường)
        var responseBuilder = new CallResponse.Builder();
        responseBuilder.SetDisallowCall(false);
        responseBuilder.SetRejectCall(false);
        responseBuilder.SetSkipCallLog(false);
        responseBuilder.SetSkipNotification(false);

        try
        {   
            // GIẢI PHÁP ĐỘT PHÁ: Sử dụng JNIEnv để ép máy Samsung gọi trực tiếp phương thức "getHandle" nguyên bản của Java.
            // Giải pháp này bỏ qua hoàn toàn thuộc tính nhập nhằng .Handle của C#
            IntPtr classRef = JNIEnv.GetObjectClass(callDetails.Handle);
            IntPtr methodId = JNIEnv.GetMethodID(classRef, "getHandle", "()Landroid/net/Uri;");
            IntPtr uriNativeResult = JNIEnv.CallObjectMethod(callDetails.Handle, methodId);

            if (uriNativeResult != IntPtr.Zero)
            {
                // Ép con trỏ ô nhớ JNI về lớp Android.Net.Uri chuẩn của hệ thống
                var androidUri = global::Java.Lang.Object.GetObject<global::Android.Net.Uri>(uriNativeResult, JniHandleOwnership.DoNotTransfer);

                if (androidUri != null)
                {
                    // Lấy chuỗi dữ liệu gốc (Ví dụ trả về: "tel:0988080086")
                    string rawUri = androidUri.ToString() ?? "";
                    string incomingNumber = "";

                    if (!string.IsNullOrEmpty(rawUri) && rawUri.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
                    {
                        incomingNumber = rawUri.Substring(4); // Loại bỏ tiền tố "tel:"
                    }
                    else
                    {
                        incomingNumber = rawUri;
                    }

                    // Log này CHẮC CHẮN sẽ in ra đầy đủ số điện thoại nối phía sau!
                    if (!string.IsNullOrEmpty(incomingNumber))
                    {
                        VnbisAgent.Common.CallEventItem _EventIn;
                        _EventIn = new VnbisAgent.Common.CallEventItem();
                        _EventIn.Ngay = _now;
                        _EventIn.BatDau = _now.ToString("HH:mm:ss");
                        _EventIn.KetThuc = _now.ToString("HH:mm:ss");
                        _EventIn.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Incoming;
                        _EventIn.Source = VnbisAgent.Common.SourceEnum.CallScreening;
                        _EventIn.CallLogID = 0;
                        _EventIn.CallerId = incomingNumber;
                        _EventIn.UID = VnbisAgent.Common.AppData.DeviceId + ((DateTimeOffset)_now).ToUnixTimeSeconds();
                        _EventIn.Huong = VnbisAgent.Common.HuongEnum.In; //Chỉ có thể bắt cuộc gọi đến
                        VnbisAgent.Common.CallManager.Instance.PushEvent(_EventIn);
                    }
                }                
            }            
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("CallScreeningEx: " + ex.ToString());
        }

        // Bắt buộc phải phản hồi lại cho tổng đài viễn thông hệ thống
        RespondToCall(callDetails, responseBuilder.Build());
    }
}