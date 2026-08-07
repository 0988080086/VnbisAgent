using System;
using System.Collections.Generic;

namespace VnbisAgent.Common;
public enum CallDirection
{
    Unknown = 0,

    Incoming = 1,

    Outgoing = 2,

    Missed = 3,

    Rejected = 4,

    Blocked = 5,

    Offhook = 6,

    Idle = 7
}

/// <summary>Quản lý danh sách cuộc gọi (Đọc từ nhật ký cuộc gọi của điện thoại)</summary>
public class CallLogManager
{
    /// <summary>Biến lưu trữ nhật ký cuộc gọi</summary>
    private readonly List<CallLogItem> mCallLogList;

    public CallLogManager()
    {
        mCallLogList = new List<CallLogItem>();
    }

    /// <summary>Danh sách chỉ đọc.</summary>
    public IReadOnlyList<CallLogItem> Logs
    {
        get
        {
            return mCallLogList.AsReadOnly();
        }
    }

    /// <summary>Tìm theo CallId</summary>
    private CallLogItem? FindId(long callId)
    {
        if (callId <= 0)
            return null;

        foreach (CallLogItem log in mCallLogList)
        {
            if (log.CallId == callId)
                return log;
        }

        return null;
    }

    /// <summary>Thêm mới</summary>
    private CallLogItem InsertLog(CallLogItem log)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        CallLogItem? old = FindId(log.CallId);

        if (old != null)
            return old;

        log.LastUpdate = DateTime.UtcNow;

        mCallLogList.Add(log);

        return log;
    }

    /// <summary>Cập nhật</summary>
    private bool UpdateLog(CallLogItem newLog)
    {
        if (newLog == null)
            return false;

        CallLogItem? old = FindId(newLog.CallId);

        if (old == null)
            return false;

        old.StartTime = newLog.StartTime;
        old.EndTime = newLog.EndTime;
        old.DurationSeconds = newLog.DurationSeconds;

        old.Direction = newLog.Direction;
        old.Completed = newLog.Completed;

        old.CallerId = newLog.CallerId;
        old.ContactName = newLog.ContactName;
        old.DisplayName = newLog.DisplayName;

        old.SimId = newLog.SimId;
        old.SimName = newLog.SimName;

        old.Synced = newLog.Synced;

        //old.RawDate = newLog.RawDate;

        old.LastUpdate = DateTime.UtcNow;

        return true;
    }

    /// <summary>Thêm mới hoặc cập nhật. Đây sẽ là hàm được gọi nhiều nhất</summary>
    public CallLogItem UpdateInSertLog(CallLogItem log)
    {
        CallLogItem? old = FindId(log.CallId);

        if (old == null)
            return InsertLog(log);

        UpdateLog(log);

        return old;
    }

    /// <summary>Xóa theo CallId</summary>
    private bool RemoveLog(long callId)
    {
        CallLogItem? log = FindId(callId);

        if (log == null)
            return false;

        mCallLogList.Remove(log);

        return true;
    }

    /// <summary>Xóa toàn bộ</summary>
    private void Clear()
    {
        mCallLogList.Clear();
    }
}

/// <summary>Thông tin một bản ghi cuộc gọi</summary>
public class CallLogItem
{
    /// <summary>CallId do Hệ điều hành cung cấp (Android: CallLog.Calls._ID, Windows: TAPI/CallHistory ID, iOS: CallKit ID</summary>
    public long CallId { get; set; }
    //public long RawDate { get; set; }
    /// <summary>Thời điểm bắt đầu cuộc gọi</summary>
    public DateTime StartTime { get; set; }
    /// <summary>Thời điểm kết thúc</summary>
    public DateTime EndTime { get; set; }
    /// <summary>Thời lượng (giây)</summary>
    public int DurationSeconds { get; set; }
    /// <summary>Chiều cuộc gọi</summary>
    public CallDirection Direction { get; set; }
    /// <summary>Đã hoàn thành chưa, cuộc gọi đã kết thúc chưa</summary>
    public bool Completed { get; set; }
    /// <summary>Số điện thoại</summary>
    public string CallerId { get; set; }
    /// <summary>Tên trong danh bạ điện thoại</summary>
    public string ContactName { get; set; }
    /// <summary>Tên hiển thị</summary>
    public string DisplayName { get; set; }
    /// <summary>ID SIM</summary>
    public string SimId { get; set; }
    /// <summary>Tên SIM</summary>
    public string SimName { get; set; }
    /// <summary>Đã đồng bộ Server chưa</summary>
    public bool Synced { get; set; }
    /// <summary>Lần cập nhật cuối</summary>
    public DateTime LastUpdate { get; set; }
    public CallLogItem()
    {
        CallId = 0;
        //RawDate = 0;
        CallerId = "";
        ContactName = "";
        DisplayName = "";
        SimId = "";
        SimName = "";
        StartTime = DateTime.MinValue;
        EndTime = DateTime.MinValue;
        DurationSeconds = 0;
        Direction = CallDirection.Unknown;
        Completed = false;
        Synced = false;
        LastUpdate = DateTime.UtcNow;
    }
    public override string ToString()
    {
        return
            CallId + " | " +
            Direction + " | " +
            CallerId + " | " +
            ContactName + " | " +
            DurationSeconds + "s";
    }
}