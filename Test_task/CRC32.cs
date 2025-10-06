using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Test_Task;

/*
  Класс для реализации контрольной суммы на CRC32
 */

namespace TestTask
{
    public static class CRC32 
    {
        // Реализация через полином 0xEDB88320(согласно CRC32) и применение табличного подхода для эффективного рассчета контрольной суммы
        private const uint Polynom = 0xEDB88320; // Полином 
        private static readonly uint[] Table = new uint[256]; // Таблица 

        static CRC32() // Конструктор
        {
            // Генерация таблицы
            for (uint i = 0; i < 256; ++i) // зполняем таблицу 
            {
                uint crc = i;
                for (uint j = 0; j < 8; ++j)
                {
                    if((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ Polynom;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                    Table[i] = crc;
                }
            }
        }

        public static uint CalculateCRC32(string filepath)
        {
            uint crc = 0xFFFFFFFF; // начальное значение
            byte[] buffer = new byte[4 * 1024 * 1024]; // 8 MB
            int bytesRead;

            // Создание окна ProgressBar
            ProgressBarFiles progressBarFiles = null;
            Application.Current.Dispatcher.Invoke(() => // вызываем ProgressBar в UI-потоке
            { 
                progressBarFiles = new ProgressBarFiles();
                progressBarFiles.TextFileName.Content = filepath;
                progressBarFiles.Show();
            });

            // Запуск чтения файла
            using (FileStream fs = File.OpenRead(filepath))
            {
                long totalBytes = fs.Length; // длина файла
                long bytesProcessed = 0; // байтов прочитано

                while ((bytesRead = fs.Read(buffer,0,buffer.Length)) > 0)
                {
                    bytesProcessed += bytesRead;
                    int progressPercentage = (int)((bytesProcessed * 100) / totalBytes);

                    crc = UpdateCRC32(crc, buffer, bytesRead);

                    Application.Current.Dispatcher.Invoke(() => // заполняем progressBar
                    {
                        progressBarFiles.ProgressBar.Value = progressPercentage;
                    });
                }
            }

            Application.Current.Dispatcher.Invoke(() => // закрываем ProgressBar в UI-потоке
            {
                progressBarFiles.Close();
            });

            return crc ^ 0xFFFFFFFF; // Финальное применение XOR
        }

        private static uint UpdateCRC32(uint crc, byte[] data, int length)
        {
            for (int i = 0; i < length; i++) 
            {
                byte index = (byte)((crc & 0xFF) ^ data[i]);
                crc = (crc >> 8) ^ Table[index];
            }
            return crc;
        }
    }
    
}
