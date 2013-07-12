﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;

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
        TcpClient IRCConnection = null;
        IRCConfig config;
        NetworkStream ns = null;
        StreamReader sr = null;
        StreamWriter sw = null;

        public IRCBot(IRCConfig config)
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
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);

                sendData("USER", config.nick + " Japa " + " Japa" + " :" + config.name);
                sendData("NICK", config.nick);
                IRCWork();
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

        public bool NickServVerify(string input)
        {
            sendData("PRIVMSG", "NickServ" + " " + ":INFO" + " " + input);
            string reply = sr.ReadLine();
            Console.WriteLine(reply);
            string[] replyParts = reply.Split((char)2);
            if (replyParts.Length > 3)
            {
                Console.WriteLine("Detected user registered as " + replyParts[3]);
                if (replyParts[3] == "Japa")
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        string NwodRoll(string param, string name)
        {
            string[] paramBits = param.Split(' ');
            int numdice;
            string output = name + " rolled: ";
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
                            output = output + "; Successes: " + successes;
                        else if (botches > 0)
                            output = output + "; Successes: " + (-botches);
                        else
                            output = output + "; Successes: 0";
                    }
                    return output;
                }
            }
            return "Invalid parameters. Usage \"!nwod <dice> [Reroll Threshold]\"";
        }

        string ShadowRoll(string param, string name)
        {
            int numdice;
            string output = name + " rolled: " ;
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
                    if(i < numdice-1)
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
                        output = output + "; Successes: " + successes + ", GLITCH!";
                    else
                        output = output + "; CRITICAL GLITCH!";
                }
                else
                {
                    if (successes > 0)
                        output = output + "; Successes: " + successes;
                    else
                        output = output + "; Total Failure";
                }
                return output;
            }
            else
                return "Invalid parameters. Usage \"!sr <dice>\"";
        }

        public void IRCWork()
        {
            string[] ex;
            string data;
            bool shouldRun = true;
            while (shouldRun)
            {
                data = sr.ReadLine();
                Console.WriteLine(data);
                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 5);

                if (ex[0] == "PING")
                {
                    sendData("PONG", ex[1]);
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
                    switch (command)
                    {
                        case ":!join":
                            if (NickServVerify(ExtractNick(ex[0])))
                                sendData("JOIN", param); //if the command is !join send the "JOIN" command to the server with the parameters set by the user
                            break;
                        case ":!say":
                            if (NickServVerify(ExtractNick(ex[0])))
                                sendData("PRIVMSG", ex[2] + " " + ":" + param); //if the command is !say, send a message to the chan (ex[2]) followed by the actual message (ex[4]).
                            break;
                        case ":!quit":
                            if (NickServVerify(ExtractNick(ex[0])))
                            {
                                sendData("QUIT", param); //if the command is quit, send the QUIT command to the server with a quit message
                                shouldRun = false; //turn shouldRun to false - the server will stop sending us data so trying to read it will not work and result in an error. This stops the loop from running and we will close off the connections properly
                            }
                            break;
                        case ":!sr":
                            sendData("PRIVMSG", ex[2] + " " + ":" + ShadowRoll(param, ExtractNick(ex[0]))); //if the command is !say, send a message to the chan (ex[2]) followed by the actual message (ex[4]).
                            break;
                        case ":!nwod":
                            sendData("PRIVMSG", ex[2] + " " + ":" + NwodRoll(param, ExtractNick(ex[0]))); //if the command is !say, send a message to the chan (ex[2]) followed by the actual message (ex[4]).
                            break;
                        case ":!part":
                            sendData("PART", ex[2]);
                            break;
                    }
                }
            }
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
