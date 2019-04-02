using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;

class ProcNetTcpConverter
{
    //These values can either be hardcoded, or left empty. If left empty the programm will ask for the user to input manually.
    static string filePath = @"";
    internal static string[] remoteIPFilter = new string[] { "" };

    static readonly string header = string.Format("{0, 5} {1, 20} {2, 20} {3, 12} {4, 5} {5} {6} {7, 5} {8, 10} {9, 7} {10, 8} {11}", "sl", "local_address", "rem_address", "state", "tx_queue", "rx_queue", "tr", "tm->when", "retrnsmt", "uid", "timeout", "inode");
    static readonly string ipRegex = @"[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}";

    static void Main(string[] args)
    {
        while (true)
        {
            try
            {
                Console.SetWindowSize(220, 50);

                if (filePath.Equals(""))
                {
                    Console.WriteLine("Please specify file path");
                    filePath = Console.ReadLine();
                }

                if (remoteIPFilter.Length == 0)
                {
                    Console.WriteLine("IPv4 Filters (seperated by space, leave blank if none):");
                    remoteIPFilter = Console.ReadLine().Split(' ');
                    if (remoteIPFilter[0] != "")
                    {
                        IPCheck();
                    }
                }

                Console.WriteLine("Press escape or ^c to pause");
                do
                {
                    while (!Console.KeyAvailable)
                    {
                        RunBatch();
                        Thread.Sleep(100);
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

                Console.WriteLine("\nPress any key to resume");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
            }
        }
    }

    /// <summary>
    /// Check if the given ip is valid. 
    /// Checks ip recursively after entering new IP.
    /// </summary>
    static void IPCheck()
    {
        for (int i = 0; i < remoteIPFilter.Length; i++)
        {
            var match = System.Text.RegularExpressions.Regex.Match(remoteIPFilter[i], ipRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                Console.WriteLine("{0} is not a valid ipv4 address, please re-enter, or leave blank to continue.", remoteIPFilter[i]);
                remoteIPFilter[i] = Console.ReadLine();
                if (remoteIPFilter[i] != "")
                {
                    IPCheck();
                }
            }
        }
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
                string message = result.GetMessage();
                if (message != null)
                {
                    Console.WriteLine(message);
                }
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
    internal static int GetNthIndex(string s, char t, int n)
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
        throw new IndexOutOfRangeException("GetNthIndex Exception: Index was out of range.");
    }
}

class TCPResult
{
    private readonly string tcpResultMessage;
    enum TcpStates
    {
        ESTABLISHED = 1,
        SYN_SENT,
        SYN_RECV,
        FIN_WAIT1,
        FIN_WAIT2,
        TIME_WAIT,
        CLOSE,
        CLOSE_WAIT,
        LAST_ACK,
        LISTEN,
        CLOSING,
        NEW_SYN_RECV,

        TCP_MAX_STATES
    };

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
        TcpStates st = ((TcpStates)Convert.ToInt32(inputList[3], 16));
        string tx_queue = inputList[4].Substring(ProcNetTcpConverter.GetNthIndex(inputList[4], ':', 1) - 8, 8);
        string rx_queue = inputList[4].Substring(ProcNetTcpConverter.GetNthIndex(inputList[4], ':', 1) + 1, 8);
        string tr = inputList[5].Substring(ProcNetTcpConverter.GetNthIndex(inputList[5], ':', 1) - 2, 2);
        string tmWhen = inputList[5].Substring(ProcNetTcpConverter.GetNthIndex(inputList[5], ':', 1) + 1, 8);
        string retrnsmt = inputList[6];
        string uid = inputList[7];
        string timeout = inputList[8];
        string inode = "";

        //iNode doens't always have 8 parameters. Sometimes it only has 3
        if (inputList.Count > 12)
        {
            inode = string.Format("{0, 6} {1, 3} {2, 16} {3, 3} {4, 3} {5, 3} {6, 3} {7,3} ", inputList[9], inputList[10], inputList[11], inputList[12], inputList[13], inputList[14], inputList[15], inputList[16]);
        }
        else
        {
            inode = string.Format("{0, 6} {1, 3} {2, 16} {3, 3} {4, 3} {5, 3} {6, 3} {7,3} ", inputList[9], inputList[10], inputList[11], "-1", "-1", "-1", "-1", "-1");
        }

        //return if the remote address is in the filter array
        if (ProcNetTcpConverter.remoteIPFilter.Contains(rem_address.Substring(0, rem_address.IndexOf(':'))))
        {
            return;
        }

        tcpResultMessage = string.Format("{0, 5} {1, 20} {2, 20} {3, 12} {4, 5}:{5} {6}:{7, 5} {8, 10} {9, 7} {10, 8} {11}", sl, local_address, rem_address, st.ToString(), tx_queue, rx_queue, tr, tmWhen, retrnsmt, uid, timeout, inode);

        if (Convert.ToInt32(tx_queue, 16) > 0 && st == TcpStates.ESTABLISHED)//Indicates this is the current active transmissiting connection
        {
            tcpResultMessage += " <= Active transmitting connection";
        }
        else if (Convert.ToInt32(rx_queue, 16) > 0 && st == TcpStates.ESTABLISHED)//Indicates this is the current active receiving connection
        {
            tcpResultMessage += " <= Active receiving connection";
        }
    }

    internal string GetMessage()
    {
        return tcpResultMessage;
    }
}