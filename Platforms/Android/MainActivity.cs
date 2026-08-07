using Android.App;
using Android.App.Roles;
using Android.Content;
using Android.Content.PM;
//using Android.DeviceLock;
using Android.OS;
//using Javax.Security.Auth;
using VnbisAgent.Platforms.Android;
//using Android.Provider;

namespace VnbisAgent
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
                        
            //Bước 1: Lấy mã thiết bị deviceId, và gán vào biến toàn cục AppData.DeviceId
            string? deviceId;
            deviceId = Android.Provider.Settings.Secure.GetString(ContentResolver,Android.Provider.Settings.Secure.AndroidId);            
            if (deviceId == null)
            {
                deviceId = "";
            }
            VnbisAgent.Common.AppData.DeviceId = deviceId;

            //Bước 2: Quyết định nhận điện thoại theo CallScreening nếu đã cấp quyền            
            bool Chk = IsCallScreeningRoleGranted();
            if (Chk == true) 
            { 
                VnbisAgent.Common.AppData.IsCallScreeningEnabled = true;
            }
            else
            {
                VnbisAgent.Common.AppData.IsCallScreeningEnabled = false;
            }

                //Bước 3: Xin quyền
#if ANDROID
                global::Android.App.Activity? _CurrActivity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (_CurrActivity != null)
            {
                // Chạy bất đồng bộ an toàn trên UI Thread để tránh xung đột giao diện
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await VnbisAgent.Platforms.Android.PermissionRequest.RequestAllPermissionsAsync(_CurrActivity);
                });
            }
#endif
            //Bước 4: Khai báo AgentService với Android, và bảo nó Khởi động dịch vụ chạy ngầm AgentService
            Intent serviceIntent;
            serviceIntent = new Intent(this, typeof(AgentService));
            StartForegroundService(serviceIntent);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            // Chuyển tiếp kết quả xử lý sang Class phân quyền của bạn
            Platforms.Android.PermissionRequest.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        protected override void OnActivityResult(int requestCode, Result resultCode, global::Android.Content.Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            // Chuyển tiếp sự kiện quay lại màn hình vào class xử lý quyền tuần tự
            VnbisAgent.Platforms.Android.PermissionRequest.OnActivityResult(requestCode, resultCode, data, this);
        }
        public bool IsCallScreeningRoleGranted()
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
            {
                var roleManager = (RoleManager)Android.App.Application.Context.GetSystemService(Context.RoleService);
                return roleManager != null && roleManager.IsRoleHeld(RoleManager.RoleCallScreening);
            }

            // Trả về true hoặc xử lý riêng nếu chạy trên các bản Android cũ dưới API 29
            return false;
        }
    }    
}