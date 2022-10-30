using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace IotCloudVoorbeeldExamen.Models
{
    public class Child
    {
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public Guid Id { get; set; }
        public string StudBookNumber { get; set; }
        public string ClassTag { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAdult { get; set; }

        public List<Meal> Meals { get; set; }
    }

    public class Meal
    {
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public Guid Id { get; set; }
        [JsonProperty(PropertyName = "Meal", NullValueHandling = NullValueHandling.Ignore)]
        public string MealCategory { get; set; }
        [JsonProperty(PropertyName = "Date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Date { get; set; }
    }
}
