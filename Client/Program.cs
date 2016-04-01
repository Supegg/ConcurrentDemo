using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

namespace Client
{
    class Program
    {
        static volatile int errors = 0;
        static volatile int rwCount = 0;
        static int robots = 4;//4*4 = 9w
        static int tcps = 100;
        //static object l = new object();//for lock
        static List<TcpClient> tcpList = new List<TcpClient>();

        static Stopwatch watch = new Stopwatch();
        static void Main(string[] args)
        {
            presure();

            do
            {
                Console.WriteLine("Robots:{0}*{1}\t, rw/sec: {2}\t, errors:{3}\t", 
                    robots, tcps, rwCount, errors);
                rwCount = 0;//set 0, per second
                Thread.Sleep(1000);
            } while (true);

            Console.Read();
            Environment.Exit(0);
        }

        private static void presure()
        {
            string server = "127.0.0.1";
            int port = 9998;
            List<Thread> ts = new List<Thread>();

            Console.WriteLine("{0}\tTcp并发服务压力测试\r\n", DateTime.Now.ToLongTimeString());

            for (int i = 0; i < robots; i++)
            {
                ts.Add(new Thread(new ParameterizedThreadStart((o) =>
                {
                    for (int r = 0; r < tcps; r++)
                    {

                        try
                        {
                            byte[] buf = Guid.NewGuid().ToByteArray();
                            Array.Resize<byte>(ref buf, 1000);
                            TcpClient conn = new TcpClient(server, port);
                            tcpList.Add(conn);
                            object[] state = new object[] {
                                conn,
                                buf
                            };

                            conn.GetStream().BeginWrite(buf, 0, buf.Length, new AsyncCallback(write), state);

                        }
                        catch (Exception ex)
                        {
                            errors++;
                            Console.WriteLine("{0}\t{1}\t{2}", Thread.CurrentThread.Name, errors, ex.Message);
                        }
                    }

                })) { Name = "T" + i });
            }

            watch.Restart();
            foreach (var t in ts)
            {
                t.Start();
                Console.WriteLine(t.Name);
            }

            //foreach (var t in ts)
            //{
            //    t.Join();
            //}
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
                errors++;
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
                errors++;
                Console.WriteLine(ex.Message);
                conn.GetStream().Close();
                conn.Close();
            }

        }
    }
}
