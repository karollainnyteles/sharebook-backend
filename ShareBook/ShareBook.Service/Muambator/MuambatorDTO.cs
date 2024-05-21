using System.Collections.Generic;

namespace ShareBook.Service.Muambator
{
    public class MuambatorDto
    {
        public string Status { get; set; }

        public string Message { get; set; }

        public IList<dynamic> Results { get; set; }
    }
}