using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CpfRegisterFunction.DTOs.Output
{
    public class GenericResponseDTO
    {
        public string UserId { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }
}
