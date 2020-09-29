using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mime;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication1 {
    public enum Method {
        CREATE,
        PUT,
        TRANSFER,
        TAKE,
        SHUTDOWN
    }

    public enum Status {
        Success = 200,
        Timeout = 204,
        BadRequest = 400,
        NotFound = 404,
        InvalidOperation = 405,
        InternalServerError = 500,
        NotAvailable = 503
    }


    public class HttpHelper {
        private Func<string, string> replacer = newMessage => Message.Replace("%s", newMessage);

        private static string Message = "{\n\tText: '%s'\n}";

        public bool VerifyMehtod(Request request, out Method method) =>
            Enum.TryParse(request.Method, out method);


        public Response OperationSuccessful(String message = "Operation Completed With Success!") =>
            CreateResponse(Status.Success, replacer(message));

        public Response InvalidOperation(Request request) =>
            CreateResponse(Status.InvalidOperation, replacer($"Method {request.Method} is not a valid operation."));

        public Response BadRequest(string message = "Invalid Json Request!") => CreateResponse(Status.BadRequest, replacer(message));

        public Response NotFoundForPath() =>
            CreateResponse(Status.NotFound, replacer($"There is no Queue for that Request!"));

        public Response TimeoutResponse() => CreateResponse(Status.Timeout, replacer("Timeout has occurred!"));


        public Response ErrorFromServer => CreateResponse(
            Status.InternalServerError,
            replacer("Internal Server Error")
        );

        public Response ServerShutdown => CreateResponse(
            Status.NotAvailable,
            replacer("Server is shutting down")
        );

        public Response ServerShutdownTimeout => CreateResponse(Status.Timeout,
            replacer("Server timed out on Shutdown.")
        );

        private Response CreateResponse(Status status, string message = "") {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("Agent", "json-server");
            headers.Add("Content-type", "application/json");
            headers.Add("Date", DateTime.Now.ToString(new CultureInfo("en-US")));

            JObject payload = default;
            if (!message.Equals(""))
                payload = JObject.Parse(message);

            return new Response {
                Status = (int) status,
                Payload = payload,
                Headers = headers
            };
        }
    }
}