using Android.App;
using Android.App.Roles;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Platform;

namespace VnbisAgent.Platforms.Android;

public static class PermissionRequest
{
    
    //TaskCompletionSource cho các quyền
    private static TaskCompletionSource<bool>? _phonePermissionTcs;         //READ_PHONE_STATE
    private static TaskCompletionSource<bool>? _callLogPermissionTcs;       //READ_CALL_LOG
    private static TaskCompletionSource<bool>? _contactsPermissionTcs;      //READ_CONTACTS
    private static TaskCompletionSource<bool>? _batteryPermissionTcs;       //BatteryOptimization
    private static TaskCompletionSource<bool>? _overlayPermissionTcs;       //Overlay
    private static TaskCompletionSource<bool>? _callScreeningPermissionTcs; //CallScreening

    //Mã định danh cho các dạng Quyền
    public const int RequestPhoneCode = 1001;           //READ_PHONE_STATE
    public const int RequestCallLogCode = 1002;         //READ_CALL_LOG
    public const int RequestContactsCode = 1003;        //READ_CONTACTS
    public const int RequestBatteryCode = 1004;         //Battery
    public const int RequestOverlayCode = 1005;         //Overlay    
    private const int RequestCallScreeningCode = 2002;  //CallScreening


    //ALL. Request Queue async/await (Gọi lần lượt)
    public static async Task RequestAllPermissionsAsync(Activity activity)
    {
        // 1. Xin quyền Điện thoại
        bool phoneGranted = await RequestPhonePermissionAsync(activity);

        // 2. Xin quyền Nhật ký cuộc gọi
        bool callLogGranted = await RequestCallLogPermissionAsync(activity);
        
        // 3. Xin quyền đọc danh bạ
        bool contactsGranted = await RequestContactsPermissionAsync(activity);

        // 4. Xin quyền Bỏ tối ưu pin
        bool _batteryPermissionTcs=await RequestIgnoreBatteryOptimization(activity);

        // 5. XỬ LÝ MỚI: Xin quyền hiển thị trên ứng dụng khác (Đợi người dùng bật xong quay lại mới chạy tiếp)
        bool overlayGranted = await RequestOverlayPermissionAsync(activity);

        //6. CallScreening        
        if (VnbisAgent.Common.AppData.IsCallScreeningEnabled == false)
        {
            bool screeningGranted = await RequestCallScreeningAsync(activity);
            VnbisAgent.Common.AppData.IsCallScreeningEnabled = screeningGranted;
        }
    }

    //1. Request READ_PHONE_STATE
    public static Task<bool> RequestPhonePermissionAsync(Activity activity)
    {
        if (ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.ReadPhoneState) == Permission.Granted)
        {
            return Task.FromResult(true);
        }

        _phonePermissionTcs = new TaskCompletionSource<bool>();

        ActivityCompat.RequestPermissions(
            activity,
            new string[] { global::Android.Manifest.Permission.ReadPhoneState },
            RequestPhoneCode);

        return _phonePermissionTcs.Task;
    }

    //2. Request  READ_CALL_LOG
    public static Task<bool> RequestCallLogPermissionAsync(Activity activity)
    {
        if (ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.ReadCallLog) == Permission.Granted)
        {
            return Task.FromResult(true);
        }

        _callLogPermissionTcs = new TaskCompletionSource<bool>();

        ActivityCompat.RequestPermissions(
            activity,
            new string[] { global::Android.Manifest.Permission.ReadCallLog },
            RequestCallLogCode);

        return _callLogPermissionTcs.Task;
    }

    // 3. Request READ_CONTACTS
    public static Task<bool> RequestContactsPermissionAsync(Activity activity)
    {
        // Nếu điện thoại đã được cấp quyền danh bạ từ trước rồi thì bỏ qua và trả về true luôn
        if (ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.ReadContacts) == Permission.Granted)
        {
            return Task.FromResult(true);
        }
        _contactsPermissionTcs = new TaskCompletionSource<bool>();
        // Bật hộp thoại xin quyền chuẩn Android hệ thống lên màn hình
        ActivityCompat.RequestPermissions(
            activity,
            new string[] { global::Android.Manifest.Permission.ReadContacts },
            RequestContactsCode);

        return _contactsPermissionTcs.Task;
    }

    //4. Request BatteryOptimization
    public static Task<bool> RequestIgnoreBatteryOptimization(Activity activity)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M)
        {
            return Task.FromResult(true);
        }

        PowerManager? pm = activity.GetSystemService(Context.PowerService) as PowerManager;

        // Nếu thiết bị Samsung đã được bỏ tối ưu pin rồi thì trả về true và đi tiếp
        if (pm == null || pm.IsIgnoringBatteryOptimizations(activity.PackageName))
        {
            return Task.FromResult(true);
        }

        _batteryPermissionTcs = new TaskCompletionSource<bool>();

        global::Android.Content.Intent intent = new global::Android.Content.Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
        intent.SetData(global::Android.Net.Uri.Parse("package:" + activity.PackageName));

        // Ép Android phải trả kết quả về thông qua OnActivityResult kèm mã RequestBatteryCode
        activity.StartActivityForResult(intent, RequestBatteryCode);

        return _batteryPermissionTcs.Task;
    }

    //5. Request Overlay
    public static Task<bool> RequestOverlayPermissionAsync(Activity activity)
    {
        // Nếu đã có quyền rồi thì bỏ qua và trả về true ngay lập tức
        if (global::Android.Provider.Settings.CanDrawOverlays(activity))
        {
            return Task.FromResult(true);
        }

        _overlayPermissionTcs = new TaskCompletionSource<bool>();

        global::Android.Content.Intent intent = new global::Android.Content.Intent(
            global::Android.Provider.Settings.ActionManageOverlayPermission,
            global::Android.Net.Uri.Parse("package:" + activity.PackageName)
        );

        // Sử dụng StartActivityForResult để có thể bắt được sự kiện khi họ tắt màn hình cài đặt quay về app
        activity.StartActivityForResult(intent, RequestOverlayCode);

        return _overlayPermissionTcs.Task;
    }

    //6. Request CallScreening
    public static Task<bool> RequestCallScreeningAsync(Activity activity)
    {   
        if (Build.VERSION.SdkInt < BuildVersionCodes.Q)
            return Task.FromResult(false);
        RoleManager? roleManager =
            activity.GetSystemService(Java.Lang.Class.FromType(typeof(RoleManager)))
            as RoleManager;
        if (roleManager == null)
            return Task.FromResult(false);
        if (roleManager.IsRoleHeld(RoleManager.RoleCallScreening))
        {
            return Task.FromResult(true);
        }
        _callScreeningPermissionTcs = new TaskCompletionSource<bool>();
#pragma warning disable CS0618
        activity.StartActivityForResult(roleManager.CreateRequestRoleIntent(RoleManager.RoleCallScreening),RequestCallScreeningCode);
#pragma warning restore CS0618        
        return _callScreeningPermissionTcs.Task;
    }

    //OnRequest CALL_LOGS After READ_PHONE_STATE
    public static void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        bool isGranted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;

        if (requestCode == RequestPhoneCode)
        {
            _phonePermissionTcs?.TrySetResult(isGranted);
        }
        else if (requestCode == RequestCallLogCode)
        {
            _callLogPermissionTcs?.TrySetResult(isGranted);
            if (isGranted)
            {
#if ANDROID
                // Phát tín hiệu thông điệp trực tiếp sang AgentService yêu cầu quét và nạp mảng dữ liệu ngay lập tức
                var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
                var refreshIntent = new global::Android.Content.Intent(context, typeof(global::VnbisAgent.Platforms.Android.AgentService));
                refreshIntent.SetAction("ACTION_REFRESH_CALL_LOGS");
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                    context.StartForegroundService(refreshIntent);
                else
                    context.StartService(refreshIntent);
#endif
            }
        }
        else if (requestCode == RequestContactsCode)
        {
            _contactsPermissionTcs?.TrySetResult(isGranted);
        }
    }

    //OnActivity CanDrawOverlays
    public static void OnActivityResult(int requestCode, Result resultCode, global::Android.Content.Intent? data, Activity activity)
    {
        //OnActivityResult for CanDrawOverlays
        if (requestCode == RequestOverlayCode)
        {
            // Kiểm tra lại xem sau khi quay lại app, người dùng đã thực sự bật On chưa
            bool isGranted = global::Android.Provider.Settings.CanDrawOverlays(activity);
            _overlayPermissionTcs?.TrySetResult(isGranted);
        }
        else if (requestCode == RequestBatteryCode)
        {
            PowerManager? pm = activity.GetSystemService(Context.PowerService) as PowerManager;
            bool isIgnoring = pm != null && pm.IsIgnoringBatteryOptimizations(activity.PackageName);

            _batteryPermissionTcs?.TrySetResult(isIgnoring);
        }
        //OnActivityResult for CallScreening
        if (requestCode == RequestCallScreeningCode)
        {
            bool granted = false;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                RoleManager? roleManager = activity.GetSystemService(Java.Lang.Class.FromType(typeof(RoleManager))) as RoleManager;

                if (roleManager != null)
                    granted = roleManager.IsRoleHeld(RoleManager.RoleCallScreening);
            }            
            _callScreeningPermissionTcs?.TrySetResult(granted);
            return;
        }
    }
}