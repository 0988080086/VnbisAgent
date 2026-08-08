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