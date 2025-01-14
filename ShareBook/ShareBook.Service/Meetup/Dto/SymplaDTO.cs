﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace ShareBook.Service.Dto
{
    public class SymplaDto
    {
        public string Status { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public List<SymplaEvent> Data { get; set; }
    }

    public class SymplaEvent
    {
        public int Id { get; set; }

        [JsonProperty("start_date")]
        public string StartDate { get; set; }

        public string Name { get; set; }
        public string Detail { get; set; }
        public string Image { get; set; }
        public string Url { get; set; }
    }
}