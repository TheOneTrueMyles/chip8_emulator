using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace chip8_emulator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CPU cpu = new CPU();
            ushort inst;
            cpu.LoadROM(@"heartmonitor/heart_monitor.ch8");

            while (cpu.PC < CPU.RAM_SIZE)
            {
                inst = cpu.FetchInstruction();
                //Console.WriteLine($"0x{inst:X4}");
            }
        }
    }

    public class CPU
    {
        public static readonly ushort START_ADDRESS = 0x0200;
        public static readonly ushort RAM_SIZE = 4096;
        
        public byte[] RAM = new byte[RAM_SIZE];
        public byte[] Display = new byte[64 * 32];
        public byte[] V = new byte[16];
        public ushort I = 0;
        public ushort PC = 0;
        public byte SoundTimer = 0;
        public byte DelayTimer = 0;
        public Stack<ushort> Stack = new Stack<ushort>();

        public CPU()
        {
            RAM = new byte[RAM_SIZE];
            PC = START_ADDRESS;
        }

        public void LoadROM(string rom)
        {
            FileStream s = new FileStream(rom, FileMode.Open);
            while (s.Position < s.Length)
            {
                RAM[START_ADDRESS + s.Position] = (byte) s.ReadByte();
            }
        }

        public ushort FetchInstruction()
        {
            ushort inst = (ushort)((RAM[PC] << 8) | RAM[PC+1]);
            PC += 2;
            return inst;
        }

        #region Debug Methods
        
        public void PrintRAM()
        {
            for (int i = 0; i < RAM.Length; i++)
            {
                if (i % 4 == 0) Console.Write($"{i:X4}: ");
                Console.Write($"{RAM[i]:X2} ");
                if (i % 4 == 3) Console.WriteLine();
            }
        }

        #endregion

    }

}
