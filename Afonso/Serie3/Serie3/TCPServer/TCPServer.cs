using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication1 {
    delegate Task<Response> operation(Request req, CancellationToken cancellationToken);

    /**
    * The echo TCP server
    */
    class TCPServer {
        private const int SERVER_PORT = 8080;

        private const int MIN_SERVICE_TIME = 10;
        private const int MAX_SERVICE_TIME = 500;

        
        private const int WAIT_FOR_IDLE_TIME = 10000;
        private const int POLLING_INTERVAL = WAIT_FOR_IDLE_TIME / 20;
        
        TcpListener _server;

        private HttpHelper _helper;

        private static Logger<string> _logger;

        private Dictionary<Method, operation> router;

        private ShutdownHandler _shutdownHandler;

        private ConcurrentDictionary<Request, AsyncTransferQueue<JObject>> _asyncQueues;


        // The maximum number of simultaneous connections allowed.
        private int MaxSimultaneousConnections = Environment.ProcessorCount;

        // The cancellation token source used to shutdown the server.
        private readonly CancellationTokenSource _cancellationTokenSource;

        // Indicates if server is on shutdown process.
        private static bool shutdown = false;
        private static bool endLog = false;

        // The JSON serializer object
        private static readonly JsonSerializer serializer = new JsonSerializer();


        public TCPServer() {
            _helper = new HttpHelper();
            router = new Dictionary<Method, operation>();
            _logger = new Logger<string>();
            _cancellationTokenSource = new CancellationTokenSource();
            _asyncQueues = new ConcurrentDictionary<Request, AsyncTransferQueue<JObject>>();
            InitializeRouter();

            _server = TcpListener.Create(SERVER_PORT);
            _server.Start();
        }

        public void InitializeRouter() {
            router.Add(Method.PUT, PutMessageIntoQueue);
            router.Add(Method.TAKE, TakeMessageFromQueue);
            router.Add(Method.CREATE, CreateMessageIntoQueue);
            router.Add(Method.SHUTDOWN, ShutdownServer);
            router.Add(Method.TRANSFER, TransferMessageIntoQueue);
        }

        private async Task<Response> TransferMessageIntoQueue(Request req, CancellationToken cancellationtoken) {
            AsyncTransferQueue<JObject> queue;

            if (!_asyncQueues.TryGetValue(req, out queue)) {
                return _helper.NotFoundForPath();
            }

            if (!req.Headers.TryGetValue("timeout", out string timeout))
                timeout = "0";

            bool result = await queue.TransferAsync(req.Payload, int.Parse(timeout), cancellationtoken);

            return result ?
                _helper.OperationSuccessful("Request successfully transfered!") :
                _helper.TimeoutResponse();
        }

        private async Task<Response> ShutdownServer(Request req, CancellationToken cancellationtoken) {
            Volatile.Write(ref shutdown,true);    //?????????
            
            Log("Server is shutting down...",false);
            
            // If there are already connections in action, wait for them to end
            for (int i = 0; i < WAIT_FOR_IDLE_TIME; i+=POLLING_INTERVAL) {
                if(!_server.Pending())
                    break;
                await Task.Delay(POLLING_INTERVAL); //50ms to wait for the requests to end
            }

            if (!req.Headers.TryGetValue("timeout", out string timeout))
                timeout = "0";
            
            _server.Stop();
            
            return await _shutdownHandler.AwaitShutdown(_cancellationTokenSource,int.Parse(timeout));
        }
        
        private async Task<Response> CreateMessageIntoQueue(Request req, CancellationToken cancellationtoken) {
            string path = req.Path;
            if (!_asyncQueues.TryAdd(req, new AsyncTransferQueue<JObject>())) {
                return _helper.BadRequest($"There is already a queue with the path {path}!");
            }

            return _helper.OperationSuccessful($"Queue with path {path} was added successfully!");
        }

        private async Task<Response> TakeMessageFromQueue(Request req, CancellationToken cancellationtoken) {
            AsyncTransferQueue<JObject> queue;

            if (!_asyncQueues.TryGetValue(req, out queue)) {
                return _helper.NotFoundForPath();
            }

            if (!req.Headers.TryGetValue("delay", out string timeout))
                timeout = "0";
            
            JObject result = await queue.TakeAsync(int.Parse(timeout));
            
            if (result == null)
                return _helper.TimeoutResponse();
            
            Response response = _helper.OperationSuccessful($"Message successfully taken from request {req}!");
            response.Payload = result;

            return response;
        }

        private async Task<Response> PutMessageIntoQueue(Request req, CancellationToken cancellationtoken) {
            AsyncTransferQueue<JObject> queue;

            if (!_asyncQueues.TryGetValue(req, out queue)) {
                return _helper.NotFoundForPath();
            }

            queue.PutAsync(req.Payload);
            return _helper.OperationSuccessful("Request successfully put to queue!");
        }

        // Process a client's connection
        private async Task ServeConnectionAsync(int id, TcpClient connection, int service_time) {
            using (connection) {
                var stream = connection.GetStream();
                var reader = new JsonTextReader(new StreamReader(stream)) {
                    // Support reading multiple top-level objects
                    SupportMultipleContent = true
                };
                var writer = new JsonTextWriter(new StreamWriter(stream));
                try {
                    // Consume any bytes until start of object character ('{')
                    do {
                        await reader.ReadAsync();
                        //Console.WriteLine($"advanced to {reader.TokenType}");
                    } while (reader.TokenType != JsonToken.StartObject &&
                             reader.TokenType != JsonToken.None);

                    if (reader.TokenType == JsonToken.None) {
                        Log($"[{id}] reached end of input stream, ending.",false);
                        return;
                    }

                    // Load root JSON object
                    JObject json = await JObject.LoadAsync(reader);

                    // Retrive the Request object, and show its Method and Headers fields
                    Request request = json.ToObject<Request>();
                    Log($"Request {{\n  Method: {request.Method}",false);
                    Log("  Headers: { ", true);
                    if (request.Headers != null) {
                        int i = 0;
                        foreach (KeyValuePair<String, String> entry in request.Headers) {
                            Console.Write($"{entry.Key}: {entry.Value}");
                            if (i < request.Headers.Count - 1)
                                Console.Write(", ");
                            i++;
                        }
                    }

                    Console.WriteLine(" }\n}");

                    // Simulate the service time
                    await Task.Delay(service_time);

                    // Build the response and send it to the client
                    var response = new Response {
                        Status = 200,
                        Payload = json,
                    };
                    
                    serializer.Serialize(writer, response);
                    await writer.FlushAsync();
                }
                catch (JsonReaderException e) {
                    Log($"[{id}] Error reading JSON: {e.Message}, continuing", false);
                    var response = new Response {Status = 400,};
                    serializer.Serialize(writer, response);
                    await writer.FlushAsync();
                    // close the connection because an error may not be recoverable by the reader
                    return;
                }
                catch (Exception e) {
                    Log($"[{id}] Unexpected exception, closing connection {e.Message}", false);
                    return;
                }
            }
        }

        /**
     * Listen for connections, but without parallelizing multiple-connection processing
     */
        public async Task ListenAsync(TcpListener listener) {
            int connId = 0;
            Random random = new Random(Environment.TickCount);
            listener.Start();
            Log($"Listening on port {SERVER_PORT}", false);
            do {
                try {
                    TcpClient connection = await listener.AcceptTcpClientAsync();
                    connId++;
                    Log($"--connection accepted with id: {connId}", false);
                    await ServeConnectionAsync(connId, connection, random.Next(MIN_SERVICE_TIME, MAX_SERVICE_TIME));
                }
                catch (ObjectDisposedException) {
                    // Exit the method normally, the listen socket was closed
                    break;
                }
                catch (InvalidOperationException) {
                    break; // When AceptTcpClienteAsync() is calleda after Stop()
                }
            } while (true);
            
        }

        private void Log(string message, bool isWrite) =>
            _logger.Put(isWrite ? message + '\n' : message);


        /**
     * Execute server and wait for <enter> to shutdown.
     */
        public static async Task Main() {
            TCPServer server = new TCPServer();
            Thread logger = new Thread(LoggerWork);
            logger.Priority = ThreadPriority.Lowest;
            logger.Start();
            var listener = new TcpListener(IPAddress.Loopback, SERVER_PORT);
            var listenTask = server.ListenAsync(listener);
            
            server.Log("---hit Ctrl + C to shutdown the server...", false);
            await Console.In.ReadLineAsync();

            // Establish an event handler to process key press events.
            // Press Ctrl+C to interrupt server
            // Shutdown the server graciously
            Console.CancelKeyPress += (sender, eargs) => {
                server.Log("CancelKeyPress, stopping server", false);
                server._cancellationTokenSource.Cancel();
                server._server.Stop();
                endLog = true;
                eargs.Cancel = true;
            };
            
            // Wait until all accepted connections are served
            await listenTask;
        }

        public static void LoggerWork() {
            Logger<string> logger = _logger;

            while (!endLog) {
                List<string> messages = logger.TakeAll();
                messages.ForEach(Console.WriteLine);
            }
        }
    }
}