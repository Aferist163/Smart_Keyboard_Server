﻿using System;
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
            { "red", 0xFF0000FF },
            { "orange", 0xFF007CFF },
            { "yellow", 0xFF00FFFF },
            { "light green", 0xFF2CFFC2 },
            { "blue", 0xFFFF0000 },
            { "light blue", 0xFFFFAA3D },
            { "blue_lighGreen", 0xFFff9e00 },
            { "green", 0xFF00FF00 },
            { "purple", 0xFFFF007D },
            { "purple_pink", 0xFFC800FF },
            { "pink", 0xFFFF00ED },
            { "pink_red", 0xFFFF008A },
            { "black" , 0xFF000000 }
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
                        SetKeyboardColor("white", 100, 100);

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
                Console.WriteLine("🚀 Сервер запущен...");

                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;

                    if (request.HttpMethod == "GET" && request.QueryString["value"] != null && request.QueryString["sliderValue"] != null && request.QueryString["sliderValueWhite"] != null)
                    {
                        string color = request.QueryString["value"];
                        string sliderValueStr = request.QueryString["sliderValue"];
                        string sliderValueWhiteStr = request.QueryString["sliderValueWhite"];

                        if (!double.TryParse(sliderValueStr, out double sliderValue))
                        {
                            sliderValue = 100; 
                        }

                        if (!double.TryParse(sliderValueWhiteStr, out double sliderValueWhite))
                        {
                            sliderValueWhite = 100;
                        }

                        int brightness = Clamp((int)sliderValue, 0, 100);
                        int whiteBalance = Clamp((int)sliderValueWhite, 0, 100);
                        SetKeyboardColor(color, brightness, whiteBalance);

                        string responseString = $"Клавиатура окрашена в {color} с яркостью {brightness}% и балансом белого {whiteBalance}";
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                        context.Response.OutputStream.Close();
                    }
                }
            }
        }

        static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        static void SetKeyboardColor(string colorName, int brightness, int whiteBalance)
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
                color = ApplyWhiteBalance(color, whiteBalance);
                color = ApplyBrightness(color, brightness);

                foreach (IAuraRgbLight light in keyboard.Lights)
                {
                    light.Color = color;
                }
                keyboard.Apply();

                Console.WriteLine($"✅ Клавиатура окрашена в {colorName} с яркостью {brightness}% и балансом белого {whiteBalance}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при установке цвета: {ex.Message}");
            }
        }

        static uint ApplyWhiteBalance(uint color, int whiteBalance)
        {
            byte r = (byte)((color >> 16) & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte b = (byte)(color & 0xFF);

            byte whiteR = (byte)(255 * whiteBalance / 100);
            byte whiteG = (byte)(255 * whiteBalance / 100);
            byte whiteB = (byte)(255 * whiteBalance / 100);

            r = (byte)Math.Min(255, r + whiteR);
            g = (byte)Math.Min(255, g + whiteG);
            b = (byte)Math.Min(255, b + whiteB);

            return (uint)((r << 16) | (g << 8) | b);
        }

        static uint ApplyBrightness(uint color, int brightness)
        {
            byte r = (byte)((color >> 16) & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte b = (byte)(color & 0xFF);

            r = (byte)(r * brightness / 100);
            g = (byte)(g * brightness / 100);
            b = (byte)(b * brightness / 100);

            return (uint)((r << 16) | (g << 8) | b);
        }
    }
}