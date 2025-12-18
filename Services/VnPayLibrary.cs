using System.Net;
using System.Text;
using System.Security.Cryptography;

namespace N16_MilkTea.Services
{
    public class VnPayLibrary
    {
        public const string VERSION = "2.1.0";
        private SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _requestData.Add(key, value);
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value)) _responseData.Add(key, value);
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out string retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            StringBuilder data = new StringBuilder();
            
            // CHỖ NÀY QUAN TRỌNG: Sắp xếp theo thứ tự bảng chữ cái để tạo chuỗi Hash đúng chuẩn
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (data.Length > 0) data.Append("&");
                data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value));
            }
            
            string queryString = data.ToString();
            
            // VNPAY Sandbox đôi khi yêu cầu mã hóa dấu cách thành %20, nhưng WebUtility.UrlEncode lại ra dấu +
            // Fix thủ công để đảm bảo đúng chuẩn RFC 3986 nếu WebUtility không chạy
            // Tuy nhiên, Code 70 thường do sai Key nhiều hơn sai Encoding. 
            // Hãy cứ dùng WebUtility.UrlEncode trước, nếu vẫn lỗi thì đổi sang Uri.EscapeDataString
            
            string vnp_SecureHash = Utils.HmacSHA512(vnp_HashSecret, queryString);
            string paymentUrl = baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;
            return paymentUrl;
        }

        // ... Các hàm ValidateSignature, GetResponseData giữ nguyên hoặc dùng bản cũ ...
        // Để code gọn, mình viết lại phần Utils và Comparer ở dưới luôn cho đồng bộ
        
        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseData();
            string myChecksum = Utils.HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            StringBuilder data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType")) _responseData.Remove("vnp_SecureHashType");
            if (_responseData.ContainsKey("vnp_SecureHash")) _responseData.Remove("vnp_SecureHash");

            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (data.Length > 0) data.Append("&");
                data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value));
            }
            return data.ToString();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.Compare(x, y, StringComparison.Ordinal);
        }
    }

    public class Utils
    {
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
}