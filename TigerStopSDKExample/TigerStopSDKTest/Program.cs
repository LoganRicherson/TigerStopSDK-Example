using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc;
using TigerStopSDKExample;

namespace TigerStopSDKExample
{
    class Program
    {
        static TigerStop_IO io;

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--web")
            {
                // Start Web API mode
                CreateHostBuilder(args).Build().Run();
            }
            else
            {
                // Run as console application
                RunConsoleApp();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        static void RunConsoleApp()
        {
            Console.WriteLine("Ready to connect....");

            Thread.Sleep(2000);

            Start:

            Console.Write("(1) Enter Comport and Baudrate\n(2) Search for Available Connections\n(3) Exit\nSELECT OPTION : ");
            string input = Console.ReadLine();

            try
            {
                switch (input)
                {
                    case "1":
                        string comport;
                        int baud;
                        Console.Write("Enter Comport : ");
                        comport = Console.ReadLine();
                        Console.Write("Enter Baudrate : ");
                        baud = Convert.ToInt32(Console.ReadLine());

                        Console.WriteLine("Connecting to " + comport + "....");

                        io = new TigerStop_IO(baud, comport);

                        if (io.IsOpen)
                        {
                            Console.WriteLine("Successfully connected.");
                            goto MainLoop;
                        }
                        else
                        {
                            Console.WriteLine("Connection failed.");
                            goto Start;
                        }
                    case "2":
                        Console.WriteLine("Searching....");

                        List<KeyValuePair<string, int>> con = TigerStop_IO.Connections();

                        if (con.Count > 0)
                        {
                            foreach (KeyValuePair<string, int> c in con)
                            {
                                Console.WriteLine("Comport : " + c.Key + " | Baudrate : " + c.Value);
                            }
                        }
                        else
                        {
                            Console.WriteLine("No connections were found.");
                        }
                        if (con.Count == 1)
                        {
                            Console.WriteLine("Connecting to " + con[0].Key + "....");

                            io = new TigerStop_IO(con[0].Value, con[0].Key);

                            if (io.IsOpen)
                            {
                                Console.WriteLine("Successfully connected.");
                                goto MainLoop;
                            }
                            else
                            {
                                Console.WriteLine("Connection failed.");
                                goto Start;
                            }
                        }

                        goto Start;
                    case "3":
                        goto Exit;
                    default:
                        Console.WriteLine("Invalid Option.");
                        goto Start;
                }
            }
            catch
            {
                Console.WriteLine("Error Occurred.");
                goto Start;
            }

            MainLoop:

            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine("==================================================");
            Console.WriteLine("                    -READY-                       ");
            Console.WriteLine("==================================================");

            while (true)
            {
                if (InputHandler())
                {
                    break;
                }
            }

            Exit:

            Console.WriteLine("Exiting....");
            Thread.Sleep(3000);
            Environment.Exit(0);
        }

        public static bool InputHandler()
        {
            bool exit = true;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Enter Command : ");
            string[] input = Console.ReadLine().Split(' ');
            Console.ForegroundColor = ConsoleColor.Red;

            switch (input[0])
            {
                case "Exit":
                case "exit":
                case "x":
                    exit = true;
                    break;
                case "Move":
                case "move":
                case "m":
                    if (io.MoveTo(input[1]))
                    {
                        Console.WriteLine("Move Successful");
                    }
                    else
                    {
                        Console.WriteLine("Move Unsuccessful");
                    }

                    exit = false;
                    break;

                default:
                    Console.WriteLine("Invalid Command.\nType 'help' to view list of available commands.\n");
                    exit = false;
                    break;
            }

            return exit;
        }
    }
}

// Startup.cs
public class Startup 
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();  // Register API controllers
        services.AddSingleton<TigerStopService>();  // Register TigerStopService as a singleton
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();  // Enable detailed error page in development
        }

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();  // Map controller routes
        });
    }
}

// TigerStopService.cs
public class TigerStopService
{
    private TigerStop_IO io;
    private const string DefaultComPort = "/dev/ttyUSB0";  // Change as needed
    private const int DefaultBaudRate = 9600;      // Change as needed

    public bool Connect()
    {
        io = new TigerStop_IO(DefaultBaudRate, DefaultComPort);
        return io.IsOpen;
    }

    public bool Move(string position)
    {
        if (io == null || !io.IsOpen)
        {
            throw new InvalidOperationException("Not connected to TigerStop.");
        }
        return io.MoveTo(position);
    }
}

// TigerStopController.cs

[ApiController]
[Route("api/[controller]")]
public class TigerStopController : ControllerBase
{
    private readonly TigerStopService _tigerStopService;

    public TigerStopController(TigerStopService tigerStopService)
    {
        _tigerStopService = tigerStopService;
    }

    [HttpPost("connect")]
    public IActionResult Connect()
    {
        bool isConnected = _tigerStopService.Connect();
        if (isConnected)
        {
            return Ok("Successfully connected");
        }
        return BadRequest("Connection failed");
    }

    [HttpPost("move/{position}")]
    public IActionResult Move(int position)
    {
        try
        {
            if (_tigerStopService.Move(position.ToString()))
            {
                return Ok("Move successful");
            }
            return BadRequest("Move failed");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

