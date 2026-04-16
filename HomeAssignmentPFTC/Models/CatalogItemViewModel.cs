namespace HomeAssignmentPFTC.Models;

public class CatalogItemViewModel
{
    public string RestaurantId { get; set; } = "";
    public string MenuId { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string DisplayPrice { get; set; } = "";
    
    //Hidden Numerical Value for sorting
    public decimal NumericPrice { get; set; }
}