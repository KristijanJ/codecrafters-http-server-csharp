using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
var socket = server.AcceptSocket(); // wait for client

Byte[] bytes = new Byte[256];
socket.Receive(bytes);

var data = Encoding.UTF8.GetString(bytes) ?? "";

var reqDataChunks = data?.Split("\r\n");

// Status line like GET /echo/abc HTTP/1.1
var statusLine = reqDataChunks?[0];

// Route from status line like /echo/abc
string route = statusLine?.Split(" ")?[1].Trim()!;

Console.WriteLine("THIS IS MY data: {0}", data);

if (route.Contains("/echo/"))
{
  var responseString = route.Replace("/echo/", "");
  socket.Send(Encoding.UTF8.GetBytes(
    "HTTP/1.1 200 OK\r\n" +
    $"Content-Type: text/plain\r\nContent-Length: {responseString.Length}\r\n\r\n" +
    responseString
  ));
}
else if (route == "/")
{
  socket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n"));
}
else
{
  socket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 404 Not Found\r\n\r\n"));
}
