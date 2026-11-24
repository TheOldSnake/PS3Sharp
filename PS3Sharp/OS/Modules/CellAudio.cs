using NAudio.Wave;
using PS3Sharp.Cell;
using PS3Sharp.Cell.PPU;
using PS3Sharp.Common;
using PS3Sharp.Memory;
using PS3Sharp.OS.LV2;
using PS3Sharp.OS.Syscall;
using System.Runtime.InteropServices;
using static PS3Sharp.Cell.Arguments;
using static PS3Sharp.Cell.ErrorCode;

namespace PS3Sharp.OS.Modules
{
    internal class CellAudio
    {
        internal const uint AudioPortCount = 8;
        internal const uint AudioMaxBlockCount = 32;
        internal const uint AudioMaxChannelsCount = 8;
        internal const uint CellAudioBlockSamples = 256;

        internal const uint CellAudioErrorAlreadyInit = 0x80310701;
        internal const uint CellAudioErrorNotInit = 0x80310703;
        internal const uint CellAudioErrorPortFull = 0x80310705;
        internal const uint CellAudioErrorPortNotRun = 0x80310708;
        internal const uint CellAudioErrorParam = 0x80310704;

        internal const uint CellAudioStatusClose = 0x1010;
        internal const uint CellAudioStatusReady = 1;
        internal const uint CellAudioStatusRun = 2;

        internal const ulong EventQueueKey = 0x1234A1BE1234A1BE; //?

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellAudioPortConfig : IPS3Struct
        {
            internal BigEndian<uint> ReadAddress;
            internal BigEndian<uint> Status;
            internal BigEndian<ulong> NumberOfChannels;
            internal BigEndian<ulong> NumberOfBlocks;
            internal BigEndian<uint> PortSize;
            internal BigEndianGuestVoidPtr PortAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CellAudioPortParam : IPS3Struct
        {
            internal BigEndian<ulong> NumberOfChannels;
            internal BigEndian<ulong> NumberOfBlocks;
            internal BigEndian<ulong> Attributes;
            internal BigEndian<float> Level;
        }

        // todo
        internal sealed class AudioPort
        {
            internal uint Number;
            internal uint Status;
            internal ulong NumberOfChannels;
            internal ulong NumberOfBlocks;
            internal GuestVoidPtr BaseAddress;
            internal uint Size;

            internal uint TotalSamplesPerBlock;
            internal uint CurrentBlock;           // To keep track of where to sample from
            internal byte[] SampleBuffer;         // 16-bit samples buffer
        }

        private readonly MemoryManager _memoryManager;
        private readonly SysEvent _sysEvent;
        private bool _initialized;
        private uint _readPositionsAddress;  //An array of 8 ulongs, each entry contains the current block position for the corrosponding port
        private uint _attachedEventID = 0;

        private readonly AudioPort[] _audioPorts = new AudioPort[AudioPortCount];
        private readonly Lock _audioThreadLock = new();

        private static readonly WaveFormat _waveFormat = new(48000, 2);

        private readonly BufferedWaveProvider _provider = new(_waveFormat)
        {
            DiscardOnBufferOverflow = true
        };

        private readonly WaveOut _output = new();
        private readonly LV2EventPort.SysEventData AudioEvent = new()
        {
            Data1 = EventQueueKey,
            Data2 = 0,
            Data3 = 0,
        };

        internal CellAudio(MemoryManager memoryManager, SysEvent sysEvent)
        {
            _memoryManager = memoryManager;
            _sysEvent = sysEvent;
        }

        internal ulong CellAudioInit(PPUThread ppu)
        {
            Console.WriteLine("[CellAudio] cellAudioInit");

            if (_initialized)
            {
                return CellAudioErrorAlreadyInit;
            }

            _initialized = true;
            _readPositionsAddress = _memoryManager.Allocator.AllocateVirtualMemory(AudioPortCount * sizeof(ulong)).VirtualAddress;

            for (uint i = 0; i < _audioPorts.Length; i++)
            {
                _audioPorts[i] = new AudioPort
                {
                    Number = i,
                    Status = CellAudioStatusClose
                };
            }

            InitNAudio();

            //  Thread audioThread = new Thread(() => AudioThreadMain());
            //  audioThread.Start();
            return CellOk;
        }

        internal ulong CellAudioPortOpen(PPUThread ppu)
        {
            if (!_initialized)
            {
                return CellAudioErrorNotInit;
            }


            GuestPtr<CellAudioPortParam> param_ptr = ppu.ReadArg<GuestPtr<CellAudioPortParam>>(Arg0);
            GuestPtr<BigEndian<uint>> portNumber_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg1);

            // Find a closed port to open
            AudioPort? port = null;
            for (int i = 0; i < _audioPorts.Length; i++)
            {
                if (_audioPorts[i].Status == CellAudioStatusClose)
                {
                    port = _audioPorts[i];
                    break;
                }
            }

            // If all ports are open return an error
            if (port == null)
            {
                return CellAudioErrorPortFull;
            }

            // Otherwise open the port and set the requested parameters
            port.Status = CellAudioStatusReady;

            CellAudioPortParam param = _memoryManager.Read<CellAudioPortParam>(param_ptr);
            port.NumberOfChannels = param.NumberOfChannels;
            port.NumberOfBlocks = param.NumberOfBlocks;

            port.TotalSamplesPerBlock = (uint)(CellAudioBlockSamples * port.NumberOfChannels);

            // Allocate memory for the port
            // Each channel has a number of blocks, each block has 256 samples (floats)
            port.Size = (uint)(port.NumberOfChannels * port.NumberOfBlocks * CellAudioBlockSamples * sizeof(float));
            port.BaseAddress = _memoryManager.Allocator.AllocateVirtualMemory(port.Size).VirtualAddress;

            // Create the sample buffer for the port
            port.SampleBuffer = new byte[port.TotalSamplesPerBlock * sizeof(short)];

            _memoryManager.Write<BigEndian<uint>>(portNumber_ptr, port.Number);

            Console.WriteLine($"[CellAudio] cellAudioPortOpen opened port = {port.Number}");
            return CellOk;
        }

        internal ulong CellAudioGetPortConfig(PPUThread ppu)
        {
            uint port_number = ppu.ReadArg<uint>(Arg0);
            GuestPtr<CellAudioPortConfig> config_ptr = ppu.ReadArg<GuestPtr<CellAudioPortConfig>>(Arg1);

            if (port_number >= AudioPortCount)
            {
                Console.WriteLine($"[CellAudio] CELL_AUDIO_ERROR_PARAM port = {port_number}");
                return CellAudioErrorParam;
            }



            // always lock this first, then lock memory
            lock (_audioThreadLock)
            {
                AudioPort port = _audioPorts[port_number];

                CellAudioPortConfig config = new()
                {
                    ReadAddress = _readPositionsAddress + (port_number * sizeof(ulong)),
                    Status = port.Status,
                    NumberOfChannels = port.NumberOfChannels,
                    NumberOfBlocks = port.NumberOfBlocks,
                    PortSize = port.Size,
                    PortAddress = port.BaseAddress,
                };

                _memoryManager.Write(config_ptr, config);
            }

            // Console.WriteLine($"[CellAudio] cellAudioGetPortConfig port = {port_number}");
            return CellOk;
        }

        internal ulong CellAudioPortStart(PPUThread ppu)
        {
            uint port_number = ppu.ReadArg<uint>(Arg0);

            if (port_number >= AudioPortCount)
            {
                Console.WriteLine($"[CellAudio] CELL_AUDIO_ERROR_PARAM port = {port_number}");
                return CellAudioErrorParam;
            }

            // lock
            lock (_audioThreadLock)
            {
                AudioPort port = _audioPorts[port_number];
                port.Status = CellAudioStatusRun;
            }

            Console.WriteLine($"[CellAudio] cellAudioPortStart port = {port_number}");
            return CellOk;
        }

        internal ulong CellAudioPortStop(PPUThread ppu)
        {
            uint port_number = ppu.ReadArg<uint>(Arg0);
            Console.WriteLine($"[CellAudio] cellAudioPortStop port = {port_number}");

            if (port_number >= AudioPortCount)
            {
                Console.WriteLine($"[CellAudio] CELL_AUDIO_ERROR_PARAM port = {port_number}");
                return CellAudioErrorParam;
            }


            lock (_audioThreadLock)
            {
                AudioPort port = _audioPorts[port_number];

                if (port.Status == CellAudioStatusRun)
                {
                    port.Status = CellAudioStatusReady;
                    return CellOk;
                }
                else
                {
                    return CellAudioErrorPortNotRun;

                }
            }
        }

        internal ulong CellAudioQuit(PPUThread ppu)
        {
            // TODO
            Console.WriteLine("[CellAudio] cellAudioQuit");
            return CellOk;
        }

        internal ulong CellAudioCreateNotifyEventQueue(PPUThread ppu)
        {
            // TODO
            GuestPtr<BigEndian<uint>> equeue_id_ptr = ppu.ReadArg<GuestPtr<BigEndian<uint>>>(Arg0);
            GuestPtr<BigEndian<ulong>> key_ptr = ppu.ReadArg<GuestPtr<BigEndian<ulong>>>(Arg1);

            // Create a lv2_event_queue
            LV2EventQueue eventQueue = new(1024, EventQueueKey, default, _memoryManager);

            // Register it in SysEvent
            _sysEvent.AddEventQueue(eventQueue.Handle, eventQueue);

            // Write the ID and the key
            _memoryManager.Write<BigEndian<uint>>(equeue_id_ptr, eventQueue.Handle);
            _memoryManager.Write<BigEndian<ulong>>(key_ptr, EventQueueKey);

            lock (_audioThreadLock)
            {
                _attachedEventID = eventQueue.Handle;
            }

            Console.WriteLine("[CellAudio] cellAudioCreateNotifyEventQueue");
            return CellOk;
        }

        internal ulong CellAudioSetNotifyEventQueue(PPUThread ppu)
        {
            // TODO
            ulong key = ppu.ReadArg<ulong>(Arg0);
            Console.WriteLine("[CellAudio] cellAudioSetNotifyEventQueue");
            return CellOk;
        }

        private void InitNAudio()
        {
             _output.Init(_provider);
             _output.Play();
        }

        private void PlayAudio(byte[] samples)
        {
            _provider.AddSamples(samples, 0, samples.Length);
        }

        private byte[] GetSamplesWaveData(float[] samples, int samplesCount)
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

        private void AudioThreadMain()
        {
            while (true)
            {
                lock (_audioThreadLock)
                {
                    for (int i = 0; i < AudioMaxChannelsCount; i++)
                    {
                        AudioPort port = _audioPorts[i];

                        // Skip ports that are not running
                        if (port.Status != CellAudioStatusRun)
                        {
                            continue;
                        }

                        // Fill the sample buffer
                        FillSampleBuffer(port);

                        // Update the current block in the array
                        _memoryManager.Write<BigEndian<ulong>>(_readPositionsAddress + ((uint)i * sizeof(ulong)), port.CurrentBlock);

                        // Play the samples
                        PlayAudio(port.SampleBuffer);
                    }
                }

                // Send audio event
                if (_attachedEventID > 0)
                {
                    if (_sysEvent.TryGetEventQueueByID(_attachedEventID, out LV2EventQueue? eventQueue))
                    {
                        eventQueue.Send(AudioEvent);

                    }
                    else
                    {
                        throw new Exception($"[AudioThread] Can't find AttachedEventID = 0x{_attachedEventID:X}");
                    }
                }

                // Sleep for 5.3 ms
                Thread.Sleep(5);
            }
        }

        private void FillSampleBuffer(AudioPort port)
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
                float floatSample = _memoryManager.Read<BigEndian<float>>(sampleAddress);
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
