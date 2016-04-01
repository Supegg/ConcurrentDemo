using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace Server
{
    class Program
    {
        static List<TcpClient> tcpList = new List<TcpClient>();
        static TcpListener listener = new TcpListener(IPAddress.Any, 9998);
        static volatile int rwCount = 0;
        //static object l = new object();
        static Stopwatch watch = new Stopwatch();
          
        static void Main(string[] args)
        {
            start();
              
            do
            {
                Console.WriteLine("Clients: {0}\t, rws/sec: {1}", tcpList.Count, rwCount);
                rwCount = 0;//set 0, per second
                Thread.Sleep(1000);
            } while (true);

            Console.WriteLine("press any key to quit....");
            Console.Read();
            Environment.Exit(0);

        }

        static void start()
        {
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(acceptCallback), listener);
        }

        private static void acceptCallback(IAsyncResult ar)
        {
            if(!watch.IsRunning)
            {
                watch.Restart();
            }

            TcpListener listener = (TcpListener)ar.AsyncState;
            try
            {
                TcpClient conn = listener.EndAcceptTcpClient(ar);
                tcpList.Add(conn);
                byte[] buf = new byte[1024];
                object[] state = new object[] {
                    conn,
                    buf
                };

                conn.GetStream().BeginRead(buf, 0, buf.Length, new AsyncCallback(read), state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                try
                {
                    listener.BeginAcceptTcpClient(
                        new AsyncCallback(acceptCallback),
                        listener);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void read(IAsyncResult ar)
        {
            rwCount++;
            object[] state = (object[])ar.AsyncState;
            TcpClient conn = (TcpClient)state[0];
            byte[] buf = (byte[])state[1];

            try
            {
                int bytesRead = conn.GetStream().EndRead(ar);

                conn.GetStream().BeginWrite(buf, 0, bytesRead, new AsyncCallback(write), state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                conn.GetStream().Close();
                conn.Close();
            }
        }

        private static void write(IAsyncResult ar)
        {
            rwCount++;
            object[] state = (object[])ar.AsyncState;
            TcpClient conn = (TcpClient)state[0];
            byte[] buf = (byte[])state[1];

            try
            {
                conn.GetStream().EndWrite(ar);
                conn.GetStream().BeginRead(buf, 0, buf.Length, new AsyncCallback(read), state);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                conn.GetStream().Close();
                conn.Close();
            }
        }
    }
}
