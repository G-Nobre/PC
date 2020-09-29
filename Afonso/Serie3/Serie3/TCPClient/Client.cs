using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ConsoleApplication1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TCPClient {
    /**
 * The type that represents a JSON request
 */
    public class Request {
        public String Method { get; set; }
        public Dictionary<String, String> Headers { get; set; }
        public JObject Payload { get; set; }

        public override String ToString() {
            return $"Method: {Method}, Headers: {Headers}, Payload: {Payload}";
        }
    }

    /**
 * The type that represents a JSON response
 */
    public class Response {
        public int Status { get; set; }
        public Dictionary<String, String> Headers { get; set; }
        public JObject Payload { get; set; }

        public override String ToString() {
            return $"Status: {Status}, Headers: {Headers}, Payload: {Payload}";
        }
    }

    /**
 * Represents the payload of the request message
 */
    public class RequestPayload {
        public int Number { get; set; }
        public String Text { get; set; }

        public override String ToString() {
            return $"[ Number: {Number}, Text: {Text} ]";
        }
    }


    internal class Client {
        private const int ServerPort = 8080;
        // private const string Address = "localhost";

        private static int _requestCount = 0;

        private static JsonSerializer _serializer = new JsonSerializer();
        // private static RequestsMaker _maker = new RequestsMaker();

        private static int REQS_PER_BATCH = 10;

        static async Task SendRequestAndReceiveResponseAsync(string server, RequestPayload payload) {
            /**
             * Create a TcpClient socket in order to connect to the echo server.
             */
            using (TcpClient connection = new TcpClient()) {
                try {
                    // Start a stop watch timer
                    Stopwatch sw = Stopwatch.StartNew();

                    // connect socket to the echo server.
                    await connection.ConnectAsync(server, ServerPort);

                    // Create and fill the Request with "payload" as Payload
                    Request request = new Request {
                        Method = $"echo-{payload.Number}",
                        Headers = new Dictionary<String, String>(),
                        Payload = (JObject) JToken.FromObject(payload),
                    };

                    // Add some headers for test purposes 
                    request.Headers.Add("agent", "json-client");
                    request.Headers.Add("timeout", "10000");

                    /**
                     * Translate the message to JSON and send it to the echo server.
                     */
                    JsonTextWriter writer = new JsonTextWriter(new StreamWriter(connection.GetStream()));
                    _serializer.Serialize(writer, request);
                    Console.WriteLine($"-->{payload}");
                    await writer.FlushAsync();

                    /**
                     * Receive the server's response and display it.
                     */
                    JsonTextReader reader = new JsonTextReader(new StreamReader(connection.GetStream())) {
                        // Configure reader to support reading multiple top-level objects
                        SupportMultipleContent = true
                    };
                    try {
                        // Consume any bytes until start of JSON object ('{')
                        do {
                            await reader.ReadAsync();
                        } while (reader.TokenType != JsonToken.StartObject &&
                                 reader.TokenType != JsonToken.None);

                        if (reader.TokenType == JsonToken.None) {
                            Console.WriteLine("***error: reached end of input stream, ending.");
                            return;
                        }

                        /**
                         * Read the response JSON object
                        */
                        JObject jresponse = await JObject.LoadAsync(reader);
                        sw.Stop();

                        /**
                         * Back to the .NET world
                         */
                        Response response = jresponse.ToObject<Response>();
                        request = response.Payload.ToObject<Request>();
                        RequestPayload recoveredPayload = request.Payload.ToObject<RequestPayload>();
                        Console.WriteLine($"<--{recoveredPayload.ToString()}, elapsed: {sw.ElapsedMilliseconds} ms");
                    }
                    catch (JsonReaderException jre) {
                        Console.WriteLine($"***error: error reading JSON: {jre.Message}");
                    }
                    catch (Exception e) {
                        Console.WriteLine($"-***error: exception: {e.Message}");
                    }

                    sw.Stop();
                    Interlocked.Increment(ref _requestCount);
                }
                catch (Exception ex) {
                    Console.WriteLine($"--***error:[{payload}] {ex.Message}");
                }
            }
        }


        public static async Task Main(string[] args) {
            bool executeOnce = false;
            string text = (args.Length > 0) ? args[0] : "--default text--";

            Task[] requests = new Task[REQS_PER_BATCH];
            Stopwatch sw = Stopwatch.StartNew();
            do {
                for (int i = 0; i < REQS_PER_BATCH; i++)
                    requests[i] =
                        SendRequestAndReceiveResponseAsync("localhost", new RequestPayload {Number = i, Text = text});
                await Task.WhenAll(requests);
            } while (!(executeOnce || Console.KeyAvailable));
            // await SendRequestAndReceiveResponseAsync("localhost", new RequestPayload {Number = 1, Text = text});
            
            Console.WriteLine($"--completed requests: {_requestCount} / {sw.ElapsedMilliseconds} ms");
        }
    }
}