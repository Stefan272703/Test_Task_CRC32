using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

/*
  Класс для реализации контрольной суммы на CRC32
 */

namespace Test_Task
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

        public static uint CalculateCRC32(byte[] bytesOfFile)
        {
            uint crc = 0xFFFFFFFF; // начальное значение

            foreach (byte b in bytesOfFile) // смотрим каждый байт опрделенного файла
            {
                byte index = ((byte)((crc & 0xFFFFFFFF) ^ b)); //XOR
                crc = (crc >> 8) ^ Table[index]; // Смещение
            }
            return crc ^ 0xFFFFFFFF; // Финальное применение XOR
        }

    }
    
}
