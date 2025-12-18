namespace N16_MilkTea.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        
        // --- THÊM DÒNG NÀY ---
        public int MaSize { get; set; } 
        
        public double Total => Price * Quantity;
    }
}