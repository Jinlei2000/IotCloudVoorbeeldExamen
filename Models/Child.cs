using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace IotCloudVoorbeeldExamen.Models
{
    public class Child
    {
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public Guid Id { get; set; }
        [JsonProperty(PropertyName = "StudBookNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string StudBookNumber { get; set; }
        [JsonProperty(PropertyName = "ClassTag", NullValueHandling = NullValueHandling.Ignore)]

        public string ClassTag { get; set; }
        [JsonProperty(PropertyName = "FirstName", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstName { get; set; }
        [JsonProperty(PropertyName = "LastName", NullValueHandling = NullValueHandling.Ignore)]
        public string LastName { get; set; }
        [JsonProperty(PropertyName = "EmailAdult", NullValueHandling = NullValueHandling.Ignore)]
        public string EmailAdult { get; set; }
        [JsonProperty(PropertyName = "Meals", NullValueHandling = NullValueHandling.Ignore)]
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
