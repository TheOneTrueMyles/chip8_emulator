using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Stopwatch cycle = new Stopwatch();

            while (true)
            {
                cycle.Restart();
                try
                {
                    inst = cpu.FetchInstruction();
                    cpu.ExecuteInstruction(inst);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                cpu.DrawDisplay();

                while (cycle.ElapsedTicks < (TimeSpan.TicksPerSecond / 500)) { 
                    // do nothing
                }
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
        public Random rnd;

        public CPU()
        {
            RAM = new byte[RAM_SIZE];
            PC = START_ADDRESS;
            rnd = new Random();
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

        public void ExecuteInstruction(ushort inst)
        {
            ushort nnn = (ushort)(inst & 0xFFF);
            byte n = (byte)(inst & 0xF);
            byte x = (byte)((inst & 0x0F00) >> 8);
            byte y = (byte)((inst & 0x00F0) >> 4);
            byte kk = (byte)(inst & 0xFF);

            switch ((inst & 0xF000) >> 12)
            {
                case 0x0:
                    if (inst == 0x00E0)
                    {
                        // CLS
                        Display = new byte[64 * 32];
                    }
                    else if (inst == 0x00EE)
                    {
                        // RET
                        PC = Stack.Pop();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Opcode not supported: 0x{inst:X4}");
                    }
                    break;

                case 0x1:
                    // JMP nnn
                    PC = nnn;
                    break;

                case 0x2:
                    // CALL nnn
                    Stack.Push(PC);
                    PC = nnn;
                    break;

                case 0x3:
                    // SE Vx, kk
                    if (V[x] == kk)
                        PC += 2;
                    break;

                case 0x4:
                    // SNE Vx, kk
                    if (V[x] != kk)
                        PC += 2;
                    break;

                case 0x5:
                    // SE Vx, Vy
                    if (V[x] == V[y])
                        PC += 2;
                    break;

                case 0x6:
                    // LD Vx, kk
                    V[x] = kk;
                    break;

                case 0x7:
                    // ADD Vx, kk
                    V[x] += kk;
                    break;

                case 0x8:
                    switch (n)
                    {
                        case 0x0:
                            // LD Vx, Vy
                            V[x] = V[y];
                            break;

                        case 0x1:
                            // OR Vx, Vy
                            V[x] |= V[y];
                            break;

                        case 0x2:
                            // AND Vx, Vy
                            V[x] &= V[y];
                            break;

                        case 0x3:
                            // XOR Vx, Vy
                            V[x] ^= V[y];
                            break;

                        case 0x4:
                            // ADD Vx, Vy
                            // VF = carry
                            V[x] += V[y];
                            V[0xF] = (byte)(V[x] < V[y] ? 1 : 0);
                            break;

                        case 0x5:
                            // SUB Vx, Vy
                            // VF = NOT borrow
                            V[0xF] = (byte)(V[x] > V[y] ? 1 : 0);
                            V[x] -= V[y];
                            break;

                        case 0x6:
                            // SHR Vx
                            // VF = shifted out bit
                            V[0xF] = (byte)(V[x] & 0x1);
                            V[x] >>= 1;
                            break;

                        case 0x7:
                            // SUBN Vx, Vy
                            // VF = NOT borrow
                            V[0xF] = (byte)(V[y] > V[x] ? 1 : 0);
                            V[x] = (byte)(V[y] - V[x]);
                            break;

                        case 0xE:
                            // SHL Vx
                            // VF = shifted out bit
                            V[0xF] = (byte)((V[x] & 0x80) >> 7);
                            V[x] <<= 1;
                            break;

                        default:
                            throw new InvalidOperationException($"Opcode not supported: 0x{inst:X4}");
                    }
                    break;

                case 0x9:
                    // SNE Vx, Vy
                    if (V[x] != V[y])
                        PC += 2;
                    break;

                case 0xA:
                    // LD I, nnn
                    I = nnn;
                    break;

                case 0xB:
                    // JP V0, nnn
                    PC = (ushort)(nnn + V[0]);
                    break;

                case 0xC:
                    // RND Vx, kk
                    V[x] = (byte)(rnd.Next(0, 255) & kk);
                    break;

                case 0xD:
                    // DRW Vx, Vy, n

                    // Grab n-byte sprite from address I and draw at coordinate (Vx, Vy) 
                    // Display is updated by XORing pixels, set VF if there's a collision
                    V[0xF] = 0;
                    for (byte i = 0; i < n; i++)
                    {
                        byte pixelRow = RAM[I + i];
                        for (byte j = 0; j < 8; j++)
                        {
                            byte pixel = (byte)((pixelRow >> (7 - j)) & 0x1);
                            byte row = (byte)(V[y] + i);
                            byte column = (byte)(V[x] + j);
                            byte oldPixel = Display[row * 64 + column];
                            if (pixel == oldPixel) // collision
                                V[0xF] = 1;
                            Display[row * 64 + column] ^= pixel;
                        }
                    }
                    //DrawDisplay();
                    break;
                    
                default:
                    throw new InvalidOperationException($"Opcode not supported: 0x{inst:X4}");
            }
        }

        public void DrawDisplay()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            string display = "";
            for(var i = 0; i < 32; i++)
            {
                for (var j = 0; j < 64; j++)
                {
                    byte pixel = Display[i * 64 + j];
                    display += pixel == 1 ? "*" : " ";
                }
                display += "\r\n";
            }
            Console.Write(display);
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
