using WindowsInput.Native;

namespace MIDIMapper
{
    internal class Config
    {
        /// <summary>
        /// 对应MIDI按键支持的功能
        /// </summary>
        public enum FuncMode : Byte
        {
            Once = 0b0000_0001,         // 按下MIDI按键则触发一次
            Repeat = 0b0000_0010,       //暂未支持
            Trigger = 0b0000_0100,      //暂未支持
            Advance = 0b0000_1000       //暂未支持
        }

        #region Once
        /// <summary>
        /// 单个MIDI按键按下触发Once的对应的配置和宏记录
        /// </summary>
        public class OnceModeMidiKeyConfig
        {
            /// <summary>
            /// 记录此类型为单次触发型
            /// </summary>
            public FuncMode mode = FuncMode.Once;

            /// <summary>
            /// 记录MIDI键盘对应按键通道
            /// </summary>
            public Byte KeyChannel;

            /// <summary>
            /// 对应MIDI键盘通道下符号位
            /// </summary>
            public Byte KeyNote;

            /// <summary>
            /// 记录触发此宏的速度阈值，低于阈值则不触发
            /// </summary>
            public Byte VelocityThreshold;

            public List<SingleStep<object>> SingleSteps { get; set; }

            /// <summary>
            /// 记录触发宏后的操作
            /// </summary>
            //public KeyNoteMacro OperationRecord { get; set; }
            public OnceModeMidiKeyConfig(byte _keyChannel, byte _keyNote, byte _velocityThreshold, List<SingleStep<object>> _singleSteps)
            {
                if (_keyChannel < 0)
                    KeyChannel = 0;
                else if (_keyChannel > 15)
                    KeyChannel = 15;
                else
                    KeyChannel = _keyChannel;

                if (_keyNote < 0)
                    KeyNote = 0;
                else if (_keyNote > 127)
                    KeyNote = 127;
                else
                    KeyNote = _keyNote;

                if (_velocityThreshold < 0)
                    VelocityThreshold = 0;
                else if (_velocityThreshold > 127)
                    VelocityThreshold = 127;
                else
                    VelocityThreshold = _velocityThreshold;

                SingleSteps = _singleSteps;
            }
        }
        #endregion

        /// <summary>
        /// 可用宏步骤
        /// </summary>
        public enum MacroType : Byte
        {
            KeyBoardKey = 0b0000_0001,      // 键盘按键宏
            Delay = 0b0000_0100,            // 延迟
            OpenFile = 0b0000_0010          // 打开文件
        }

        #region KeyBoardKey
        /// <summary>
        /// KeyBoardKey单步骤类型
        /// </summary>
        public enum KeyBoardEventType
        {
            KeyUp = 0b0000_0001,            // 松开按键
            KeyDown = 0b0000_0010,          // 按下按键
            KeyPress = 0b0000_0100          // KeyDown + KeyUp
        }

        /// <summary>
        /// KeyBoard单步骤
        /// </summary>
        public class KeyBoardEvent
        {
            public KeyBoardEventType keyType;
            public VirtualKeyCode key;
            public KeyBoardEvent(KeyBoardEventType _keyType, VirtualKeyCode _key)
            { 
                keyType = _keyType;
                key = _key;
            }
        }
        #endregion

        #region Delay
        #endregion

        #region OpenFile
        #endregion

        /// <summary>
        /// 按键单步骤
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class SingleStep<T>
        {
            public MacroType operate { get; set; }
            public T data { get; set; }
            public SingleStep(T _data)
            {
                this.data = _data;
                switch (data)
                {
                    case KeyBoardEvent:
                        operate = MacroType.KeyBoardKey;
                        break;
                        //case DelayEvent:
                        //    break;
                        //case OpenFileEvent:
                        //    break;
                }
            }
        }
    }
}
