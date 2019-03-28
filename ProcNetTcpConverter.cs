using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;

namespace HexToIP
{
    class ProcNetTcpConverter
    {
        static string filePath = @"";
        static readonly string header = string.Format("{0, 5} {1, 20} {2, 20} {3, 5} {4, 5} {5} {6} {7, 5} {8} {9, 5} {10, 10} {11, 7}", "sl", "local_address", "rem_address", "st", "tx_queue", "rx_queue", "tr", "tm->when", "retrnsmt", "uid", "timeout", "inode");

        static void Main(string[] args)
        {
            Console.SetWindowSize(200, 50);

            if (filePath.Equals(""))
            {
                Console.WriteLine("Please specify file path");
                filePath = Console.ReadLine();
            }

            Console.WriteLine("Press escape or ^c to abort");
            do
            {
                //Get the results from the batch every 1 seconds
                while (!Console.KeyAvailable)
                {
                    RunBatch();
                    Thread.Sleep(1000);
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            Console.WriteLine("\nPress any key to quit");
            Console.ReadLine();
        }

        /// <summary>
        /// Run a batch script that checks for all open TCP connections on a connected device.
        /// </summary>
        static void RunBatch()
        {
            var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    FileName = filePath
                }
            };

            process.Start();
            string rawResult = process.StandardOutput.ReadToEnd();

            ReplaceHexNotation(rawResult);
        }

        /// <summary>
        /// Repalce the ip address + port that is in hex notation with an ip address + port that is in decimal notation
        /// </summary>
        /// <param name="rawResult"></param>
        static void ReplaceHexNotation(string rawResult)
        {
            string[] splitResults = rawResult.Trim().Split('\n');

            Console.WriteLine("\n" + splitResults[0]);

            for (int i = 0; i < splitResults.Length; i++)
            {
                if (i == 1)
                {
                    Console.WriteLine(header);
                }

                if (i > 2)
                {
                    TCPResult result = new TCPResult(splitResults[i]);
                    Console.WriteLine(result.GetMessage());
                }
            }
        }

        /// <summary>
        /// Convert an ip address + port (format: 00AABB11:CD23) from hex notation to decimal notation.
        /// Implementation taken from: https://stackoverflow.com/a/1355163/8628766
        /// </summary>
        /// <param name="input">Hex "ip:port" to convert</param>
        /// <returns>Input ip as decimal string</returns>
        static internal string ConvertFromInput(string input)
        {
            string[] ipPart = input.Split(':');
            var ip = new IPAddress(long.Parse(ipPart[0], NumberStyles.AllowHexSpecifier)).ToString();
            var port = long.Parse(ipPart[1], NumberStyles.AllowHexSpecifier).ToString();
            return ip + ":" + port;
        }

        /// <summary>
        /// Find the index of a character's n'th occurance
        /// </summary>
        /// <param name="s">Input string</param>
        /// <param name="t">Character to check</param>
        /// <param name="n">Occurance index</param>
        /// <returns></returns>
        static int GetNthIndex(string s, char t, int n)
        {
            int count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == t)
                {
                    count++;
                    if (count == n)
                    {
                        return i;
                    }
                }
            }
            throw new IndexOutOfRangeException();
        }
    }

    class TCPResult
    {
        private readonly string tcpResultMessage;

        /// <summary>
        /// Recreate the Tcp result message so we can re-format it with the new ip format
        /// </summary>
        /// <param name="rawInput">input string</param>
        internal TCPResult(string rawInput)
        {
            rawInput = rawInput.Trim();
            List<string> inputList = rawInput.Split(' ').ToList();
            inputList.RemoveAll(o => o.Equals(""));

            string sl = inputList[0];
            string local_address = ProcNetTcpConverter.ConvertFromInput(inputList[1]);
            string rem_address = ProcNetTcpConverter.ConvertFromInput(inputList[2]);
            string st = inputList[3];
            string tx_queue = inputList[4];
            string rx_queue = inputList[5];
            string tr = inputList[6];
            string tmWhen = inputList[7];
            string retrnsmt = inputList[8];
            string uid = inputList[9];
            string timeout = inputList[10];
            string inode = string.Format("{0} {1, 5} {2, 3} {3, 3} {4, 3} {5, 4} ", inputList[11], inputList[12], inputList[13], inputList[14], inputList[15], inputList[16]);

            tcpResultMessage = string.Format("{0, 5} {1, 20} {2, 20} {3, 5} {4, 5} {5} {6} {7, 5} {8, 10} {9, 7} {10} {11}", sl, local_address, rem_address, st, tx_queue, rx_queue, tr, tmWhen, retrnsmt, uid, timeout, inode);
        }

        internal string GetMessage()
        {
            return tcpResultMessage;
        }
    }
}