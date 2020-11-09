using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StartreckSimulator.Properties;
using WatsonWebsocket;

namespace StartreckSimulator.Models
{
    public class ClassificationServer
    {
        #region / / / / /  Private fields  / / / / /

        private WatsonWsServer _server;
        private readonly Timer _timer;
        private List<ClassificationTypes> _classifications = new List<ClassificationTypes>();
        private readonly Dictionary<Request, DateTime> _requestTimes = new Dictionary<Request, DateTime>();
        private readonly object _syncToken = new object();

        #endregion

        #region / / / / /  Properties  / / / / /

        public Uri ServerUrl { get; }

        public bool IsOpen { get; private set; }

        public int StatusCode { get; set; } = 200;

        public bool IsSuccess { get; set; } = true;

        public IEnumerable<ClassificationTypes> Classifications => _classifications;

        #endregion

        #region / / / / /  Private methods  / / / / /

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_syncToken)
            {
                var requestsToRemove = new List<Request>();
                foreach (var requestTime in _requestTimes)
                {
                    if (DateTime.Now - requestTime.Value >= TimeSpan.FromMilliseconds(Settings.Default.ClassificationRequestIntervalMS))
                    {
                        var response = CreateResponse(requestTime.Key.RequestId, requestTime.Key.MissionId, requestTime.Key.SensorId);
                        SendResponse(response);
                        requestsToRemove.Add(requestTime.Key);
                    }
                }

                requestsToRemove.ForEach(x => _requestTimes.Remove(x)); 
            }
        }

        private void Server_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                string message = Encoding.UTF8.GetString(e.Data);
                if (Settings.Default.ValidateMessages && !message.IsValid(out var errors))
                {
                    Error?.Invoke(this, errors);
                }
                else
                {
                    ProcessMessage(message);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        private void SendResponse(Response response)
        {
            try
            {
                foreach (var client in _server.ListClients())
                {
                    string json = "";
                    if (Settings.Default.AddJsonRootObject)
                    {
                        var root = new
                        {
                            Response = response
                        };
                        json = root.ToJson();
                    }
                    else
                    {
                        json = response.ToJson();
                    }
                    _server.SendAsync(client, json);
                    ResponseSent?.Invoke(this, json);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        private void SendAck(string requestId)
        {
            try
            {
                var ack = new Acknowledge
                {
                    MessageType = MessageType.Acknowledge,
                    Code = StatusCode,
                    RequestId = requestId
                };

                foreach (var client in _server.ListClients())
                {
                    string json = "";
                    if (Settings.Default.AddJsonRootObject)
                    {
                        var root = new
                        {
                            Acknowledge = ack
                        };
                        json = root.ToJson();
                    }
                    else
                    {
                        json = ack.ToJson();
                    }

                    _server.SendAsync(client, json);
                    AckSent?.Invoke(this, json);
                }
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        private void HandleRequest(Request request)
        {
            lock (_syncToken)
            {
                if (request.Command == "Classification")
                {
                    _requestTimes.Add(request, DateTime.Now);
                }
                else if (request.Command == "Stop")
                {
                    var key = _requestTimes.Keys.FirstOrDefault(x => x.RequestId == request.RequestId);
                    if (key != null)
                    {
                        _requestTimes.Remove(key);
                    }
                } 
            }
            SendAck(request.RequestId);
        }

        private Response CreateResponse(string requestId, int missionId, string sensorId)
        {
            var response =  new Response
            {
                MessageType = MessageType.Response,
                MissionId = missionId,
                SensorId = sensorId,
                RequestId = requestId,
                Code = StatusCode
            };
            if (IsSuccess)
            {
                response.Classifications = Classifications.Select(x => new Classification {Type = x})
                    .ToArray();
            }
            else
            {
                response.Code = 500;
                response.Message = "INTERNAL SERVER ERROR";
            }

            return response;
        }

        private string ParseJson(string json, out string root)
        {
            var rootToken = JObject.Parse(json).First;
            root = rootToken?.Path;
            return rootToken.First.ToString();
        }

        private void ProcessMessage(string message)
        {
            if (Settings.Default.AddJsonRootObject)
            {
                var json = ParseJson(message, out string root);

                if (root.ToLower() == "request")
                {
                    var request = JsonConvert.DeserializeObject<Request>(json);
                    RequestReceived?.Invoke(this, message);
                    HandleRequest(request);
                }
                else if (root.ToLower() == "acknowledge")
                {
                    AckReceived?.Invoke(this, message);
                }
                else
                {
                    Error?.Invoke(this, new NotSupportedException($"Unknown Message Type: {root}"));
                }
            }
            else
            {
                var root = JsonConvert.DeserializeObject<RootObject>(message);
                switch (root.MessageType)
                {
                    case MessageType.Request:
                        var request = JsonConvert.DeserializeObject<Request>(message);
                        RequestReceived?.Invoke(this, message);
                        HandleRequest(request);
                        break;
                    case MessageType.Response:
                        break;
                    case MessageType.Acknowledge:
                        AckReceived?.Invoke(this, message);
                        break;
                    default:
                        Error?.Invoke(this, new NotSupportedException($"Unknown Message Type: {root}"));
                        break;
                }
            }
        }

        #endregion

        #region / / / / /  Public methods  / / / / /

        public ClassificationServer(Uri url)
        {
            ServerUrl = url;
            _timer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
            _timer.Elapsed += Timer_Elapsed;
        }

        public void Start()
        {
            try
            {
                _server = new WatsonWsServer(ServerUrl.DnsSafeHost, ServerUrl.Port, false);
                _server.MessageReceived += Server_MessageReceived;
                _server.Start();
                _timer.Start();
                IsOpen = true;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        public void Stop()
        {
            _server.Dispose();
            _timer.Stop();
            IsOpen = false;
        }

        public void UpdateClassifications(IEnumerable<ClassificationTypes> classifications)
        {
            _classifications = new List<ClassificationTypes>(classifications);
        }

        #endregion

        #region / / / / /  Events  / / / / /

        public event EventHandler<string> RequestReceived;
        public event EventHandler<string> ResponseSent;
        public event EventHandler<string> AckReceived;
        public event EventHandler<string> AckSent;
        public event EventHandler<Exception> Error;

        #endregion
    }
}