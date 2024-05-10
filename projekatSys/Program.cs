using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class WebServer
{
    private HttpListener _listener;
    private string _rootDirectory;
    private Cache _cache;

    public WebServer(string uriPrefix, string rootDirectory)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(uriPrefix);
        _rootDirectory = rootDirectory;
        _cache = new Cache();
    }

    public void Start()
    {
        _listener.Start();
        Console.WriteLine("Server je startovan");
        while (true)
        {
            HttpListenerContext context = _listener.GetContext();
            ThreadPool.QueueUserWorkItem((_) => ProcessRequest(context));
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        string filename = context.Request.RawUrl.Trim('/');
        if (string.IsNullOrEmpty(filename))
        {
            context.Response.StatusCode = 400; // Bad Request
            context.Response.Close();
            return;
        }

        string path = Path.Combine(_rootDirectory, filename);

        if (!File.Exists(path))
        {
            context.Response.StatusCode = 404;
            WriteResponse(context, "Greska: Ne postoji fajl koji zelite");
            return;
        }

        string hash;
        if (_cache.TryGet(path, out hash))
        {
            WriteResponse(context, hash);
            return;
        }

        using (FileStream stream = File.OpenRead(path))
        {
            SHA256Managed sha = new SHA256Managed();
            byte[] checksum = sha.ComputeHash(stream);
            hash = BitConverter.ToString(checksum).Replace("-", String.Empty);
            _cache.Add(path, hash);
        }

        WriteResponse(context, hash);
    }

    private void WriteResponse(HttpListenerContext context, string responseString)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        context.Response.ContentLength64 = buffer.Length;
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.Close();
    }
}

public class Cache
{
    private Dictionary<string, string> _cache;

    public Cache()
    {
        _cache = new Dictionary<string, string>();
    }

    public bool TryGet(string key, out string value)
    {
        return _cache.TryGetValue(key, out value);
    }

    public void Add(string key, string value)
    {
        _cache[key] = value;
    }
}

class Program
{
    static void Main(string[] args)
    {
        WebServer server = new WebServer("http://localhost:5050/", @"C:\Users\RZYEN 5\source\repos\projekatSys\projekatSys\root_folder");
        server.Start();
    }
}
