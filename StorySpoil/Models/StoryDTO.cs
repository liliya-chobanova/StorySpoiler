using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace StorySpoiler.Models
{
    internal class StoryDTO
    {
        [JsonPropertyName("Title")]
        public string Title { get; set; }

        [JsonPropertyName("Description")]
        public string Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
