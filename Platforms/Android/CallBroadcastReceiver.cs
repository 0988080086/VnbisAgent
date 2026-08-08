using Android.App;
using Android.Content;
using Android.Content.OM;
using Android.Locations;
using Android.Telephony;
using Microsoft.Extensions.Logging;
using VnbisAgent.Common;

namespace VnbisAgent.Platforms.Android.Services;

[BroadcastReceiver(Enabled = true,Exported = true)]
[IntentFilter(new string[]{TelephonyManager.ActionPhoneStateChanged, Intent.ActionNewOutgoingCall })]
public class CallBroadcastReceiver : BroadcastReceiver
{
    // Tạo sự kiện static báo trạng thái cuộc gọi kết thúc
    public static event Action? OnCallEnded;

    private static string savedNumber = "";
    // Chỉ chống nhiễu Android
    private static string _lastState = "";
    private static DateTime _lastTime = DateTime.MinValue;
    // Nếu cùng trạng thái lặp lại quá nhanh thì bỏ
    private const int DuplicateMilliseconds = 50;
    public override void OnReceive(Context? _context, Intent? _intent)
    {
        //Trạng thái cuộc gọi (RINGING, OFFHOOK, IDLE)
        //Chỉ báo ra: Cuộc gọi đi, và Kết thúc cuộc gọi
        //Cuộc gọi đến: Để ScreenCalling giải quyết
        if (_intent == null) return;
        DateTime _now = DateTime.Now;

        try
        {
            //TÌNH HUỐNG 1: ActionNewOutgoingCall (Chỉ sảy ra với gọi đi)
            if (_intent.Action == Intent.ActionNewOutgoingCall)
            {   
                // Lấy số điện thoại đang gọi đi qua khóa Intent.ExtraPhoneNumber
                string? outgoingNumber = _intent.GetStringExtra(Intent.ExtraPhoneNumber);
                if (!string.IsNullOrEmpty(outgoingNumber))
                {
                    savedNumber = outgoingNumber;
                    CallEventItem _EventOut;
                    _EventOut = new CallEventItem();                    
                    _EventOut.Ngay = _now;
                    _EventOut.BatDau = _now.ToString("HH:mm:ss");
                    _EventOut.KetThuc = _now.ToString("HH:mm:ss");
                    _EventOut.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Outgoing;
                    _EventOut.Source = VnbisAgent.Common.SourceEnum.BroadcastReceiver;
                    _EventOut.CallLogID = 0;
                    _EventOut.CallerId = outgoingNumber;
                    _EventOut.UID = VnbisAgent.Common.AppData.DeviceId + ((DateTimeOffset)_now).ToUnixTimeSeconds();
                    _EventOut.Huong = VnbisAgent.Common.HuongEnum.Out;

                    //Gọi đi thì luôn gửi, vì chỉ có BroadcastReceiver mới nhận ra sự kiện này
                    VnbisAgent.Common.CallManager.Instance.PushEvent(_EventOut);                    
                }
                return;
            }

            //TÌNH HUỐNG 2: ActionPhoneStateChanged (Có cả sự kiện phụ của gọi đến gọi đi)
            if (_intent.Action == TelephonyManager.ActionPhoneStateChanged)
            {
                string _state = "";
                object? obj = _intent.GetStringExtra(TelephonyManager.ExtraState);
                if (obj != null)
                {
                    _state = obj.ToString()!;
                }
                
                if (_state == TelephonyManager.ExtraStateRinging)
                {
                    //GỌI ĐẾN: Lấy số điện thoại từ ExtraIncomingNumber                    
                    string? incomingNumber = _intent.GetStringExtra(TelephonyManager.ExtraIncomingNumber);
                    if (!string.IsNullOrEmpty(incomingNumber))
                    {
                        savedNumber = incomingNumber;
                        CallEventItem _EventIn = new CallEventItem();
                        _EventIn.Ngay = _now;
                        _EventIn.BatDau = _now.ToString("HH:mm:ss");
                        _EventIn.KetThuc = _now.ToString("HH:mm:ss");
                        _EventIn.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Incoming;
                        _EventIn.Source = VnbisAgent.Common.SourceEnum.BroadcastReceiver;
                        _EventIn.CallLogID = 0;
                        _EventIn.CallerId = incomingNumber;
                        _EventIn.UID = VnbisAgent.Common.AppData.DeviceId + ((DateTimeOffset)_now).ToUnixTimeSeconds();
                        _EventIn.Huong = VnbisAgent.Common.HuongEnum.In;
                        
                        //GỌI ĐẾN Chỉ gửi khi CallScreening không đăng ký thành công
                        if ( VnbisAgent.Common.AppData.IsCallScreeningEnabled == false)
                        {
                            VnbisAgent.Common.CallManager.Instance.PushEvent(_EventIn);
                        }
                    }
                }
                else if (_state == TelephonyManager.ExtraStateOffhook)
                {
                    //NGHE MÁY (Không phân biệt được là từ gọi đến hay gọi đi)
                    CallEventItem _Offhook = new CallEventItem();
                    _Offhook.Ngay = _now;
                    _Offhook.BatDau = _now.ToString("HH:mm:ss");
                    _Offhook.KetThuc = _now.ToString("HH:mm:ss");
                    _Offhook.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Offhook;
                    _Offhook.Source = VnbisAgent.Common.SourceEnum.BroadcastReceiver;
                    _Offhook.CallLogID = 0;
                    _Offhook.CallerId = "";
                    _Offhook.UID = VnbisAgent.Common.AppData.DeviceId + ((DateTimeOffset)_now).ToUnixTimeSeconds();
                    _Offhook.Huong = VnbisAgent.Common.HuongEnum.Internal;

                    //Không phân biệt được nghe máy là ĐI hay ĐÊN, nên không gửi
                    //VnbisAgent.Common.CallManager.Instance.PushEvent(_Offhook);
                }
                else if (_state == TelephonyManager.ExtraStateIdle)
                {
                    CallEventItem _EventIn = new CallEventItem();
                    _EventIn.Ngay = _now;
                    _EventIn.BatDau=_now.ToString("HH:mm:ss");
                    _EventIn.KetThuc = _now.ToString("HH:mm:ss");
                    _EventIn.TinhTrang = VnbisAgent.Common.TinhTrangEnum.Idle;
                    _EventIn.Source = VnbisAgent.Common.SourceEnum.BroadcastReceiver;
                    _EventIn.CallLogID = 0;
                    _EventIn.CallerId = "";
                    _EventIn.UID = VnbisAgent.Common.AppData.DeviceId + ((DateTimeOffset)_now).ToUnixTimeSeconds();
                    _EventIn.Huong = VnbisAgent.Common.HuongEnum.Internal;

                    //Luôn gửi, vì chỉ có BroadcastReceiver mới nhận được sự kiện này
                    VnbisAgent.Common.CallManager.Instance.PushEvent(_EventIn);

                    //Gọi đóng cửa sổ Popup nếu tồn tại
                    // Bắn sự kiện ra ngoài
                    OnCallEnded?.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("BroadcastReceiver Error " + ex.ToString());
        }
    }
}