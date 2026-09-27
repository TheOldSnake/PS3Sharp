using NAudio.Wave;
using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory_Manager;
using PS3Sharp.OS.LV2;
using PS3Sharp.OS.Syscall;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    public static class CellAudio
    {
        public const uint AUDIO_PORT_COUNT = 8;
        public const uint AUDIO_MAX_BLOCK_COUNT = 32;
        public const uint AUDIO_MAX_CHANNELS_COUNT = 8;
        public const uint CELL_AUDIO_BLOCK_SAMPLES = 256;

        public const uint CELL_AUDIO_ERROR_ALREADY_INIT = 0x80310701;
        public const uint CELL_AUDIO_ERROR_NOT_INIT = 0x80310703;
        public const uint CELL_AUDIO_ERROR_PORT_FULL = 0x80310705;
        public const uint CELL_AUDIO_ERROR_PORT_NOT_RUN = 0x80310708;
        public const uint CELL_AUDIO_ERROR_PARAM = 0x80310704;

        public const uint CELL_AUDIO_STATUS_CLOSE = 0x1010;
        public const uint CELL_AUDIO_STATUS_READY = 1;
        public const uint CELL_AUDIO_STATUS_RUN = 2;

        public const ulong EVENT_QUEUE_KEY = 0x1234A1BE1234A1BE; //?

        [StructLayout(LayoutKind.Sequential)]
        public struct CellAudioPortConfig : IPS3Struct
        {
            public BigEndian<uint> ReadAddress;
            public BigEndian<uint> Status;
            public BigEndian<ulong> NumberOfChannels;
            public BigEndian<ulong> NumberOfBlocks;
            public BigEndian<uint> PortSize;
            public BigEndianGuestVoidPtr PortAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CellAudioPortParam : IPS3Struct
        {
            public BigEndian<ulong> NumberOfChannels;
            public BigEndian<ulong> NumberOfBlocks;
            public BigEndian<ulong> Attributes;
            public BigEndian<float> Level;
        }


        public sealed class AudioPort
        { // todo
            public uint Number;
            public uint Status;
            public ulong NumberOfChannels;
            public ulong NumberOfBlocks;
            public GuestVoidPtr BaseAddress;
            public uint Size;

            public uint TotalSamplesPerBlock;
            public uint CurrentBlock;           // To keep track of where to sample from
            public byte[] SampleBuffer;         // 16-bit samples buffer
        }

        private static bool _initialized;
        private static uint _readPositionsAddress;  //An array of 8 ulongs, each entry contains the current block position for the corrosponding port

        private static readonly AudioPort[] _audioPorts = new AudioPort[AUDIO_PORT_COUNT];
        private static readonly Lock _audioThreadLock = new();

        public static ulong CellAudioInit(PPU ppu)
        {
            Console.WriteLine("[CellAudio] cellAudioInit");

            if (_initialized)
            {
                return CELL_AUDIO_ERROR_ALREADY_INIT;
            }

            _initialized = true;
            _readPositionsAddress = MemoryManager.AllocateVirtualMemory(AUDIO_PORT_COUNT * sizeof(ulong)).VirtualAddress;

            for (uint i = 0; i < _audioPorts.Length; i++)
            {
                _audioPorts[i] = new AudioPort
                {
                    Number = i,
                    Status = CELL_AUDIO_STATUS_CLOSE
                };
            }

            InitNAudio();

            //  Thread audioThread = new Thread(() => AudioThreadMain());
            //  audioThread.Start();
            return CELL_OK;
        }

        public static ulong CellAudioPortOpen(PPU ppu)
        {
            if (!_initialized)
            {
                return CELL_AUDIO_ERROR_NOT_INIT;
            }


            GuestPtr<CellAudioPortParam> param_ptr = ppu.ReadArg<GuestPtr<CellAudioPortParam>>(ARG0);
            GuestPtr<uint> portNumber_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG1);

            // Find a closed port to open
            AudioPort? port = null;
            for (int i = 0; i < _audioPorts.Length; i++)
            {
                if (_audioPorts[i].Status == CELL_AUDIO_STATUS_CLOSE)
                {
                    port = _audioPorts[i];
                    break;
                }
            }

            // If all ports are open return an error
            if (port == null)
            {
                return CELL_AUDIO_ERROR_PORT_FULL;
            }

            // Otherwise open the port and set the requested parameters
            port.Status = CELL_AUDIO_STATUS_READY;

            unsafe
            {
                CellAudioPortParam* param = param_ptr.AsNativePtr();
                port.NumberOfChannels = param->NumberOfChannels;
                port.NumberOfBlocks = param->NumberOfBlocks;
            }

            port.TotalSamplesPerBlock = (uint)(CELL_AUDIO_BLOCK_SAMPLES * port.NumberOfChannels);

            // Allocate memory for the port
            // Each channel has a number of blocks, each block has 256 samples (floats)
            port.Size = (uint)(port.NumberOfChannels * port.NumberOfBlocks * CELL_AUDIO_BLOCK_SAMPLES * sizeof(float));
            port.BaseAddress = MemoryManager.AllocateVirtualMemory(port.Size).VirtualAddress;

            // Create the sample buffer for the port
            port.SampleBuffer = new byte[port.TotalSamplesPerBlock * sizeof(short)];

            MemoryManager.Write<uint>(portNumber_ptr, port.Number);

            Console.WriteLine($"[CellAudio] cellAudioPortOpen opened port = {port.Number}");
            return CELL_OK;
        }

        public static ulong CellAudioGetPortConfig(PPU ppu)
        {
            uint port_number = ppu.ReadArg<uint>(ARG0);
            GuestPtr<CellAudioPortConfig> config_ptr = ppu.ReadArg<GuestPtr<CellAudioPortConfig>>(ARG1);

            if (port_number >= AUDIO_PORT_COUNT)
            {
                Console.WriteLine($"[CellAudio] CELL_AUDIO_ERROR_PARAM port = {port_number}");
                return CELL_AUDIO_ERROR_PARAM;
            }



            // always lock this first, then lock memory
            lock (_audioThreadLock)
            {
                AudioPort port = _audioPorts[port_number];

                unsafe
                {
                    CellAudioPortConfig* config = config_ptr.AsNativePtr();
                    config->ReadAddress = _readPositionsAddress + (port_number * sizeof(ulong));
                    config->Status = port.Status;
                    config->NumberOfChannels = port.NumberOfChannels;
                    config->NumberOfBlocks = port.NumberOfBlocks;
                    config->PortSize = port.Size;
                    config->PortAddress = port.BaseAddress;
                }
            }

            Console.WriteLine($"[CellAudio] cellAudioGetPortConfig port = {port_number}");
            return CELL_OK;
        }

        public static ulong CellAudioPortStart(PPU ppu)
        {
            uint port_number = ppu.ReadArg<uint>(ARG0);

            if (port_number >= AUDIO_PORT_COUNT)
            {
                Console.WriteLine($"[CellAudio] CELL_AUDIO_ERROR_PARAM port = {port_number}");
                return CELL_AUDIO_ERROR_PARAM;
            }

            // lock
            lock (_audioThreadLock)
            {
                AudioPort port = _audioPorts[port_number];
                port.Status = CELL_AUDIO_STATUS_RUN;
            }

            Console.WriteLine($"[CellAudio] cellAudioPortStart port = {port_number}");
            return CELL_OK;
        }

        public static ulong CellAudioPortStop(PPU ppu)
        {
            uint port_number = ppu.ReadArg<uint>(ARG0);
            Console.WriteLine($"[CellAudio] cellAudioPortStop port = {port_number}");

            if (port_number >= AUDIO_PORT_COUNT)
            {
                Console.WriteLine($"[CellAudio] CELL_AUDIO_ERROR_PARAM port = {port_number}");
                return CELL_AUDIO_ERROR_PARAM;
            }


            lock (_audioThreadLock)
            {
                AudioPort port = _audioPorts[port_number];

                if (port.Status == CELL_AUDIO_STATUS_RUN)
                {
                    port.Status = CELL_AUDIO_STATUS_READY;
                    return CELL_OK;
                }
                else
                {
                    return CELL_AUDIO_ERROR_PORT_NOT_RUN;

                }
            }
        }

        public static ulong CellAudioQuit(PPU ppu)
        {
            // TODO
            Console.WriteLine("[CellAudio] cellAudioQuit");
            return CELL_OK;
        }

        public static ulong CellAudioCreateNotifyEventQueue(PPU ppu)
        {
            // TODO
            GuestPtr<uint> equeue_id_ptr = ppu.ReadArg<GuestPtr<uint>>(ARG0);
            GuestPtr<ulong> key_ptr = ppu.ReadArg<GuestPtr<ulong>>(ARG1);

            // Create a lv2_event_queue
            LV2EventQueue eventQueue = new(1024, EVENT_QUEUE_KEY, default);

            // Register it in SysEvent
            SysEvent.AddEventQueue(eventQueue.Handle, eventQueue);

            // Write the ID and the key
            MemoryManager.Write<uint>(equeue_id_ptr, eventQueue.Handle);
            MemoryManager.Write<ulong>(key_ptr, EVENT_QUEUE_KEY);

            lock (_audioThreadLock)
            {
                AttachedEventID = eventQueue.Handle;
            }

            Console.WriteLine("[CellAudio] cellAudioCreateNotifyEventQueue");
            return CELL_OK;
        }

        public static ulong CellAudioSetNotifyEventQueue(PPU ppu)
        {
            // TODO
            ulong key = ppu.ReadArg<ulong>(ARG0);
            Console.WriteLine("[CellAudio] cellAudioSetNotifyEventQueue");
            return CELL_OK;
        }


        static WaveFormat waveFormat = new(48000, 2);

        static BufferedWaveProvider provider = new(waveFormat)
        {
            DiscardOnBufferOverflow = true
        };

        static WaveOutEvent output = new();
        static uint AttachedEventID = 0;

        static LV2EventPort.SysEventData AudioEvent = new()
        {
            Data1 = EVENT_QUEUE_KEY,
            Data2 = 0,
            Data3 = 0,
        };

        private static void InitNAudio()
        {
            output.Init(provider);
            output.Play();
        }

        private static void PlayAudio(byte[] samples)
        {
            provider.AddSamples(samples, 0, samples.Length);
        }

        public static byte[] GetSamplesWaveData(float[] samples, int samplesCount)
        {
            byte[] pcm = new byte[samplesCount * 2];
            int sampleIndex = 0,
                pcmIndex = 0;

            while (sampleIndex < samplesCount)
            {
                short outsample = (short)(samples[sampleIndex] * short.MaxValue);
                pcm[pcmIndex] = (byte)(outsample & 0xff);
                pcm[pcmIndex + 1] = (byte)((outsample >> 8) & 0xff);

                sampleIndex++;
                pcmIndex += 2;
            }

            return pcm;
        }

        private static void AudioThreadMain()
        {
            while (true)
            {
                lock (_audioThreadLock)
                {
                    for (int i = 0; i < AUDIO_MAX_CHANNELS_COUNT; i++)
                    {
                        AudioPort port = _audioPorts[i];

                        // Skip ports that are not running
                        if (port.Status != CELL_AUDIO_STATUS_RUN)
                        {
                            continue;
                        }

                        // Fill the sample buffer
                        FillSampleBuffer(port);

                        // Update the current block in the array
                        MemoryManager.Write<ulong>(_readPositionsAddress + ((uint)i * sizeof(ulong)), port.CurrentBlock);

                        // Play the samples
                        PlayAudio(port.SampleBuffer);
                    }
                }

                // Send audio event
                if (AttachedEventID > 0)
                {
                    if (SysEvent.GetEventQueueByID(AttachedEventID, out LV2EventQueue? eventQueue))
                    {
                        eventQueue.Send(AudioEvent);

                    }
                    else
                    {
                        throw new Exception($"[AudioThread] Can't find AttachedEventID = 0x{AttachedEventID:X}");
                    }
                }

                // Sleep for 5.3 ms
                Thread.Sleep(5);
            }
        }

        private static void FillSampleBuffer(AudioPort port)
        {
            uint samplesPerBlock = port.TotalSamplesPerBlock;
            uint bufferIndex = 0;

            // Get the memory address to sample from
            uint blockAddress = port.BaseAddress + (port.CurrentBlock * samplesPerBlock * sizeof(float));
            uint sampleAddress = blockAddress;
            byte[] sampleBuffer = port.SampleBuffer;

            // Sample one block
            for (int i = 0; i < samplesPerBlock; i++)
            {
                // Read a float sample from memory
                float floatSample = MemoryManager.Read<float>(sampleAddress);
                sampleAddress += sizeof(float);

                // Convert from float to short on the fly -- can we use floats directly with NAudio ??
                short shortSample = (short)(floatSample * short.MaxValue);
                sampleBuffer[bufferIndex] = (byte)(shortSample & 0xFF);
                sampleBuffer[bufferIndex + 1] = (byte)((shortSample >> 8) & 0xFF);
                bufferIndex += 2;

                // Debug print
                /*if (sample != 0) {
                    Console.WriteLine($"Sample = {sample}");
                }*/
            }

            // Advance one block
            port.CurrentBlock++;
            port.CurrentBlock %= (uint)port.NumberOfBlocks;
        }
    }
}
