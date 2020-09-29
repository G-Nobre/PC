using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication1 {
    /**
 * Type that representes a JSON response
 */
    public class Response {
        public int Status { get; set; }
        public Dictionary<String, String> Headers { get; set; }
        public JObject Payload { get; set; }
    }
}