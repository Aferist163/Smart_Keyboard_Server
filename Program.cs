using System;
using System.Net;
using System.Collections.Generic;
using AuraServiceLib;

namespace KeyboardLightingServer
{
    class Program
    {
        private static IAuraSdk sdk;
        private static IAuraSyncDevice keyboard;
        private static readonly Dictionary<string, uint> colorMap = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
        {
            { "blue", 0x000000FF },
            { "red", 0x00FF0000 },
            { "green", 0x0000FF00 },
            { "yellow", 0x00FFFF00 },
            { "white", 0x00FFFFFF }
        };

        static void Main()
        {
            try
            {
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                Console.WriteLine("Инициализация Aura SDK...");

                if (!InitializeAuraSdk())
                {
                    Console.WriteLine("Ошибка: Клавиатура не найдена. Завершение работы.");
                    return;
                }

                Console.WriteLine("Сервер запущен на порту 5000...");
                StartServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Фатальная ошибка: {ex.Message}");
            }
        }

        static bool InitializeAuraSdk()
        {
            try
            {
                sdk = new AuraSdk();
                sdk.SwitchMode();
                IAuraSyncDeviceCollection devices = sdk.Enumerate(0);

                Console.WriteLine($"Найдено устройств: {devices.Count}");

                foreach (IAuraSyncDevice dev in devices)
                {
                    Console.WriteLine($"Устройство: {dev.Name} | Тип: {dev.Type}");

                    if (dev.Type == 0x80000)
                    {
                        keyboard = dev;
                        Console.WriteLine("✅ Клавиатура найдена.");
                        SetKeyboardColor("white");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при инициализации Aura SDK: {ex.Message}");
            }
            return false;
        }

        static void StartServer()
        {
            using (HttpListener listener = new HttpListener())
            {
                listener.Prefixes.Add("http://192.168.0.166:5000/color/");
                listener.Start();

                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    if (request.HttpMethod == "GET" && request.QueryString["value"] != null)
                    {
                        string color = request.QueryString["value"];
                        SetKeyboardColor(color);

                        string responseString = $"Клавиатура окрашена в {color}";
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                    }
                }
            }
        }

        static void SetKeyboardColor(string colorName)
        {
            if (keyboard == null)
            {
                Console.WriteLine("❌ Ошибка: Клавиатура не обнаружена.");
                return;
            }

            if (!colorMap.TryGetValue(colorName, out uint color))
            {
                Console.WriteLine($"⚠️ Неизвестный цвет '{colorName}', используется белый.");
                color = 0x00FFFFFF;
            }

            try
            {
                Console.WriteLine($"🎨 Установка цвета: {colorName}...");

                foreach (IAuraRgbLight light in keyboard.Lights)
                {
                    light.Color = color;
                }
                keyboard.Apply();
                keyboard.Apply();

                Console.WriteLine($"✅ Клавиатура окрашена в {colorName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при установке цвета: {ex.Message}");
            }
        }
    }
}
