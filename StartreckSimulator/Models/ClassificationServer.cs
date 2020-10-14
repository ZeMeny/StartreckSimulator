using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Newtonsoft.Json;
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

        public IEnumerable<ClassificationTypes> Classifications => _classifications;

        #endregion

        #region / / / / /  Private methods  / / / / /

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_syncToken)
            {
                List<Request> requestsToRemove = new List<Request>();
                foreach (var requestTime in _requestTimes)
                {
                    if (DateTime.Now - requestTime.Value >= TimeSpan.FromMilliseconds(Settings.Default.ClassificationRequestIntervalMS))
                    {
                        var response = CreateResponse(requestTime.Key.MissionId, requestTime.Key.SensorId);
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
                var request = JsonConvert.DeserializeObject<Request>(Encoding.UTF8.GetString(e.Data));

                RequestReceived?.Invoke(this, request);
                HandleRequest(request);
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
                string json = JsonConvert.SerializeObject(response, Formatting.Indented);

                foreach (var client in _server.ListClients())
                {
                    _server.SendAsync(client, json);
                }
                ResponseSent?.Invoke(this, response);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
            }
        }

        private void SendAck()
        {
            try
            {
                var ack = new Acknowledge
                {
                    Code = StatusCode
                };
                string json = JsonConvert.SerializeObject(ack, Formatting.Indented);

                foreach (var client in _server.ListClients())
                {
                    _server.SendAsync(client, json);
                }
                AckSent?.Invoke(this, ack);
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
                    var key = _requestTimes.Keys.FirstOrDefault(x =>
                        x.MissionId == request.MissionId && x.SensorId == request.SensorId);
                    if (key != null)
                    {
                        _requestTimes.Remove(key);
                    }
                } 
            }
            SendAck();
        }

        private Response CreateResponse(int missionId, string sensorId)
        {
            return new Response
            {
                Classifications = Classifications.Select(x => new Classification { Type = x.ToString() })
                    .ToArray(),
                MissionId = missionId,
                SensorId = sensorId
            };
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
            _classifications.AddRange(classifications);
            _classifications = _classifications.Distinct().ToList();
        }

        #endregion

        #region / / / / /  Events  / / / / /

        public event EventHandler<Request> RequestReceived;
        public event EventHandler<Response> ResponseSent;
        public event EventHandler<Acknowledge> AckSent;
        public event EventHandler<Exception> Error;

        #endregion
    }
}