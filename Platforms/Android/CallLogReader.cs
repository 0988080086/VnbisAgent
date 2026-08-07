using Android.Content;

namespace VnbisAgent.Platforms.Android;
internal class CallLogReader
{
    private static long _ReadCount = 0;
    private readonly Context _context;
    public CallLogReader(Context context)
    {
        _context = context;
        _ReadCount = 0;
    }

    public System.Collections.Generic.List<global::VnbisAgent.Common.CallLogItem> ReadLogs()
    {
        var _callList = new System.Collections.Generic.List<global::VnbisAgent.Common.CallLogItem>();
        try
        {
            var _uri = global::Android.Provider.CallLog.Calls.ContentUri;
            long threeDaysAgo = (long)(DateTime.UtcNow.AddDays(-3) - new DateTime(1970, 1, 1)).TotalMilliseconds;
            string selection = "date>?";
            string[] selectionArgs =
                {
                    threeDaysAgo.ToString()
                };

            var cursor = _context.ContentResolver.Query(_uri, null, selection, selectionArgs, "_id DESC");
            if (cursor == null)
            {
                VnbisAgent.Common.LogWriter.WriteLine("ReadLogs Cursor = NULL");
                return _callList;
            }
            _ReadCount = _ReadCount + 1;

            int idIndex = cursor.GetColumnIndex("_id");
            int numberIndex = cursor.GetColumnIndex("number");
            int nameIndex = cursor.GetColumnIndex("name");
            int dateIndex = cursor.GetColumnIndex("date");
            // Kiểm tra sau khi đã khai báo
            if (idIndex < 0 || numberIndex < 0 || dateIndex < 0)
            {
                VnbisAgent.Common.LogWriter.WriteLine("ReadLogs: Thiếu cột _id");
                cursor.Close();
                return _callList;
            }

            int count = 0;
            long maxCallID =  VnbisAgent.Common.AppData.LastCallID;            

            while (cursor.MoveToNext() && count < 3)
            {
                long id = cursor.GetLong(idIndex);

                // Nếu đã đọc rồi thì bỏ qua
                if (id <= VnbisAgent.Common.AppData.LastCallID)
                {                    
                    break;
                }
                string number = cursor.GetString(numberIndex) ?? "";
                string? name = cursor.GetString(nameIndex) ?? "Khách vãng lai";
                long dateMills = cursor.GetLong(dateIndex);
                DateTime dateTime =new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(dateMills).ToLocalTime();
                var item = new global::VnbisAgent.Common.CallLogItem
                {
                    CallLogID = id,
                    CallerId = number,
                    DisplayName = name,
                    StartTime = dateTime
                };
                _callList.Add(item);

                if (id > maxCallID)
                    maxCallID = id;
                count++;
            }

            cursor.Close();

            // Cập nhật CallID lớn nhất đã đọc
            VnbisAgent.Common.AppData.LastCallID = maxCallID;
        }
        catch (Exception ex)
        {
            VnbisAgent.Common.LogWriter.WriteLine("CallLog lỗi đọc " + ex.ToString());
        }
        ////Cập nhật lần đọc đầu tiên, từ lần sau sẽ không cập nhật
        //if (_ReadCount <=  1)
        //{
        //    VnbisAgent.Common.LogWriter.WriteLine("Đọc lần 1");
        //    // Đưa vào CallManager
        //    if (_callList.Count > 1)
        //    {
        //        _callList.Reverse();
        //    }
        //    foreach (VnbisAgent.Common.CallLogItem item in _callList)
        //    {
        //        VnbisAgent.Common.CallManager.Instance.UpdateInsertLog(item);
        //    }
        //}
        //Trả về
        return _callList;   
    }    
}
