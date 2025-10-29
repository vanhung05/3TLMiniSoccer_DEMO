using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Field> Fields { get; set; } = new List<Field>();
        public List<Product> FeaturedProducts { get; set; } = new List<Product>();
        public int TotalBookings { get; set; }
        public int TotalFields { get; set; }
    }
}
