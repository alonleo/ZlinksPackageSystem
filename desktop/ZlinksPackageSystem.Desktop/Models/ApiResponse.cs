using System.Collections.Generic;

namespace ZlinksPackageSystem.Desktop.Models
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    public class PageResponse<T>
    {
        public List<T> Records { get; set; } = new();
        public int Total { get; set; }
        public int Size { get; set; }
        public int Current { get; set; }
        public int Pages { get; set; }
    }
}