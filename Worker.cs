using Newtonsoft.Json;
using System.Diagnostics;
using System.Text.Json.Nodes;
using WindowsInput;
using WindowsInput.Native;
using static MIDIMapper.Config;

namespace MIDIMapper
{
    public class Worker : BackgroundService
    {
        //private const int MIM_DATA = 0x3C3;

        private static List<OnceModeMidiKeyConfig> _cfg;
        private const int MIDI_CALLBACK_NULL = 0x0000_0000;
        private const int MIDI_IO_STATUS = 0x0000_0020;
        private const int MIDI_CALLBACK_FUNCTION = 0x0003_0000;
        private static InputSimulator inputSimulator = new InputSimulator();
        public enum MidiMessage : int
        {
            MIM_OPEN = 0x3C1,       //��ʾ MIDI �����豸�Ѵ�
            MIM_CLOSE = 0x3C2,      //��ʾ MIDI �����豸�ѹر�
            MIM_DATA = 0x3C3,       //��ʾ�յ�һ�� MIDI ������Ϣ��
            MIM_LONGDATA = 0x3C4,   //��ʾ�յ�һ������Ϣ=SysEx ��������ʵʱ��Ϣ����
            MIM_ERROR = 0x3C5,      //��ʾ������ MIDI �������
            MIM_LONGERROR = 0x3C6   //��ʾ�����˳���Ϣ��SysEx ��������ʵʱ��Ϣ���Ĵ���
        }
        // ����MMEAPI�еĺ�������
        #region MIDI_IN
        [DllImport("winmm.dll")]
        private static extern int midiInOpen(out IntPtr lphMidiIn, int uDeviceID, MidiInCallback dwCallback, IntPtr dwInstance, int dwFlags);

        [DllImport("winmm.dll")]
        private static extern int midiInGetErrorText(int errCode, StringBuilder errMsg, int sizeOfErrMsg);

        [DllImport("winmm.dll")]
        private static extern int midiInStart(IntPtr hMidiIn);

        [DllImport("winmm.dll")]
        private static extern int midiInStop(IntPtr hMidiIn);

        [DllImport("winmm.dll")]
        private static extern int midiInClose(IntPtr hMidiIn);

        [DllImport("winmm.dll")]
        private static extern int midiInGetNumDevs();
        #endregion

        #region MIDI_OUT
        [DllImport("winmm.dll")]
        private static extern uint midiOutOpen(out IntPtr lphMidiOut, uint uDeviceID, IntPtr dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll")]
        private static extern uint midiOutClose(IntPtr hMidiOut);

        [DllImport("winmm.dll")]
        private static extern uint midiOutShortMsg(IntPtr hMidiOut, uint dwMsg);
        #endregion

        private const int MAX_DEVICE_NAME_LEN = 32;
        [StructLayout(LayoutKind.Sequential)]
        struct MIDIINCAPS
        {
            public ushort wMid;           // ������ ID
            public ushort wPid;           // ��Ʒ ID
            public uint vDriverVersion;   // ��������汾
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_DEVICE_NAME_LEN)]
            public string szPname;        // �豸����
            public uint dwSupport;        // �豸֧�ֵ�����
            //public ushort wChannels;      // MIDI ͨ������
            //public ushort wReserved;      // �����ֶ�
            //public ushort wMidEx;         // ��չ������ ID
            //public ushort wPidEx;         // ��չ��Ʒ ID
            //public uint vDriverVersionEx; // ��չ��������汾
            //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            //public string szPnameEx;      // ��չ�豸����
            //public uint dwSupportEx;      // ��չ�豸֧�ֵ�����
        }
        [DllImport("winmm.dll")]
        private static extern int midiInGetDevCaps(int uDeviceID, ref MIDIINCAPS caps, int cbMidiInCaps);

        // MIDI����ص���������
        private delegate void MidiInCallback(IntPtr hMidiIn, MidiMessage wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

        // MIDI����ص�������ʵ��
        private static void MidiInputCallback(IntPtr hMidiIn, MidiMessage wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
        {
            switch (wMsg)
            {
                case MidiMessage.MIM_OPEN:
                    // MIDI �����豸�Ѵ�
                    cLogger(_loggerT.Success, $"MIDI Input Device On\n");
                    break;

                case MidiMessage.MIM_CLOSE:
                    // MIDI �����豸�ѹر�
                    cLogger(_loggerT.Warning, $"MIDI Input Device Off\n");
                    break;

                case MidiMessage.MIM_DATA:
                    // �յ� MIDI ������Ϣ
                    //inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_O);
                    var midimessage = dwParam1.ToInt32();
                    byte statusByte = (byte)((midimessage >> 0) & 0xFF);
                    byte dataByte1 = (byte)((midimessage >> 8) & 0xFF);
                    byte dataByte2 = (byte)((midimessage >> 16) & 0xFF);

                    // ����״̬�ֽڵĸ���λ���ж���Ϣ����
                    byte messageType = (byte)(statusByte & 0xF0);

                    // ����״̬�ֽڵĵ���λ����ȡ MIDI ͨ����
                    byte channel = (byte)(statusByte & 0x0F);

                    cLogger(_loggerT.DEBUG, $"0x{midimessage:X8}\t");

                    switch (messageType)
                    {
                        case 0x80:
                            // Note Off ��Ϣ
                            byte note = dataByte1; // ������
                            byte velocity = dataByte2; // �ɿ�ʱ������
                            cLogger(_loggerT.Success, $"Note Off\tCH:{channel}\tNote:{note}\tVel:{velocity}\n");
                            break;
                        case 0x90:
                            // Note On ��Ϣ
                            note = dataByte1; // ������
                            velocity = dataByte2; // ����ʱ������
                            var tmp = _cfg.Find(x => (x.KeyChannel == channel) && (x.KeyNote == note));
                            cLogger(_loggerT.Success, $"Note On\tCH:{channel}\tNote:{note}\tVel:{velocity}\n");
                            Debug.Assert(tmp != null);
                            if (tmp.mode == FuncMode.Once)
                            {
                                if (tmp.VelocityThreshold <= velocity)
                                {
                                    foreach (var item in tmp.SingleSteps)
                                    {
                                        switch(item.operate)
                                        {
                                            case MacroType.KeyBoardKey:
                                                Debug.Assert(!String.IsNullOrEmpty(item.data.ToString()));
                                                var Keyevent = JsonConvert.DeserializeObject<KeyBoardEvent>(item.data.ToString());
                                                Debug.Assert(Keyevent is KeyBoardEvent);
                                                switch (Keyevent.keyType)
                                                {
                                                    case KeyBoardEventType.KeyUp:
                                                        inputSimulator.Keyboard.KeyUp(Keyevent.key);
                                                        break;
                                                    case KeyBoardEventType.KeyDown:
                                                        inputSimulator.Keyboard.KeyDown(Keyevent.key);
                                                        break;
                                                    case KeyBoardEventType.KeyPress:
                                                        inputSimulator.Keyboard.KeyPress(Keyevent.key);
                                                        break;
                                                }
                                                break;
                                            case MacroType.Delay:
                                                break;
                                            case MacroType.OpenFile:
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    cLogger(_loggerT.Warning, $"\tInsufficient button velocity\n");
                                }
                            }

                            break;
                        case 0xB0:
                            // �������仯��Ϣ
                            byte controller = dataByte1; // ��������
                            byte value = dataByte2; // ������ֵ
                            cLogger(_loggerT.Success, $"Controller\tCH:{channel}\tController:{controller}\tValue:{value}\n");
                            break;

                        case 0xC0:
                            // Program Change ��Ϣ
                            byte program = dataByte1; // �µĳ����
                            cLogger(_loggerT.Success, $"Program Change\tCH:{channel}\tPGnum:{program}\n");
                            break;

                        case 0xE0:
                            // Pitch Bend ��Ϣ
                            int pitchBendValue = dataByte1 | (dataByte2 << 7);
                            cLogger(_loggerT.Success, $"Pitch Bend\tCH:{channel}\tValue:{pitchBendValue}\n");
                            break;

                        // �������͵� MIDI ��Ϣ
                        // �ɸ�����Ҫ�����Ӧ�Ĵ����֧

                        default:
                            // δ֪���͵� MIDI ��Ϣ
                            cLogger(_loggerT.Warning, $"�յ�δ֪���͵� MIDI ��Ϣ - ͨ��: {channel}, ״̬�ֽ�: {statusByte:X2}, �����ֽ�1: {dataByte1:X2}, �����ֽ�2: {dataByte2:X2}\n");
                            break;
                    }
                    break;
                case MidiMessage.MIM_LONGDATA:      // �յ�����Ϣ��SysEx ��������ʵʱ��Ϣ��
                    cLogger(_loggerT.Warning, $"MIM_LONGDATA function not currently supported\n");
                    break;

                case MidiMessage.MIM_ERROR:         // ���� MIDI �������
                case MidiMessage.MIM_LONGERROR:     // ��������Ϣ��SysEx ��������ʵʱ��Ϣ���Ĵ���
                    cLogger(_loggerT.Fail, $"MIDI Error.\nPlease contact the author and provide relevant equipment information and the cause of the error for repair.\nwMsg Code:{wMsg}\n\n");
                    break;
            }
        }

        public Worker()
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            cLogger(_loggerT.Motd,
                $"----------------------------------------------------------------------------\r\n" +
                $"�������[   �������[�����[�������������[ �����[�������[   �������[ �����������[ �������������[ �������������[ ���������������[�������������[ \r\n" +
                $"���������[ ���������U�����U�����X�T�T�����[�����U���������[ ���������U�����X�T�T�����[�����X�T�T�����[�����X�T�T�����[�����X�T�T�T�T�a�����X�T�T�����[\r\n" +
                $"�����X���������X�����U�����U�����U  �����U�����U�����X���������X�����U���������������U�������������X�a�������������X�a�����������[  �������������X�a\r\n" +
                $"�����U�^�����X�a�����U�����U�����U  �����U�����U�����U�^�����X�a�����U�����X�T�T�����U�����X�T�T�T�a �����X�T�T�T�a �����X�T�T�a  �����X�T�T�����[\r\n" +
                $"�����U �^�T�a �����U�����U�������������X�a�����U�����U �^�T�a �����U�����U  �����U�����U     �����U     ���������������[�����U  �����U\r\n" +
                $"�^�T�a     �^�T�a�^�T�a�^�T�T�T�T�T�a �^�T�a�^�T�a     �^�T�a�^�T�a  �^�T�a�^�T�a     �^�T�a     �^�T�T�T�T�T�T�a�^�T�a  �^�T�a\r\n" +
                $"----------------------------------------------------------------------------\r\n" +
                $"                                                                            \r\n");
            int deviceCount = midiInGetNumDevs();
            cLogger(_loggerT.Note, $"Device Amount: {deviceCount}\n\n");

            for (int deviceId = 0; deviceId < deviceCount; deviceId++)
            {
                MIDIINCAPS caps = new MIDIINCAPS();
                int res = midiInGetDevCaps(deviceId, ref caps, Marshal.SizeOf<MIDIINCAPS>());
                if (res == 0)
                {
                    cLogger(_loggerT.Info, $"Device {deviceId + 1}��{caps.szPname}\n");
                    cLogger(_loggerT.None, $"\tManufacturer ID: {caps.wMid}\n");
                    cLogger(_loggerT.None, $"\tPorduct ID: {caps.wPid}\n");
                    cLogger(_loggerT.None, $"\tDriver Ver: {caps.vDriverVersion}\n");
                    cLogger(_loggerT.None, $"\tSupported Func: 0x{caps.dwSupport:X}\n");
                    //cLogger(_loggerT.None, $"\tMIDI Channels: {caps.wChannels}\n");
                    //cLogger(_loggerT.None, $"\tReserved: {caps.wReserved}\n");
                    //cLogger(_loggerT.None, $"\tExtended Manufacturer ID: {caps.wMidEx}\n");
                    //cLogger(_loggerT.None, $"\tExtended Product ID: {caps.wPidEx}\n");
                    //cLogger(_loggerT.None, $"\tExtended Driver Version: {caps.vDriverVersionEx}\n");
                    //cLogger(_loggerT.None, $"\tExtended Device Name: {caps.szPnameEx}\n");
                    //cLogger(_loggerT.None, $"\tExtended Supported Func: 0x{caps.dwSupportEx:X}\n");
                    cLogger(_loggerT.None, $"\n");

                }
            }

            #region �����ļ�

            int uDeviceId = 1;
            if (!ConfigManager<List<object>>.isConfigExist())
            {
                cLogger(_loggerT.Success, $"Config file successfully loaded\n");
                uint res2 = midiOutOpen(out IntPtr hMidiOut, (uint)uDeviceId, IntPtr.Zero, IntPtr.Zero, 0);
                if (res2 == 0)
                {
                    List<OnceModeMidiKeyConfig> cfg = new List<OnceModeMidiKeyConfig>();
                    for (byte channelId = 0; channelId < 16; channelId++)
                    {
                        for (byte noteId = 0; noteId < 128; noteId++)
                        {
                            uint msg = (uint)((0x90 | (uint)channelId) | ((uint)noteId << 8) | (0x7F << 16));
                            res2 = midiOutShortMsg(hMidiOut, msg);
                            if (res2 == 0)
                            {
                                List<Config.SingleStep<object>> steps = new List<SingleStep<object>>();
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.CAPITAL)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_T)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.CAPITAL)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_E)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_S)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_T)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyDown, VirtualKeyCode.CONTROL)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_A)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_C)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_V)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_V)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_A)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_C)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_V)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyPress, VirtualKeyCode.VK_V)));
                                steps.Add(new SingleStep<object>(
                                    new KeyBoardEvent(
                                        KeyBoardEventType.KeyUp, VirtualKeyCode.CONTROL)));
                                Config.OnceModeMidiKeyConfig thekey = new Config.OnceModeMidiKeyConfig(
                                    channelId,
                                    noteId,
                                    64,
                                    steps);
                                cfg.Add(thekey);
                                //cLogger(_loggerT.Success, $"{channelId}-{noteId} isSupported\n");
                            }
                        }
                    }
                    ConfigManager<List<OnceModeMidiKeyConfig>>.GenerateConfig(cfg);
                    cLogger(_loggerT.Success, "Config saved successfully.\n");
                }
                else
                {
                    cLogger(_loggerT.Fail, $"Unable to open MIDI device\n");
                }

                midiOutClose(hMidiOut);
            }
            else 
            {
                cLogger(_loggerT.Success, "Config file detected\n");
            }
            _cfg = ConfigManager<List<OnceModeMidiKeyConfig>>.LoadConfig();
            cLogger(_loggerT.Success, $"Config file successfully loaded\n");
            #endregion

            IntPtr hMidiIn;
            cLogger(_loggerT.Success, $"Using Device:{uDeviceId + 1}\n");
            var midiInCallback = new MidiInCallback(MidiInputCallback);
            int result = midiInOpen(out hMidiIn, uDeviceId, midiInCallback, IntPtr.Zero, MIDI_CALLBACK_FUNCTION);

            if (result == 0)
            {
                result = midiInStart(hMidiIn);
                if (result != 0)
                {
                    StringBuilder errMsg = new StringBuilder(256);
                    midiInGetErrorText(result, errMsg, errMsg.Capacity);
                    cLogger(_loggerT.Fail, $"Unable to open MIDI input device,\n\terror code: {result}\n\terror message: {errMsg}\n");
                }
            }
            else
            {
                StringBuilder errMsg = new StringBuilder(256);
                midiInGetErrorText(result, errMsg, errMsg.Capacity);
                cLogger(_loggerT.Fail, $"Unable to open MIDI input device,\n\terror code: {result}\n\terror message: {errMsg}\n");
            }
            cLogger(_loggerT.Note, "Press any key to close MIDI device\n");
            Console.ReadKey();
            await Task.Delay(1);
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //        Console.WriteLine($"MIDI�豸������{midiInGetNumDevs()}");
            //        //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    await Task.Delay(1000, stoppingToken);
            //}
            result = midiInStop(hMidiIn);
            result = midiInClose(hMidiIn);
        }
    }
}