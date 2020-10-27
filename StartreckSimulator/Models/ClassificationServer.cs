﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
                var json = ParseJson(message);
                var request = JsonConvert.DeserializeObject<Request>(json);

                // if this is not a classification request
                if (string.IsNullOrEmpty(request.Command))
                {
                    var ack = JsonConvert.DeserializeObject<Acknowledge>(message);
                    // if it is not a valid ack
                    if (ack.Code <= 0)
                    {
                        return;
                    }
                    AckReceived?.Invoke(this, ack);
                }
                else
                {
                    RequestReceived?.Invoke(this, request);
                    HandleRequest(request);
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
                string json = JsonConvert.SerializeObject(response, Formatting.Indented, new StringEnumConverter());

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

        private void SendAck(string requestId)
        {
            try
            {
                var ack = new Acknowledge
                {
                    Code = StatusCode,
                    RequestId = requestId
                };
                string json = JsonConvert.SerializeObject(ack, Formatting.Indented, new StringEnumConverter());

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
                else if (request.Command == "Acknowledge")
                {
                    return;
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
                MissionId = missionId,
                SensorId = sensorId,
                RequestId = requestId,
                Code = StatusCode.ToString()
            };
            if (IsSuccess)
            {
                response.Classifications = Classifications.Select(x => new Classification {Type = x})
                    .ToArray();
            }
            else
            {
                response.Code = "Failure";
                response.Message = "GPU Overload";
            }

            return response;
        }

        private string ParseJson(string json)
        {
            return JObject.Parse(json).Children().ElementAt(0).Children().ElementAt(0).ToString();
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

        public event EventHandler<Request> RequestReceived;
        public event EventHandler<Response> ResponseSent;
        public event EventHandler<Acknowledge> AckSent;
        public event EventHandler<Acknowledge> AckReceived;
        public event EventHandler<Exception> Error;

        #endregion
    }
}