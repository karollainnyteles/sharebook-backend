﻿using System;

namespace ShareBook.Service.AwsSqs.Dto
{
    public class NewBookBody
    {
        public Guid BookId { get; set; }
        public string BookTitle { get; set; }
        public Guid CategoryId { get; set; }
    }
}