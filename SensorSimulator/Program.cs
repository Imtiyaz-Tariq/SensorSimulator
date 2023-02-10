using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SensorSimulator
{
    internal class Program
    {
        //Connection string for device to cloud messaging
        private static readonly string connectionString_IoTHub = "HostName=SensorHub13.azure-devices.net;DeviceId=SensorsSimulator;SharedAccessKey=mj0pkfH05/SIdqqf2vU4e47D2+g0qMu659JMdYYwHbo=";

        //Device Client
        static DeviceClient truckDeviceClient;

        //Random Generator
        static Random random = new Random();

        //truck sensor details
        //const string SensorId = "1";
        const int mpd_Flr = 1;
        static DateTime StreamTime = DateTime.Now;
        const double o3 = 45.5;
        const double co = 23.43;
        const double pm25_min = 0.04;// truckTemperature_min = 20;
        const double pm25_max = 950; // truckTemperature_max = 40;
        static double pm25 = 67.6;// truckTemperature = 20;
        const double no2_min = 0.01; // truckLattitude_min = 80;
        const double no2_max = 362; // truckLattitude_max = 120;
        static double no2 = 28.6; //truckLattitude = 80;
        const double so2_min = 0.01;// truckLongitude_min = 80;
        const double so2_max = 194; // truckLongitude_max = 120;
        static double so2 = 14.5; // truckLongitude = 80;
        static void Main(string[] args)
        {
            var cts = new CancellationTokenSource();
            Console.WriteLine("Press CTRL+C to stop the simulation");
            Console.CancelKeyPress += (s, e) =>
            {
                Console.WriteLine("Stopping the Application....");
                cts.Cancel();
                e.Cancel = true;
            };

            truckDeviceClient = DeviceClient.CreateFromConnectionString(connectionString_IoTHub);

            SendMessagesToIoTHub(cts.Token);

            Console.ReadLine();
        }
        private static async void SendMessagesToIoTHub(CancellationToken token)
        {
            int SensorId;
            int[] flrs = { 1, 2, 3 };
            while (!token.IsCancellationRequested)
            {
                SensorId = 101;
                foreach (int flr in flrs)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        pm25 = GenerateSensorReading(pm25, pm25_min, pm25_max);
                        no2 = GenerateSensorReading(no2, no2_min, no2_max);
                        so2 = GenerateSensorReading(so2, so2_min, so2_max);

                        var json = CreateJSON(SensorId.ToString(), flr, pm25,o3,co, no2, so2);
                        var message = CreateMessage(json);
                        await truckDeviceClient.SendEventAsync(message);
                        Console.WriteLine($"Sending data at {DateTime.Now} and data : {message}");
                        SensorId += 1;
                    }
                   
                }
                await Task.Delay(5000);
            }
        }

        private static double GenerateSensorReading(double currentValue, double min, double max)
        {
            double percentage = 5; // 5%

            // generate a new value based on the previous supplied value
            // The new value will be calculated to be within the threshold specified by the "percentage" variable from the original number.
            // The value will also always be within the the specified "min" and "max" values.
            double value = currentValue * (1 + ((percentage / 100) * (2 * random.NextDouble() - 1)));

            value = Math.Max(value, min);
            value = Math.Min(value, max);

            return value;
        }

        private static string CreateJSON(string sid,int mpdflr,double pollutant1, double pollutant2, double pollutant3, double pollutant4,double pollutant5)
        {
            var data = new
            {
                sensorId = sid,
                mpd_Flr = mpdflr,
                StreamTime = DateTime.Now,
                pm25 = pollutant1,
                o3 = pollutant2,
                co = pollutant3,
                no2 = pollutant2,
                so2 = pollutant3                   

            };
            return JsonConvert.SerializeObject(data);
        }

        private static Message CreateMessage(string jsonObject)
        {
            var message = new Message(Encoding.ASCII.GetBytes(jsonObject));
            message.Properties.Add("thesholdBreach", "true");

            // MESSAGE CONTENT TYPE
            message.ContentType = "application/json";
            message.ContentEncoding = "UTF-8";

            return message;
        }
    }
}
