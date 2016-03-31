using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace Server
{
    class Program
    {
        static List<TcpClient> tcpList = new List<TcpClient>();
        static TcpListener listener = new TcpListener(IPAddress.Any, 9998);
        static int rwCount = 0;
        //static object l = new object();
          
        static void Main(string[] args)
        {
            start();
              
            do
            {
                Console.WriteLine("Clients: {0}, rwCount: {1}", tcpList.Count, rwCount);
                rwCount = 0;
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
            object[] state = (object[])ar.AsyncState;
            TcpClient conn = (TcpClient)state[0];
            byte[] buf = (byte[])state[1];

            try
            {
                int bytesRead = conn.GetStream().EndRead(ar);
                rwCount++;

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
            object[] state = (object[])ar.AsyncState;
            TcpClient conn = (TcpClient)state[0];
            byte[] buf = (byte[])state[1];

            try
            {
                conn.GetStream().EndWrite(ar);
                rwCount++;
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
