using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

namespace Shadowbot
{
    struct IRCConfig
    {
        public string server;
        public int port;
        public string nick;
        public string name;
    }

    class IRCBot
    {
        Stopwatch pinger = new Stopwatch(); 
        TcpClient IRCConnection = null;
        IRCConfig config;
        NetworkStream ns = null;
        StreamReader sr = null;
        StreamWriter sw = null;

        public IRCBot(IRCConfig config)
        {
            bool stayConnected = true;
            while (stayConnected)
            {
                this.config = config;
                try
                {
                    IRCConnection = new TcpClient(config.server, config.port);
                }
                catch
                {
                    Console.WriteLine("Connection Error");
                }

                try
                {
                    ns = IRCConnection.GetStream();
                    ns.ReadTimeout = 10;
                    sr = new StreamReader(ns);
                    sw = new StreamWriter(ns);

                    sendData("USER", config.nick + " Japa " + " Japa" + " :" + config.name);
                    sendData("NICK", config.nick);
                    stayConnected = !IRCWork();
                }
                catch
                {
                    Console.WriteLine("Communication error");
                }
                finally
                {
                    if (sr != null)
                        sr.Close();
                    if (sw != null)
                        sw.Close();
                    if (ns != null)
                        ns.Close();
                    if (IRCConnection != null)
                        IRCConnection.Close();
                }
            }
        }

        public void sendData(string cmd, string param)
        {
            if (param == null)
            {
                sw.WriteLine(cmd);
                sw.Flush();
                Console.WriteLine(cmd);
            }
            else
            {
                sw.WriteLine(cmd + " " + param);
                sw.Flush();
                Console.WriteLine(cmd + " " + param);
            }
        }

        string ExtractNick(string input)
        {
            string[] name = input.Split('!');
            string nick = name[0].Trim(':');
            return nick;
        }

        bool NickServVerify(string input)
        {
            sendData("WHOIS", input);
            string[] replyParts;
            for (int counter = 0; counter < 16; counter++)
            {
                string reply = "";
                while (true)
                {
                    try
                    {
                        reply = sr.ReadLine();
                        break;
                    }
                    catch { }
                }
                Console.WriteLine(reply);
                replyParts = reply.Split(' ');
                if (replyParts.Length > 1)
                {
                    if (replyParts[1] == "318")
                        break;
                    else if (replyParts[1] == "330")
                    {
                        Console.WriteLine(input + " is logged in as " + replyParts[4]);
                        if (replyParts[4] == "Japa")
                            return true;
                        else
                            return false;
                    }
                }
                else
                    return false;
            }
            return false;
        }

        string Decide(string param)
        {
            if (param.Length <= 0)
                return "There's nothing to decide.";
            string[] options = param.Split(',');
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = options[i].Trim(' ');
            }
            Random generator = new Random();
            return "You should go for: " + options[generator.Next(options.Length)] + ".";
        }

        string NwodRoll(string param, string name)
        {
            string[] paramBits = param.Split(' ');
            int numdice;
            string output = "\u0002" + name + "\u0002" + " rolled: ";
            int awesomes = 0;
            int successes = 0;
            int failures = 0;
            int botches = 0;
            int threshold;
            if (paramBits.Length > 0)
            {
                if (Int32.TryParse(paramBits[0], out numdice))
                {
                    if (paramBits.Length > 1 && Int32.TryParse(paramBits[1], out threshold))
                    {
                    }
                    else
                        threshold = 10;
                    if (threshold < 5)
                        return "Threshold is entirely too silly. Try a bigger number.";
                    Random generator = new Random();
                    while (numdice > 0)
                    {
                        for (int i = 0; i < numdice; i++)
                        {
                            int die = generator.Next(10);
                            die++; //to make it 1-6 instead of 0-5
                            output += die;
                            if (i < numdice - 1)
                                output += ", ";
                            if (die >= threshold)
                                awesomes++;
                            if (die >= 8)
                                successes++;
                            else if (die == 1)
                                botches++;
                            else
                                failures++;
                        }
                        numdice = awesomes;
                        if (awesomes > 0)
                        {
                            output = output + "; Successes: " + successes + ", Rerolling " + awesomes + ": ";
                            awesomes = 0;
                        }
                        else if (successes > 0)
                            output = output + "; Successes: " + "\u0002" + successes + "\u0002";
                        else if (botches > 0)
                            output = output + "; Successes: " + "\u0002" + (-botches) + "\u0002";
                        else
                            output = output + "; Successes: \u00020\u0002";
                    }
                    return output;
                }
            }
            return "Invalid parameters. Usage \"!nwod <dice> [Reroll Threshold]\"";
        }

        string ShadowRoll(string param, string name)
        {
            int numdice;
            string output = "\u0002" + name + "\u0002" + " rolled: ";
            int successes = 0;
            int failures = 0;
            int botches = 0;
            if (Int32.TryParse(param, out numdice))
            {
                Random generator = new Random();
                for (int i = 0; i < numdice; i++)
                {
                    int die = generator.Next(6);
                    die++; //to make it 1-6 instead of 0-5
                    output += die;
                    if (i < numdice - 1)
                        output += ", ";
                    if (die >= 5)
                        successes++;
                    else if (die == 1)
                        botches++;
                    else
                        failures++;
                }
                if (botches > (numdice / 2))
                {
                    if (successes > 0)
                        output = output + "; Successes: " + "\u0002" + successes + "\u0002" + ", \u0002GLITCH!\u0002";
                    else
                        output = output + "; \u0002CRITICAL GLITCH!\u0002";
                }
                else
                {
                    if (successes > 0)
                        output = output + "; Successes: " + "\u0002" + successes + "\u0002";
                    else
                        output = output + "; \u0002Total Failure\u0002";
                }
                return output;
            }
            else
                return "Invalid parameters. Usage \"!sr <dice>\"";
        }

        string GetSender(string[] message)
        {
            if (message[2][0] == '#')
                return message[2];
            else
                return ExtractNick(message[0]);
        }

        string Tell(string param)
        {
            string[] split = param.Split(' ');
            string returnString = "";
            returnString += split[0] + " :";
            for (int i = 1; i < split.Length; i++)
            {
                returnString += split[i];
                returnString += " ";
            }
            return returnString;
        }
        string Do(string param)
        {
            string[] split = param.Split(' ');
            string returnString = "";
            returnString += split[0] + " :\u0001ACTION ";
            for (int i = 1; i < split.Length; i++)
            {
                returnString += split[i];
                returnString += " ";
            }
            returnString += "\u0001";
            return returnString;
        }

        public bool IRCWork()
        {
            pinger.Restart();
            bool waitingForPing = false;
            string[] ex;
            string data;
            bool shouldRun = true;
            while (shouldRun)
            {
                try
                {
                    data = sr.ReadLine();
                    Console.WriteLine(data);
                    char[] charSeparator = new char[] { ' ' };
                    ex = data.Split(charSeparator, 5);

                    if (ex[0] == "PING")
                    {
                        sendData("PONG", ex[1]);
                    }
                    if (ex[1] == "PONG")
                    {
                        long ticks;
                        if (Int64.TryParse(ex[3].TrimStart(':'), out ticks))
                        {
                            Console.WriteLine(TimeSpan.FromTicks(DateTime.Now.Ticks - ticks).TotalSeconds + " second lag");
                        }
                        else
                        {
                            Console.WriteLine("Could not understand " + ex[3].TrimStart(':'));
                        }
                        pinger.Restart();
                        waitingForPing = false;
                    }

                    if (ex.Length > 3) //is the command received long enough to be a bot command?
                    {
                        string command = ex[3]; //grab the command sent
                        string param = "";

                        for (int i = 4; i < ex.Length; i++)
                        {
                            param += ex[i];
                            param += " ";
                            if (param[0] == ':')
                                break;
                        }
                        switch (command.ToLower())
                        {
                            case ":!join":
                                if (NickServVerify(ExtractNick(ex[0])))
                                    sendData("JOIN", param); //if the command is !join send the "JOIN" command to the server with the parameters set by the user
                                break;
                            case ":!say":
                                if (NickServVerify(ExtractNick(ex[0])))
                                    sendData("PRIVMSG", GetSender(ex) + " " + ":" + param); //if the command is !say, send a message to the chan (ex[2]) followed by the actual message (ex[4]).
                                break;
                            case ":!quit":
                                if (NickServVerify(ExtractNick(ex[0])))
                                {
                                    sendData("QUIT", ":" + param); //if the command is quit, send the QUIT command to the server with a quit message
                                    shouldRun = false; //turn shouldRun to false - the server will stop sending us data so trying to read it will not work and result in an error. This stops the loop from running and we will close off the connections properly
                                }
                                break;
                            case ":!sr":
                                sendData("PRIVMSG", GetSender(ex) + " " + ":" + ShadowRoll(param, ExtractNick(ex[0]))); //if the command is !say, send a message to the chan (ex[2]) followed by the actual message (ex[4]).
                                break;
                            case ":!nwod":
                                sendData("PRIVMSG", GetSender(ex) + " " + ":" + NwodRoll(param, ExtractNick(ex[0]))); //if the command is !say, send a message to the chan (ex[2]) followed by the actual message (ex[4]).
                                break;
                            case ":!part":
                                sendData("PART", GetSender(ex));
                                break;
                            case ":!decide":
                                sendData("PRIVMSG", GetSender(ex) + " " + ":" + Decide(param)); //if the command is !say, send a message to the chan (ex[2]) followed by the actual message (ex[4]).
                                break;
                            case ":!tell":
                                if (NickServVerify(ExtractNick(ex[0])))
                                    sendData("PRIVMSG", Tell(param));
                                break;
                            case ":!do":
                                if (NickServVerify(ExtractNick(ex[0])))
                                    sendData("PRIVMSG", Do(param));
                                break;
                        }
                    }
                }
                catch
                {
                }
                if ((pinger.Elapsed > TimeSpan.FromSeconds(30)) && !waitingForPing)
                {
                    sendData("PING", DateTime.Now.Ticks.ToString());
                    waitingForPing = true;
                }
                if ((pinger.Elapsed > TimeSpan.FromMinutes(2)) && waitingForPing)
                {
                    Console.WriteLine("Connection Timeout.");
                    return false;
                }
            }
            return true;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            IRCConfig conf = new IRCConfig();
            conf.name = "ShadowBot";
            conf.nick = "ShadowBot";
            conf.port = 6667;
            conf.server = "irc.darkmyst.org";
            new IRCBot(conf);
            Console.WriteLine("Bot quit/crashed");
            Console.ReadLine();
        }
    }
}
