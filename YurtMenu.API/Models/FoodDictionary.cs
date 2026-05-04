namespace YurtMenu.API.Models
{
    public class FoodDictionary
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int Calories { get; set; }

        public MealType MealType { get; set; } // 0: Sabah, 1: Akşam
    }

}
