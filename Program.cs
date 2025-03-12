using System;
using System.Collections.Generic;
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

            while (true)
            {
                try
                {
                    inst = cpu.FetchInstruction();
                    cpu.ExecuteInstruction(inst);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
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

                default:
                    throw new InvalidOperationException($"Opcode not supported: 0x{inst:X4}");
            }
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
