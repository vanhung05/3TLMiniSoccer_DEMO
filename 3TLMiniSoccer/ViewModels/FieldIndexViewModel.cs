using _3TLMiniSoccer.Models;

namespace _3TLMiniSoccer.ViewModels
{
    public class FieldIndexViewModel
    {
        public List<Field> Fields { get; set; } = new List<Field>();
        public List<FieldType> FieldTypes { get; set; } = new List<FieldType>();
        public int? SelectedFieldTypeId { get; set; }
        public DateTime? SelectedDate { get; set; }
    }
}
