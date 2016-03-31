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
        static volatile int tcpNum = 0;
        static int robots = 1;
        static int repeat = 100;
        //static object l = new object();//for lock
        static List<TcpClient> tcpList = new List<TcpClient>();
        static Stopwatch watch = new Stopwatch();
          //teest branch
        static void Main(string[] args)
        {
            presure();

            do
            {
                Console.Write(DateTime.Now.ToLongTimeString());
                Console.WriteLine("  Robots:{0}*{1}\t, tcpNums: {2}, errors:{3}\t, run times:{4}", robots, repeat, tcpNum, errors, watch.ElapsedMilliseconds);
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
                    for (int r = 0; r < repeat; r++)
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

                            tcpNum++;

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
