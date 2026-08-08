using Android.App;
using Android.App.Roles;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using AndroidX.Core.App;
using AndroidX.Core.Content;
//using System.Threading.Tasks;
//using static Microsoft.Maui.ApplicationModel.Platform;

namespace VnbisAgent.Platforms.Android;

public static class PermissionRequest
{
    // TaskCompletionSource cho các quyền
    private static TaskCompletionSource<bool>? _phonePermissionTcs;             // READ_PHONE_STATE
    private static TaskCompletionSource<bool>? _callLogPermissionTcs;           // READ_CALL_LOG
    private static TaskCompletionSource<bool>? _contactsPermissionTcs;          // READ_CONTACTS
    private static TaskCompletionSource<bool>? _answerPhoneCallsPermissionTcs;  // ANSWER_PHONE_CALLS
    private static TaskCompletionSource<bool>? _callPhonePermissionTcs;         // CALL_PHONE
    private static TaskCompletionSource<bool>? _batteryPermissionTcs;           // BatteryOptimization
    private static TaskCompletionSource<bool>? _overlayPermissionTcs;           // Overlay
    private static TaskCompletionSource<bool>? _callScreeningPermissionTcs;     // CallScreening

    // Mã định danh cho các dạng Quyền
    public const int RequestPhoneCode = 1001;               // READ_PHONE_STATE
    public const int RequestCallLogCode = 1002;             // READ_CALL_LOG
    public const int RequestContactsCode = 1003;            // READ_CONTACTS
    public const int RequestAnswerPhoneCallsCode = 1004;    // ANSWER_PHONE_CALLS
    public const int RequestCallPhoneCode = 1005;           // CALL_PHONE
    public const int RequestBatteryCode = 1006;             // Battery
    public const int RequestOverlayCode = 1007;             // Overlay    
    private const int RequestCallScreeningCode = 2002;      // CallScreening

    // ALL. Request Queue async/await (Gọi xin quyền lần lượt)
    public static async Task RequestAllPermissionsAsync(Activity activity)
    {
        // 1. Xin quyền Trạng thái điện thoại (READ_PHONE_STATE)
        bool phoneGranted = await RequestPhonePermissionAsync(activity);

        // 2. Xin quyền Nhật ký cuộc gọi (READ_CALL_LOG)
        bool callLogGranted = await RequestCallLogPermissionAsync(activity);

        // 3. Xin quyền đọc Danh bạ (READ_CONTACTS)
        bool contactsGranted = await RequestContactsPermissionAsync(activity);

        // 4. Xin quyền Nghe cuộc gọi (ANSWER_PHONE_CALLS)
        bool answerCallsGranted = await RequestAnswerPhoneCallsPermissionAsync(activity);

        // 5. Xin quyền Thực hiện/Ngắt cuộc gọi (CALL_PHONE)
        bool callPhoneGranted = await RequestCallPhonePermissionAsync(activity);

        // 6. Xin quyền Bỏ tối ưu pin
        bool batteryGranted = await RequestIgnoreBatteryOptimization(activity);

        // 7. Xin quyền hiển thị trên ứng dụng khác (Overlay)
        bool overlayGranted = await RequestOverlayPermissionAsync(activity);

        // 8. Xin quyền CallScreening Role
        if (VnbisAgent.Common.AppData.IsCallScreeningEnabled == false)
        {
            bool screeningGranted = await RequestCallScreeningAsync(activity);
            VnbisAgent.Common.AppData.IsCallScreeningEnabled = screeningGranted;
        }
    }

    // 1. Request READ_PHONE_STATE
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

    // 2. Request READ_CALL_LOG
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
        if (ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.ReadContacts) == Permission.Granted)
        {
            return Task.FromResult(true);
        }

        _contactsPermissionTcs = new TaskCompletionSource<bool>();

        ActivityCompat.RequestPermissions(
            activity,
            new string[] { global::Android.Manifest.Permission.ReadContacts },
            RequestContactsCode);

        return _contactsPermissionTcs.Task;
    }

    // 4. Request ANSWER_PHONE_CALLS (Quyền trả lời cuộc gọi)
    public static Task<bool> RequestAnswerPhoneCallsPermissionAsync(Activity activity)
    {
        // Quyền này chỉ hỗ trợ từ Android 8.0 (API level 26) trở lên
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return Task.FromResult(true);
        }

        if (ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.AnswerPhoneCalls) == Permission.Granted)
        {
            return Task.FromResult(true);
        }

        _answerPhoneCallsPermissionTcs = new TaskCompletionSource<bool>();

        ActivityCompat.RequestPermissions(
            activity,
            new string[] { global::Android.Manifest.Permission.AnswerPhoneCalls },
            RequestAnswerPhoneCallsCode);

        return _answerPhoneCallsPermissionTcs.Task;
    }

    // 5. Request CALL_PHONE (Quyền ngắt / thực hiện cuộc gọi)
    public static Task<bool> RequestCallPhonePermissionAsync(Activity activity)
    {
        if (ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.CallPhone) == Permission.Granted)
        {
            return Task.FromResult(true);
        }

        _callPhonePermissionTcs = new TaskCompletionSource<bool>();

        ActivityCompat.RequestPermissions(
            activity,
            new string[] { global::Android.Manifest.Permission.CallPhone },
            RequestCallPhoneCode);

        return _callPhonePermissionTcs.Task;
    }

    // 6. Request BatteryOptimization
    public static Task<bool> RequestIgnoreBatteryOptimization(Activity activity)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M)
        {
            return Task.FromResult(true);
        }

        PowerManager? pm = activity.GetSystemService(Context.PowerService) as PowerManager;

        if (pm == null || pm.IsIgnoringBatteryOptimizations(activity.PackageName))
        {
            return Task.FromResult(true);
        }

        _batteryPermissionTcs = new TaskCompletionSource<bool>();

        global::Android.Content.Intent intent = new global::Android.Content.Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
        intent.SetData(global::Android.Net.Uri.Parse("package:" + activity.PackageName));

        activity.StartActivityForResult(intent, RequestBatteryCode);

        return _batteryPermissionTcs.Task;
    }

    // 7. Request Overlay
    public static Task<bool> RequestOverlayPermissionAsync(Activity activity)
    {
        if (global::Android.Provider.Settings.CanDrawOverlays(activity))
        {
            return Task.FromResult(true);
        }

        _overlayPermissionTcs = new TaskCompletionSource<bool>();

        global::Android.Content.Intent intent = new global::Android.Content.Intent(
            global::Android.Provider.Settings.ActionManageOverlayPermission,
            global::Android.Net.Uri.Parse("package:" + activity.PackageName)
        );

        activity.StartActivityForResult(intent, RequestOverlayCode);

        return _overlayPermissionTcs.Task;
    }

    // 8. Request CallScreening
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
        activity.StartActivityForResult(roleManager.CreateRequestRoleIntent(RoleManager.RoleCallScreening), RequestCallScreeningCode);
#pragma warning restore CS0618        

        return _callScreeningPermissionTcs.Task;
    }

    // Lắng nghe kết quả từ dialog cấp quyền hệ thống (ActivityCompat.RequestPermissions)
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
        else if (requestCode == RequestAnswerPhoneCallsCode)
        {
            _answerPhoneCallsPermissionTcs?.TrySetResult(isGranted);
        }
        else if (requestCode == RequestCallPhoneCode)
        {
            _callPhonePermissionTcs?.TrySetResult(isGranted);
        }
    }

    // Lắng nghe kết quả từ các Intent cài đặt hệ thống (StartActivityForResult)
    public static void OnActivityResult(int requestCode, Result resultCode, global::Android.Content.Intent? data, Activity activity)
    {
        if (requestCode == RequestOverlayCode)
        {
            bool isGranted = global::Android.Provider.Settings.CanDrawOverlays(activity);
            _overlayPermissionTcs?.TrySetResult(isGranted);
        }
        else if (requestCode == RequestBatteryCode)
        {
            PowerManager? pm = activity.GetSystemService(Context.PowerService) as PowerManager;
            bool isIgnoring = pm != null && pm.IsIgnoringBatteryOptimizations(activity.PackageName);

            _batteryPermissionTcs?.TrySetResult(isIgnoring);
        }
        else if (requestCode == RequestCallScreeningCode)
        {
            bool granted = false;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                RoleManager? roleManager = activity.GetSystemService(Java.Lang.Class.FromType(typeof(RoleManager))) as RoleManager;

                if (roleManager != null)
                    granted = roleManager.IsRoleHeld(RoleManager.RoleCallScreening);
            }
            _callScreeningPermissionTcs?.TrySetResult(granted);
        }
    }
}