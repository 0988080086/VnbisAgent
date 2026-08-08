using System.Data;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace VnbisAgent.Platforms.Android;

public class OverlayData : BaseAdapter
{
    private readonly Context _context;
    private readonly DataTable _table;

    public OverlayData(Context context, DataTable table)
    {
        _context = context;
        _table = table;
    }

    public override int Count => _table?.Rows.Count ?? 0;

    public override Java.Lang.Object GetItem(int position) => null;

    public override long GetItemId(int position) => position;

    public override global::Android.Views.View GetView(int position, global::Android.Views.View convertView, ViewGroup parent)
    {
        var view = convertView ?? LayoutInflater.From(_context).Inflate(Resource.Layout.item_info_row, parent, false);

        var lblTitle = view.FindViewById<TextView>(Resource.Id.lblTitle);
        var lblContent = view.FindViewById<TextView>(Resource.Id.lblContent);

        if (_table != null && position < _table.Rows.Count)
        {
            DataRow row = _table.Rows[position];

            // Đọc tên cột "TieuDe" và "NoiDung" từ DataTable
            string title = row["TieuDe"]?.ToString() ?? "";
            string content = row["NoiDung"]?.ToString() ?? "";

            lblTitle.Text = title.EndsWith(":") ? title : title + ":";
            lblContent.Text = content;
        }

        return view;
    }
    public System.Data.DataTable ViewData
    {
        get { return _table; }
    }
}
