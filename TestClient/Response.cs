namespace TestClient
{
    public class Response
    {
        public string SensorId { get; set; }
        public int MissionId { get; set; }
        public Classification[] Classifications { get; set; }
        public string RequestId { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }
}