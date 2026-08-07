using VnbisAgent.Common;

namespace VnbisAgent.UI;
public partial class PhoneTestPage : ContentPage
{
    public PhoneTestPage()
    {
        InitializeComponent();

        //Đăng ký sự kiện SessionChanged
        CallManager.Instance.SessionChanged += CallManager_SessionChanged;

        //Hiển thị:
        ShowListCallLog();
    }
    //Sự kiện PhoneTestPage được mở lại
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await System.Threading.Tasks.Task.Delay(500);
        ShowListCallLog();
    }
    //Khi sự kiện SessionChanged báo tới, thì sẽ gọi lại ShowListCallLog
    private void CallManager_SessionChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ShowListCallLog();
        });
    }

    //Hiển thị lại nhật ký cuộc gọi
    private void ShowListCallLog()
    {
        try
        {
            List<VnbisAgent.Common.CallEventItem> _CallEventList;
            _CallEventList = VnbisAgent.Common.CallManager.Instance.CallEventGet.ToList();
            if (_CallEventList.Count > 0)
            {
                CollectionViewCallEvent.ItemsSource = _CallEventList.OrderByDescending(x => x.UID).ToList();                
            }
            else
            {
                CollectionViewCallEvent.ItemsSource = null;                
            }
        }        
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("ShowListCallLog: " + ex.Message);
        }
    }

    //Đọc lại nhật ký
    private void ButtonReadAgentLog_Clicked(object sender, EventArgs e)
    {
        // 1. Đảo ngược trạng thái hiển thị của EditorLog
        EditorLog.IsVisible = !EditorLog.IsVisible;

        // 2. Trạng thái CollectionView luôn luôn ngược lại với EditorLog
        CollectionViewCallEvent.IsVisible = !EditorLog.IsVisible;

        // 3. Thay đổi chữ hiển thị trên nút bấm tương thích theo trạng thái
        if (EditorLog.IsVisible)
        {
            ButtonReadAgentLog.Text = "Quay lại danh sách cuộc gọi";

            //Đọc toàn bộ file log lỗi
            EditorLog.Text = VnbisAgent.Common.LogWriter.ReadAll();
        }
        else
        {
            ButtonReadAgentLog.Text = "Xem File Log Lỗi";
        }
    }

    //Hiển thị Popup khi nhấn vào một item trong CollectionViewCallEvent
    private void CollectionViewCallEvent_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 1. Lấy phần tử được chọn từ CurrentSelection
        var selectedItem = e.CurrentSelection.FirstOrDefault();
        if (selectedItem == null)
            return;
        // 2. Bỏ chọn trên CollectionView để lần sau ấn lại vào đúng dòng đó vẫn kích hoạt lại sự kiện
        if (sender is CollectionView collectionView)
        {
            collectionView.SelectedItem = null;
        }
        // 3. Ép kiểu về Class Model dữ liệu của bạn (ví dụ: CallLogModel / CallItem)
        // Lấy ra thông tin như Số điện thoại, Tên người gọi,...
        if (selectedItem is CallEventItem callData)
        {
            // 4. Gọi hiển thị cửa sổ overlay
            VnbisAgent.Common.AppData.ShowPopupTel(callData.CallerId, callData.UID);
        }
    }
}

//// Kiểm tra điều kiện an toàn phòng trường hợp click rỗng
//if (e.CurrentSelection.Count == 0) return;

//// Ép kiểu dòng dữ liệu được chọn về đối tượng CallLogItem chuẩn trong dự án của bạn
//if (e.CurrentSelection[0] is global::VnbisAgent.Common.CallLogItem selectedLog)
//{
//    VnbisAgent.Common.AppData.ShowPopupTel(selectedLog.CallerId, selectedLog.DisplayName);
//}

//// Đưa trạng thái Item đã chọn về rỗng để lần sau người dùng có thể bấm lại cuộc gọi này
//((CollectionView)sender).SelectedItem = null;
//            // Ép kiểu dòng dữ liệu được chọn về đối tượng CallLogItem chuẩn trong dự án của bạn
//            if (e.CurrentSelection[0] is global::VnbisAgent.Common.CallLogItem selectedLog)
//        {
//#if ANDROID
//            // 1. Lấy Context hệ thống ngầm Android Native từ lõi MAUI
//            var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;

//            // 2. Tạo Intent kết nối trực tiếp đến dịch vụ chạy ngầm AgentService
//            var intent = new global::Android.Content.Intent(context, typeof(global::VnbisAgent.Platforms.Android.AgentService));

//            // 3. Đóng gói hành động hiển thị và nạp các trường thông tin cần thiết
//            intent.SetAction("ACTION_SHOW_OVERLAY");
//            intent.PutExtra("SELECTED_PHONE", selectedLog.CallerId);
//            intent.PutExtra("SELECTED_NAME", selectedLog.DisplayName);

//            // 4. Kích hoạt dịch vụ ngầm thực thi lệnh vẽ đè màn hình lơ lửng
//            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
//            {
//                context.StartForegroundService(intent);
//            }
//            else
//            {
//                context.StartService(intent);
//            }
//#endif
//        }