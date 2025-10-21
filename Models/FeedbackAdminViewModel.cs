using System.Collections.Generic;

namespace QOS.Models
{
    /// <summary>
    /// ViewModel cho trang Admin quản lý feedbacks
    /// </summary>
    public class FeedbackAdminViewModel
    {
        /// <summary>
        /// Danh sách feedbacks
        /// </summary>
        public List<ContactViewModel> Feedbacks { get; set; }

        /// <summary>
        /// Trạng thái hiện tại đang filter (All, Pending, Resolved, Closed)
        /// </summary>
        public string? CurrentStatus { get; set; }

        /// <summary>
        /// Từ khóa tìm kiếm
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Thống kê tổng quan
        /// </summary>
        public FeedbackStatistics Statistics { get; set; }

        public FeedbackAdminViewModel()
        {
            Feedbacks = new List<ContactViewModel>();
            CurrentStatus = "All";
            SearchKeyword = "";
            Statistics = new FeedbackStatistics();
        }
    }

    /// <summary>
    /// Class chứa thống kê feedbacks
    /// </summary>
    public class FeedbackStatistics
    {
        /// <summary>
        /// Tổng số feedback
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Số feedback đang chờ xử lý
        /// </summary>
        public int Pending { get; set; }

        /// <summary>
        /// Số feedback đã giải quyết
        /// </summary>
        public int Resolved { get; set; }

        /// <summary>
        /// Số feedback đã đóng
        /// </summary>
        public int Closed { get; set; }

        /// <summary>
        /// Số feedback trong tuần này
        /// </summary>
        public int ThisWeek { get; set; }

        /// <summary>
        /// Số feedback trong tháng này
        /// </summary>
        public int ThisMonth { get; set; }
    }
}