using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDIMapper
{
    public class CustomClass
    {
        /// <summary>
        /// 日志记录分类
        /// </summary>
        public enum _loggerT { None = -2, Note = -1, Info = 0, Success = 1, Fail = 2, DEBUG = 3, Warning = 4, Alert = 5, Motd = 6, ANS = 99 }
        /// <summary>
        /// /// 日志输出器
        /// </summary>
        /// <param name="type">记录种类</param>
        /// <param name="inf">记录信息</param>
        public static void cLogger(_loggerT type, string inf)
        {
            switch (type)
            {
                case _loggerT.Info:
                    ConsoleColor InfoBackColor = Console.BackgroundColor, InfoForeColor = ConsoleColor.DarkGreen;
                    Console.BackgroundColor = InfoBackColor;
                    Console.ForegroundColor = InfoForeColor;
                    Console.Write($"[INFO] {inf}");
                    Console.ResetColor();

                    break;
                case _loggerT.Note:
                    ConsoleColor NoteBackColor = Console.BackgroundColor, NoteForeColor = ConsoleColor.Cyan;
                    Console.BackgroundColor = NoteBackColor;
                    Console.ForegroundColor = NoteForeColor;
                    Console.Write($"[NOTE] {inf}");
                    Console.ResetColor();

                    break;
                case _loggerT.Success:
                    ConsoleColor SuccessBackColor = Console.BackgroundColor, SuccessForeColor = ConsoleColor.Green;
                    Console.BackgroundColor = SuccessBackColor;
                    Console.ForegroundColor = SuccessForeColor;
                    Console.Write($"[+] {inf}");
                    Console.ResetColor();
                    break;
                case _loggerT.Fail:
                    ConsoleColor FailBackColor = Console.BackgroundColor, FailForeColor = ConsoleColor.Red;
                    Console.BackgroundColor = FailBackColor;
                    Console.ForegroundColor = FailForeColor;
                    Console.Write($"[-] {inf}");
                    Console.ResetColor();
                    break;
                case _loggerT.DEBUG:
                    ConsoleColor DEBUGBackColor = Console.BackgroundColor, DEBUGForeColor = ConsoleColor.DarkGray;
                    Console.BackgroundColor = DEBUGBackColor;
                    Console.ForegroundColor = DEBUGForeColor;
                    Console.Write($"[DEBUG]: {inf}");
                    Console.ResetColor();
                    break;
                case _loggerT.Warning:
                    ConsoleColor WarningBackColor = Console.BackgroundColor, WarningForeColor = ConsoleColor.Yellow;
                    Console.BackgroundColor = WarningBackColor;
                    Console.ForegroundColor = WarningForeColor;
                    Console.Write($"[WARNING] {inf}");
                    Console.ResetColor();
                    break;
                case _loggerT.Alert:
                    ConsoleColor AlertBackColor = Console.BackgroundColor, AlertForeColor = ConsoleColor.DarkYellow;
                    Console.BackgroundColor = AlertBackColor;
                    Console.ForegroundColor = AlertForeColor;
                    Console.Write($"[AlERT] {inf}");
                    Console.ResetColor();
                    break;
                case _loggerT.Motd:
                    ConsoleColor MotdBackColor = Console.BackgroundColor, MotdForeColor = ConsoleColor.Magenta;
                    Console.BackgroundColor = MotdBackColor;
                    Console.ForegroundColor = MotdForeColor;
                    Console.Write($"{inf}");
                    Console.ResetColor();
                    break;
                case _loggerT.ANS:
                    ConsoleColor ANSBackColor = ConsoleColor.White, ANSForeColor = ConsoleColor.Black;
                    Console.BackgroundColor = ANSBackColor;
                    Console.ForegroundColor = ANSForeColor;
                    Console.Write($"[ANSWER] {inf}");
                    Console.ResetColor();
                    break;
                default:
                    Console.BackgroundColor = Console.BackgroundColor;
                    Console.ForegroundColor = Console.ForegroundColor;
                    Console.Write($"{inf}");
                    Console.ResetColor();
                    break;
            }
        }


    }
}
