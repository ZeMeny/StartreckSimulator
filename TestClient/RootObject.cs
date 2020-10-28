namespace TestClient
{
    public class RootObject<T>
    {
        public T Content { get; set; }

        public RootObject(T content)
        {
            Content = content;
        }
    }
}