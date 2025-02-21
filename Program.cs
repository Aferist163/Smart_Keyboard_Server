using System;
using System.Net;
using AuraServiceLib;

namespace KeyboardLightingServer
{
    class Program
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("Окрашиваем клавиатуру в белый цвет...");
                SetKeyboardColor("white");
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://192.168.0.166:5000/color/");

                listener.Start();
                Console.WriteLine("Сервер запущен на порту 5000...");

                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    if (request.HttpMethod == "GET" && request.QueryString["value"] != null)
                    {
                        string color = request.QueryString["value"];
                        SetKeyboardColor(color);

                        HttpListenerResponse response = context.Response;
                        string responseString = $"Клавиатура окрашена в {color}";
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                        response.OutputStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске сервера: {ex.Message}");
            }
        }

        static void SetKeyboardColor(string colorName)
        {
            uint color = 0x00FFFFFF; 

            switch (colorName.ToLower())
            {
                case "blue":
                    color = 0x000000FF;
                    break;
                case "red":
                    color = 0x00FF0000;
                    break;
                case "green":
                    color = 0x0000FF00;
                    break;
                case "yellow":
                    color = 0x00FFFF00;
                    break;
                case "white":
                    color = 0x00FFFFFF;
                    break;
            }

            try
            {
                Console.WriteLine("Запуск Aura SDK...");
                IAuraSdk sdk = new AuraSdk();
                sdk.SwitchMode();
                IAuraSyncDeviceCollection devices = sdk.Enumerate(0);

                Console.WriteLine($"Найдено устройств: {devices.Count}");

                foreach (IAuraSyncDevice dev in devices)
                {
                    Console.WriteLine($"Устройство: {dev.Name} | Тип: {dev.Type}");

                    if (dev.Type == 0x80000) 
                    {
                        Console.WriteLine("Клавиатура найдена, применяем цвет...");
                        foreach (IAuraRgbLight light in dev.Lights)
                        {
                            light.Color = color;
                        }
                        dev.Apply();
                        dev.Apply();
                        Console.WriteLine($"Клавиатура окрашена в {colorName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при установке цвета клавиатуры: {ex.Message}");
            }
        }
    }
}